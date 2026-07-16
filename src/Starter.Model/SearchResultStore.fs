namespace Starter.Features

open System
open System.Collections.Generic
open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open Avalonia.Threading
open Starter.TextMatching
open R3
open Serilog
open Starter.Features.CustomCollections
open Starter.SearchEngine

type ContextMenuState =
    | Loading of IContextMenuLoader
    | SomeItemsLoaded of IContextMenuLoader * ResizeArray<int> * ResizeArray<ContextMenuResultData>

type SearchResultStore(resultScoreDb, searchEngines: IDictionary<string, ISearchEngine>) =
    let staticResults = new BehaviorSubject<_>(ResizeArray())
    let dynamicSearchEngines = ResizeArray<IDynamicSearchEngine>()

    let slab = Memory.Slab.createDefault()
    let comparer =
        Comparison<SearchResultData>(fun e1 e2 ->
            compare
                (SearchResultData.getWeight resultScoreDb e1)
                (SearchResultData.getWeight resultScoreDb e2)
        )
    let contextMenuComparer =
        fun e1 e2 ->
            compare
                (ContextMenuResultData.getWeight resultScoreDb e1)
                (ContextMenuResultData.getWeight resultScoreDb e2)

    let stopwatch = Stopwatch()

    let mutable queryCancellationTokenSource = new CancellationTokenSource()

    let mutable contextMenu = Stack<_>()
    let mutable lastQuery = ValueNone

    /// Output results
    let results = ObservableList<SearchResultData> 300
    let contextMenuResults = ObservableList<ContextMenuResultData> 20
    let loadingTimes = new Subject<TimeSpan Nullable>()

    let fuzzyMatchResult normalizedText result =
        let res =
            match result.NormalizedName with
            | ValueNone -> FuzzyMatch.string false true true slab normalizedText result.SearchResult.Name
            | ValueSome name -> FuzzyMatch.runes false false true slab normalizedText (Span name)

        result.FuzzyMatchResult <- res

        match res with
        | ValueSome fusilResult when fusilResult.Score > 0s ->
            result.AccentuationMap <- fusilResult.MatchingPositions
            true
        | _ ->
            match result.SearchResult.Keywords with
            | null -> false
            | keywords ->
                keywords |> Array.exists (fun keyword ->
                    match FuzzyMatch.fastString false normalizedText keyword with
                    | ValueSome res when res.Score > 0s ->
                        result.AccentuationMap <- null
                        result.FuzzyMatchResult <- ValueSome res
                        true
                    | _ -> false
                )

    let fuzzyMatchContextMenuResult normalizedText (result: ContextMenuEntryData) =
        let res = FuzzyMatch.string false true true slab normalizedText result.Name

        result.FuzzyMatchResult <- res

        match res with
        | ValueSome fusilResult when fusilResult.Score > 0s ->
            result.AccentuationMap <- fusilResult.MatchingPositions
            true
        | _ ->
            match result.Result.Keywords with
            | null -> false
            | keywords ->
                keywords |> Array.exists (fun keyword ->
                    match FuzzyMatch.fastString false normalizedText keyword with
                    | ValueSome res when res.Score > 0s ->
                        result.AccentuationMap <- null
                        result.FuzzyMatchResult <- ValueSome res
                        true
                    | _ -> false
                )



    let querySingleDynamicSearchEngine ct (activator: ISearchEngineActivator | null) query (engine: IDynamicSearchEngine) = // Show only this engine results
        try
            let struct (instantResults, futureResults) = engine.Search(query, ct, activator)

            let inline addResults r =
                let c = results.Count
                results.AddRange r

                if c <> results.Count then // If r was not empty
                    results.Sort comparer
                    results.NotifyChanged()

            instantResults
            |> Seq.map (SearchResultData.createDynamic engine)
            |> fun r ->
                results.AddRange r
                results.Sort comparer
                results.NotifyChanged()

            match engine.BufferResults with
            | false ->
                futureResults
                    .Select(Seq.map (SearchResultData.createDynamic engine))
                    .ObserveOnUIThreadDispatcher()
                    .Subscribe addResults
            | true ->
                futureResults
                    .Chunk(TimeSpan.FromMilliseconds 200L)
                    .Select(Seq.collect (Seq.map (SearchResultData.createDynamic engine)))
                    .Subscribe(fun r -> Dispatcher.UIThread.Post(fun () -> addResults r))
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
            |> Seq.filter (fun result -> result.SearchResult.ShowIfNoActivator && fuzzyMatchResult normalizedText result)
            |> results.AddRange
            results.Sort comparer
            results.NotifyChanged()

            stopwatch.Stop()
            stopwatch.Elapsed |> loadingTimes.OnNext
        )
        |> disposeOnCancelled ct

    let querySingleStaticSearchEngine ct (activator: ISearchEngineActivator) query = // Show only this engine results
        stopwatch.Restart()
        let normalizedText =
            query
            |> TextNormalization.String.normalize
            |> Array.map System.Text.Rune.ToLowerInvariant

        staticResults.Subscribe(fun staticResults ->
            stopwatch.Start()
            results.Clear()

            match query with
            | "" -> // Show all search engine results
                staticResults |> Seq.filter (fun result ->
                    if result.SearchEngineId = activator.SearchEngineId
                       && (result.SearchResult.ActivatorFilter |> Array.isEmpty
                           || result.SearchResult.ActivatorFilter |> Array.contains activator) then
                        result.AccentuationMap <- null
                        true
                    else false
                )
            | _ -> // Show matching results
                staticResults |> Seq.filter (fun result ->
                    result.SearchEngineId = activator.SearchEngineId
                    && (result.SearchResult.ActivatorFilter |> Array.isEmpty
                        || result.SearchResult.ActivatorFilter |> Array.contains activator)
                    && fuzzyMatchResult normalizedText result
                )
            |> results.AddRange

            results.Sort comparer
            results.NotifyChanged()

            stopwatch.Stop()
            stopwatch.Elapsed |> loadingTimes.OnNext
        ) |> disposeOnCancelled ct

    let queryContextMenuResults (contextMenu: ContextMenuState) query =
        stopwatch.Restart()
        contextMenuResults.Clear()

        match contextMenu with
        | Loading _ -> ()
        | SomeItemsLoaded (_, _, results) ->
            let normalizedText =
                query
                |> TextNormalization.String.normalize
                |> Array.map System.Text.Rune.ToLowerInvariant

            match query with
            | "" ->
                // Show all search engine results
                results |> Seq.iter (function
                    | ContextMenuResultData.Entry entry -> entry.AccentuationMap <- null
                    | _ -> ()
                )

                results :> _ seq
            | _ ->
                // Show matching results
                results
                |> Seq.filter (function
                    | ContextMenuResultData.Entry entry -> fuzzyMatchContextMenuResult normalizedText entry
                    | _ -> false
                )
                |> Seq.sortWith contextMenuComparer
            |> contextMenuResults.AddRange

        contextMenuResults.NotifyChanged()

        stopwatch.Stop()
        stopwatch.Elapsed |> loadingTimes.OnNext

    interface IDisposable with
        member this.Dispose() = staticResults.Dispose()

    member this.SetContextMenu(loader: IContextMenuLoader) =
        try
            contextMenuResults.Clear()
            let state = ContextMenuState.Loading loader |> ref
            contextMenu.Push state

            let rec insert (idx: int) (data: ContextMenuResultData) (pos: int) =
                match state.Value with
                | Loading _ -> ()
                | SomeItemsLoaded (_, indexes, results) ->
                    if pos >= results.Count then
                        indexes.Add idx
                        results.Add data

                    elif idx < indexes[pos] then
                        indexes.Add idx
                        results.Add data

                        for i = results.Count-1 to pos+1 do
                            let temp = results[i-1]
                            results[i-1] <- results[i]
                            results[i] <- temp

                            let temp = indexes[i-1]
                            indexes[i-1] <- indexes[i]
                            indexes[i] <- temp
                    elif idx = indexes[pos] then
                        results[pos] <- data
                    else insert idx data (pos+1)

            loader.LoadItems(
                (fun itemCount ->
                    Dispatcher.UIThread.Post(
                        (fun () ->
                            state.contents <- ContextMenuState.SomeItemsLoaded (
                                loader,
                                Array.init itemCount id |> ResizeArray,
                                Array.create itemCount ContextMenuResultData.Loading |> ResizeArray
                            )
                        ),
                        DispatcherPriority.Background
                    )
                ),
                (fun result idx ->
                    Dispatcher.UIThread.Post(
                        (fun () ->
                            let data = ContextMenuResultData.create result
                            insert idx data 0
                            this.Requery()
                        ),
                        DispatcherPriority.Background
                    )
                ),
                (fun exn idx ->
                    Dispatcher.UIThread.Post(
                        (fun () ->
                            let data = ContextMenuResultData.LoadFailed exn
                            insert idx data 0
                            this.Requery()
                        ),
                        DispatcherPriority.Background
                    )
                ),
                ignore
            )
        with e ->
            Log.Error(e, "Failed to load context menu")

    member this.ExitContextMenu() = contextMenu.TryPop() |> ignore
    member this.ContextMenuEnabled = contextMenu.Count <> 0

    member this.Results = results
    member this.ContextMenuResults = contextMenuResults
    member this.LoadingTimes = loadingTimes

    member this.AddSource(searchEngine: IStaticSearchEngine) =
        Task.Run<unit>(fun () -> task {
            try
                let! results = searchEngine.LoadResults()

                Log.Debug $"{searchEngine.Name}: %A{results}"

                results
                |> Seq.map (SearchResultData.createStatic searchEngine.Id)
                |> Seq.cache
                |> function
                    | s when Seq.isEmpty s -> ()
                    | s ->
                        staticResults.Value.AddRange s
                        staticResults.Value |> staticResults.OnNext
                        Log.Information $"{searchEngine.Name} results loaded"

                searchEngine.ResultsChanged.Subscribe(fun newResults ->
                    // Remove old results
                    staticResults.Value.RemoveAll(fun res -> res.SearchEngineId = searchEngine.Id) |> ignore

                    // Add new results
                    newResults
                    |> Seq.map (SearchResultData.createStatic searchEngine.Id)
                    |> Seq.cache
                    |> function
                        | s when Seq.isEmpty s -> ()
                        | s ->
                            staticResults.Value.AddRange s
                            staticResults.Value.Sort comparer
                            staticResults.Value |> staticResults.OnNext
                            Log.Information $"{searchEngine.Name} results loaded"
                ) |> ignore
            with e -> Log.Error(e, $"{searchEngine.Name} failed to load results:\n{e.Message}")
        })

    member this.AddSource(searchEngine: IDynamicSearchEngine) =
        dynamicSearchEngines.Add searchEngine

    member this.ClearResults() =
        loadingTimes.OnNext (Nullable())
        queryCancellationTokenSource.Cancel()
        results.Clear()
        results.NotifyChanged()

    member this.Query(text: string, activator: ISearchEngineActivator | null) =
        queryCancellationTokenSource.Cancel()
        queryCancellationTokenSource <- new CancellationTokenSource()
        let ct = queryCancellationTokenSource.Token
        lastQuery <- ValueSome text

        match contextMenu.TryPeek() with
        | false, _ ->
            match activator with
            | null when text = String.Empty -> this.ClearResults()
            | null -> queryAllSearchEngines ct text
            | activator ->
                match searchEngines.TryGetValue activator.SearchEngineId with
                | true, :? IStaticSearchEngine -> querySingleStaticSearchEngine ct activator text
                | true, (:? IDynamicSearchEngine as searchEngine) ->
                    results.Clear()
                    querySingleDynamicSearchEngine ct activator text searchEngine
                | _ -> Log.Error $"Cannot find search engine matching the current activator: {activator.Id}"

        | true, contextMenu ->
            queryContextMenuResults contextMenu.Value text

    member this.SortResults() =
        results.Sort comparer
        results.NotifyChanged()

    member private this.Requery() =
        match lastQuery with
        | ValueNone -> ()
        | ValueSome lastQuery -> this.Query(lastQuery, null)
