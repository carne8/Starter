namespace Starter.Features

open System
open System.Collections.Generic
open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open Avalonia.Threading
open R3
open Serilog
open Starter.Features.CustomCollections
open Starter.SearchEngine
open Starter.TextMatching
open FsToolkit.ErrorHandling

type SearchResultStore(
        resultScoreDb,
        searchEngines: IDictionary<string, SearchEngine>,
        fuzzyMatchResults: Dictionary<SearchResult, FuzzyMatch.FuzzyResult>
    )
    =
    let normalizedTexts = Dictionary<SearchResult, Text.Rune array>()

    let staticResults = new BehaviorSubject<SearchResult ResizeArray>(ResizeArray())
    let dynamicSearchEngines = ResizeArray<DynamicSearchEngine>()

    let slab = Memory.Slab.createDefault()
    let comparer =
        Comparison<SearchResult>(fun e1 e2 ->
            let f1, f2 =
                fuzzyMatchResults.TryGetValue e1 |> ValueOption.ofPair,
                fuzzyMatchResults.TryGetValue e2 |> ValueOption.ofPair

            compare
                (SearchResultHelpers.getWeight resultScoreDb f1 e1)
                (SearchResultHelpers.getWeight resultScoreDb f2 e2)
        )

    let stopwatch = Stopwatch()

    let mutable queryCancellationTokenSource = new CancellationTokenSource()

    /// Output results
    let results = ObservableList<SearchResult> 300
    let loadingTimes = new Subject<TimeSpan Nullable>()

    let fuzzyMatchResult normalizedText result =
        let fusilResult =
            match normalizedTexts.TryGetValue result with
            | false, _ -> FuzzyMatch.string false true true slab normalizedText result.Name
            | true, name -> FuzzyMatch.runes false false true slab normalizedText (Span name)


        match fusilResult with
        | ValueSome fusilResult when fusilResult.Score >= 0s ->
            fuzzyMatchResults[result] <- fusilResult
            true
        | _ ->
            match result.Keywords with
            | null ->
                fuzzyMatchResults.Remove result |> ignore
                false
            | keywords ->
                keywords |> Array.exists (fun keyword ->
                    match FuzzyMatch.fastString false normalizedText keyword with
                    | ValueSome fusilResult when fusilResult.Score > 0s ->
                        fuzzyMatchResults[result] <- fusilResult
                        true
                    | _ -> false
                )

    let querySingleDynamicSearchEngine ct (activator: SearchEngineActivator | null) query (engine: DynamicSearchEngine) = // Show only this engine results
        try
            let struct (instantResults, futureResults) = engine.Search(query, ct, activator)

            let inline addResults r =
                let c = results.Count
                r
                |> Seq.map (fun (s: SearchResult) ->
                    s.SearchEngineId <- engine.Id
                    s
                )
                |> results.AddRange

                if c <> results.Count then // If r was not empty
                    results.Sort comparer
                    results.NotifyChanged()

            addResults instantResults

            match engine.BufferResults with
            | false ->
                futureResults
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe addResults
            | true ->
                futureResults
                    .Chunk(TimeSpan.FromMilliseconds 200L)
                    .Subscribe(fun r -> Dispatcher.UIThread.Post(fun () ->
                        r
                        |> Seq.concat
                        |> addResults
                    ))
            |> disposeOnCancelled ct
        with e ->
            Log.Error(e, $"Failed to get results from dynamic search engine: {engine.Name}")

    let queryAllSearchEngines (ct: CancellationToken) query =
        stopwatch.Restart()

        let normalizedText =
            query
            |> TextNormalization.String.normalize
            |> Array.map System.Text.Rune.ToLowerInvariant

        let mutable cts = new CancellationTokenSource()

        staticResults.Subscribe(fun staticResults ->
            stopwatch.Start()

            // Make sure to not call dynamic engines without cancelling the previous request
            cts.Cancel()
            cts <- new CancellationTokenSource()
            ct.Register(fun () -> cts.Cancel()) |> ignore

            results.Clear()

            // Dynamic results
            dynamicSearchEngines |> Seq.iter (querySingleDynamicSearchEngine cts.Token null query)

            // Static results
            staticResults
            |> Seq.filter (fun result -> result.ShowIfNoActivator && fuzzyMatchResult normalizedText result)
            |> results.AddRange
            results.Sort comparer
            results.NotifyChanged()

            stopwatch.Stop()
            stopwatch.Elapsed |> loadingTimes.OnNext
        )
        |> disposeOnCancelled ct

    let querySingleStaticSearchEngine ct (activator: SearchEngineActivator) query = // Show only this engine results
        stopwatch.Restart()
        let normalizedText =
            query
            |> TextNormalization.String.normalize
            |> Array.map System.Text.Rune.ToLowerInvariant

        staticResults.Subscribe(fun staticResults ->
            stopwatch.Start()
            results.Clear()

            staticResults
            |> Seq.filter (fun result ->
                result.SearchEngineId = activator.SearchEngineId
                &&
                match result.ActivatorFilter with
                | null | [| |] -> true
                | activators -> activators |> Array.contains activator
                &&
                fuzzyMatchResult normalizedText result
            )
            |> results.AddRange

            results.Sort comparer
            results.NotifyChanged()

            stopwatch.Stop()
            stopwatch.Elapsed |> loadingTimes.OnNext
        ) |> disposeOnCancelled ct

    interface IDisposable with
        member this.Dispose() = staticResults.Dispose()

    member this.Results = results
    member this.LoadingTimes = loadingTimes

    member this.AddSource(searchEngine: StaticSearchEngine) =
        Task.Run<unit>(fun () -> task {
            try
                let! results = searchEngine.LoadResults()

                Log.Debug $"{searchEngine.Name}: %A{results}"

                results
                |> Seq.cache
                |> function
                    | s when Seq.isEmpty s -> ()
                    | s ->
                        s |> Seq.iter (fun s -> s.SearchEngineId <- searchEngine.Id)
                        staticResults.Value.AddRange s
                        staticResults.Value |> staticResults.OnNext
                        Log.Information $"{searchEngine.Name} results loaded"

                searchEngine.ResultsChanged.Subscribe(fun newResults ->
                    // Remove old results
                    staticResults.Value.RemoveAll(fun res -> res.SearchEngineId = searchEngine.Id) |> ignore

                    // Add new results
                    newResults
                    |> Seq.cache
                    |> function
                        | s when Seq.isEmpty s -> ()
                        | s ->
                            s |> Seq.iter (fun s -> s.SearchEngineId <- searchEngine.Id)
                            staticResults.Value.AddRange s // TODO: Prevent parallel execution
                            staticResults.Value.Sort comparer
                            staticResults.Value |> staticResults.OnNext
                            Log.Information $"{searchEngine.Name} results loaded"
                ) |> ignore
            with e -> Log.Error(e, $"{searchEngine.Name} failed to load results:\n{e.Message}")
        })

    member this.AddSource(searchEngine: DynamicSearchEngine) =
        dynamicSearchEngines.Add searchEngine

    member this.ClearResults() =
        loadingTimes.OnNext (Nullable())
        queryCancellationTokenSource.Cancel()
        results.Clear()
        results.NotifyChanged()

    member this.Query(text: string, activator: SearchEngineActivator | null) =
        queryCancellationTokenSource.Cancel()
        queryCancellationTokenSource <- new CancellationTokenSource()
        let ct = queryCancellationTokenSource.Token

        match activator with
        | null when text = String.Empty -> this.ClearResults()
        | null -> queryAllSearchEngines ct text
        | activator ->
            match searchEngines.TryGetValue activator.SearchEngineId with
            | true, :? StaticSearchEngine -> querySingleStaticSearchEngine ct activator text
            | true, (:? DynamicSearchEngine as searchEngine) ->
                results.Clear()
                querySingleDynamicSearchEngine ct activator text searchEngine
            | _ -> Log.Error $"Cannot find search engine matching the current activator: {activator.Id}"

    member this.SortResults() =
        results.Sort comparer
        results.NotifyChanged()
