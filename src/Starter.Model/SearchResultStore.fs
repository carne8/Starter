namespace Starter.Features

open System
open System.Collections.Generic
open System.Threading
open Fusil
open R3
open Serilog
open Starter.Features.CustomCollections
open Starter.SearchEngine

type SearchResultStore(resultScoreDb, searchEngines: IDictionary<string, SearchEngine>) =
    let staticResults = new BehaviorSubject<_>(ResizeArray())
    let dynamicSearchEngines = ResizeArray<DynamicSearchEngine>()

    let slab = Memory.Slab.createDefault()
    let comparer =
        Comparison<SearchResultData>(fun e1 e2 ->
            compare (SearchResultData.getWeight resultScoreDb e1) (SearchResultData.getWeight resultScoreDb e2)
        )

    let mutable queryCancellationTokenSource = new CancellationTokenSource()
    /// Output results
    let results = ObservableList<SearchResultData> 300

    let queryAllSearchEngines ct query =
        let normalizedText =
            query
            |> TextNormalization.String.normalize
            |> Array.map System.Text.Rune.ToLowerInvariant

        let fuzzyMatchFunc = fuzzyMatch false true true slab normalizedText

        staticResults.Subscribe(fun staticResults ->
            results.Clear()

            // Dynamic results
            dynamicSearchEngines |> Seq.iter (fun searchEngine ->
                let struct (newResults, futureResults) = searchEngine.Search(query, ct, null)

                let kind =
                    match searchEngine.ImportantResults with
                    | true -> SearchResultKind.DynamicUnique
                    | false -> SearchResultKind.DynamicInstant

                newResults
                |> Seq.map (SearchResultData.createDynamic searchEngine kind)
                |> results.AddRange

                results.Sort comparer
                results.NotifyChanged()

                futureResults
                |> Observable.subscribe (fun newResults ->
                    newResults
                    |> Seq.map (SearchResultData.createDynamic searchEngine SearchResultKind.Dynamic)
                    |> results.AddRange

                    results.Sort comparer
                    results.NotifyChanged()
                )
                |> disposeOnCancelled ct
            )

            // Static results
            staticResults
            |> Seq.filter (fun result ->
                let fuzzyRes = fuzzyMatchFunc result.SearchResult.Name
                result.FuzzyMatchResult <- fuzzyRes

                match fuzzyRes with
                | ValueSome fusilResult when fusilResult.Score > 0s ->
                    result.AccentuationMap <- fusilResult.MatchingPositions
                    true
                | _ -> false
            )
            |> results.AddRange
            results.Sort comparer
            results.NotifyChanged()
        )
        |> disposeOnCancelled ct

    let queryStaticSearchEngine ct (activator: ISearchEngineActivator) query = // Show only this engine results
        let normalizedText =
            query
            |> TextNormalization.String.normalize
            |> Array.map System.Text.Rune.ToLowerInvariant

        let fuzzyMatchFunc = fuzzyMatch false true true slab normalizedText

        staticResults.Subscribe(fun staticResults ->
            results.Clear()

            match query with
            | "" -> // Show all search engine results
                staticResults |> Seq.filter (fun result ->
                    if result.SearchEngineId <> activator.SearchEngineId then false else
                    if result.SearchResult.ActivatorFilter |> Array.contains activator |> not then false else
                    result.AccentuationMap <- null
                    true
                )
            | _ -> // Show matching results
                staticResults |> Seq.filter (fun result ->
                    if result.SearchEngineId <> activator.SearchEngineId then false else
                    if result.SearchResult.ActivatorFilter |> Array.contains activator |> not then false else

                    let fuzzyRes = fuzzyMatchFunc result.SearchResult.Name
                    result.FuzzyMatchResult <- fuzzyRes

                    match fuzzyRes with
                    | ValueSome fusilResult when fusilResult.Score > 0s ->
                        result.AccentuationMap <- fusilResult.MatchingPositions
                        true
                    | _ -> false
                )
            |> results.AddRange

            results.Sort comparer
            results.NotifyChanged()
        ) |> disposeOnCancelled ct

    let queryDynamicSearchEngine ct (activator: ISearchEngineActivator) query (engine: DynamicSearchEngine) = // Show only this engine results
        try
            results.Clear()
            let struct (instantResults, obs) = engine.Search(query, ct, activator)

            obs.ObserveOnUIThreadDispatcher().Subscribe(fun newResults ->
                newResults
                |> Seq.map (SearchResultData.createDynamic engine SearchResultKind.Dynamic)
                |> results.AddRange

                results.Sort comparer
                results.NotifyChanged()
            )
            |> disposeOnCancelled ct

            let instantSrKind =
                match engine.ImportantResults with
                | true -> SearchResultKind.DynamicUnique
                | false -> SearchResultKind.DynamicInstant

            instantResults
            |> Seq.map (SearchResultData.createDynamic engine instantSrKind)
            |> results.AddRange

            results.Sort comparer
            results.NotifyChanged()
        with e ->
            Log.Error(e, $"Failed to get results from dynamic search engine: {engine.Name}")


    interface IDisposable with
        member this.Dispose() = staticResults.Dispose()

    member this.Results = results

    member this.AddSource(searchEngine: StaticSearchEngine) =
        task {
            try
                let! results, resultsChanged = searchEngine.LoadResults()

                Log.Debug $"{searchEngine.Name}: %A{results}"

                results
                |> Seq.map (SearchResultData.createStatic searchEngine)
                |> Seq.sortBy (SearchResultData.getWeight resultScoreDb)
                |> Seq.toArray
                |> function
                    | [||] -> ()
                    | arr ->
                        staticResults.Value.AddRange arr
                        staticResults.Value |> staticResults.OnNext
                        Log.Information $"{searchEngine.Name} results loaded"

                resultsChanged.Subscribe(fun newResults ->
                    let othersResults =
                        staticResults.Value
                        |> Seq.filter (_.SearchEngineId >> (<>) searchEngine.Id)

                    newResults
                    |> Seq.map (SearchResultData.createStatic searchEngine)
                    |> Seq.append othersResults
                    |> Seq.sortBy (SearchResultData.getWeight resultScoreDb)
                    |> Seq.toArray
                    |> function
                        | [||] -> ()
                        | arr ->
                            staticResults.Value.Clear()
                            staticResults.Value.AddRange arr
                            staticResults.Value |> staticResults.OnNext
                            Log.Information $"{searchEngine.Name} results loaded"
                ) |> ignore
            with e -> Log.Error(e, $"{searchEngine.Name} failed to load results:\n{e.Message}")
        }

    member this.AddSource(searchEngine: DynamicSearchEngine) =
        dynamicSearchEngines.Add searchEngine

    member this.ClearResults() =
        queryCancellationTokenSource.Cancel()
        results.Clear()
        results.NotifyChanged()

    member this.Query(text: string, activator: ISearchEngineActivator | null) =
        queryCancellationTokenSource.Cancel()
        queryCancellationTokenSource <- new CancellationTokenSource()
        let ct = queryCancellationTokenSource.Token

        match activator with
        | null -> queryAllSearchEngines ct text
        | activator ->
            match searchEngines.TryGetValue activator.SearchEngineId with
            | true, :? StaticSearchEngine -> queryStaticSearchEngine ct activator text
            | true, (:? DynamicSearchEngine as searchEngine) -> queryDynamicSearchEngine ct activator text searchEngine
            | _ -> Log.Error $"Cannot find search engine matching the current activator: {activator.Id}"
