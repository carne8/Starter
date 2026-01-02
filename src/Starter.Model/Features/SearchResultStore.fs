module Starter.Features.SearchResultStore

open System
open System.Collections.Generic
open System.Threading
open Fusil
open Fusil.Fusil
open R3
open Serilog
open Starter
open Starter.Features.CustomCollections
open Starter.SearchEngine

type SearchResultStore(resultScoreDb) =
    let staticResults = new BehaviorSubject<_>(ResizeArray())
    let dynamicSearchEngines = ResizeArray()

    let slab = Slab.createDefault()
    let comparer =
        Comparison<SearchResultData>(fun e1 e2 ->
            compare (SearchResultData.getWeight resultScoreDb e1) (SearchResultData.getWeight resultScoreDb e2)
        )

    let mutable queryCancellationTokenSource = new CancellationTokenSource()

    /// Output results
    let results = ObservableList<SearchResultData> 300

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

    member this.Query(text: string) =
        queryCancellationTokenSource.Cancel()
        queryCancellationTokenSource <- new CancellationTokenSource()
        let ct = queryCancellationTokenSource.Token

        let normalizedText =
            text
            |> TextNormalization.String.normalize
            |> Array.map System.Text.Rune.ToLowerInvariant // TODO: Do this in Fusil

        let fuzzyMatchFunc =
            fuzzyMatch false true true slab normalizedText
            >> ValueOption.ofOption // TODO: Change this in Fusil

        staticResults.Subscribe(fun staticResults ->
            results.Clear()

            // Dynamic results
            dynamicSearchEngines |> Seq.iter (fun searchEngine ->
                let struct (newResults, futureResults) = searchEngine.Search(text, ct, null) // TODO: Activator

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
