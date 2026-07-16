namespace Starter.Features

open System.Collections.Generic
open Avalonia.Threading
open Serilog
open Starter.Features.CustomCollections
open Starter.SearchEngine
open Starter.TextMatching

type ContextMenuState =
    | Loading of IContextMenuLoader
    | SomeItemsLoaded of IContextMenuLoader * ResizeArray<int> * ResizeArray<ContextMenuResultData>

type ContextMenuStore(resultScoreDb) =
    let contextMenuComparer =
        fun e1 e2 ->
            compare
                (ContextMenuResultData.getWeight resultScoreDb e1)
                (ContextMenuResultData.getWeight resultScoreDb e2)

    let slab = Memory.Slab.createDefault()
    let fuzzyMatchResult normalizedText (result: ContextMenuEntryData) =
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


    let rec insert (state: ContextMenuState ref) (idx: int) (data: ContextMenuResultData) (pos: int) =
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

            else insert state idx data (pos+1)

    let removeAllLoading (state: ContextMenuState ref) =
        match state.Value with
        | Loading _ -> ()
        | SomeItemsLoaded (_, indexes, results) ->
            let n = results.Count
            let mutable freeIdx = 0

            // Find first element that we want to remove
            while freeIdx < n && not results[freeIdx].IsLoading do
                freeIdx <- freeIdx+1

            let mutable current = freeIdx+1
            while current < n do
                // Find first element we want to keep
                while current < n &&
                      (results[current].IsLoading ||
                       (freeIdx >= 1
                        && results[current].IsSeparator
                        && results[freeIdx-1].IsSeparator)) do // Prevent consecutive separator
                    current <- current + 1

                if current < n then
                    indexes[freeIdx] <- indexes[current]
                    results[freeIdx] <- results[current]
                    freeIdx <- freeIdx+1
                    current <- current+1

            indexes.RemoveRange(freeIdx, n - freeIdx)
            results.RemoveRange(freeIdx, n - freeIdx)


    let mutable contextMenu = Stack<_>()
    let contextMenuResults = ObservableList<ContextMenuResultData> 20
    let mutable lastQueryText = ValueNone

    member this.ContextMenuResults = contextMenuResults
    member this.ContextMenuEnabled = contextMenu.Count <> 0
    member this.ContextMenuLoading =
        match contextMenu.TryPeek() with
        | false, _ -> false
        | true, { contents = Loading _ } -> true
        | true, { contents = SomeItemsLoaded _ } -> false

    member this.ExitContextMenu() =
        match contextMenu.TryPop() with
        | false, _ -> ()
        | true, c ->
            let loader =
                match c.Value with
                | Loading l
                | SomeItemsLoaded (l, _, _) -> l

            match loader with
            | :? System.IDisposable as d -> d.Dispose()
            | _ -> ()

    member this.SetContextMenu(loader: IContextMenuLoader) =
        try
            contextMenuResults.Clear()
            let state = ContextMenuState.Loading loader |> ref
            contextMenu.Push state

            loader.LoadItems(
                (fun itemCount ->
                    Dispatcher.UIThread.Post(
                        (fun () ->
                            state.Value <- ContextMenuState.SomeItemsLoaded (
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
                            insert state idx data 0
                            this.RefreshResults()
                        ),
                        DispatcherPriority.Background
                    )
                ),
                (fun exn idx ->
                    Dispatcher.UIThread.Post(
                        (fun () ->
                            let data = ContextMenuResultData.LoadFailed exn
                            insert state idx data 0
                            this.RefreshResults()
                        ),
                        DispatcherPriority.Background
                    )
                ),
                (fun () ->
                    Dispatcher.UIThread.Post(
                        (fun () ->
                            removeAllLoading state
                            this.RefreshResults()
                        ),
                        DispatcherPriority.Background
                    )
                )
            )
        with e ->
            Log.Error(e, "Failed to load context menu")

    member this.Query(text: string) =
        lastQueryText <- ValueSome text
        // stopwatch.Restart()
        contextMenuResults.Clear()

        match contextMenu.TryPeek() with
        | false, _ -> ()
        | true, { contents = Loading _ } -> ()
        | true, { contents = SomeItemsLoaded (_, _, results) } ->
            let normalizedText =
                text
                |> TextNormalization.String.normalize
                |> Array.map System.Text.Rune.ToLowerInvariant

            match text with
            | "" ->
                // Show all search engine results
                results
                |> Seq.filter (function
                    | ContextMenuResultData.Entry entry ->
                        entry.AccentuationMap <- null
                        true
                    | ContextMenuResultData.LoadFailed _ -> false
                    | _ -> true
                )
                :> _ seq
            | _ ->
                // Show matching results
                results
                |> Seq.filter (function
                    | ContextMenuResultData.Entry entry -> fuzzyMatchResult normalizedText entry
                    | _ -> false
                )
                |> Seq.sortWith contextMenuComparer
            |> contextMenuResults.AddRange

        contextMenuResults.NotifyChanged()

        // stopwatch.Stop()
        // stopwatch.Elapsed |> loadingTimes.OnNext

    member this.RefreshResults() =
        match lastQueryText with
        | ValueNone -> ()
        | ValueSome text -> this.Query(text)
