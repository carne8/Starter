namespace Starter.Features

open System.Threading
open System.Threading.Tasks
open System.ComponentModel
open System.Collections.Generic
open Avalonia.Threading
open FsToolkit.ErrorHandling
open Serilog
open Starter.Features.CustomCollections
open Starter.SearchEngine
open Starter.TextMatching

type ContextMenuStore(resultScoreDb) =
    let contextMenuComparer =
        fun e1 e2 ->
            compare
                (ContextMenuEntryData.getWeight resultScoreDb e1)
                (ContextMenuEntryData.getWeight resultScoreDb e2)

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


    let mutable cts = new CancellationTokenSource()
    let mutable contextMenuStack = Stack<struct {| Items: Task<List<_>>; Loader: obj |}>()
    let contextMenuResults = ObservableList<ContextMenuEntryData> 20
    let mutable lastQueryText = ValueNone

    let propertyChangedEvent = new Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    interface INotifyPropertyChanged with
        #nowarn 3261
        [<CLIEvent>]
        member _.PropertyChanged = propertyChangedEvent.Publish
        #warnon 3261

    member this.ContextMenuResults = contextMenuResults
    member this.ContextMenuEnabled = contextMenuStack.Count <> 0
    member this.ContextMenuLoading =
        match contextMenuStack.TryPeek() with
        | false, _ -> false
        | true, l -> not l.Items.IsCompleted

    member this.ExitContextMenu() =
        match contextMenuStack.TryPop() with
        | false, _ -> ()
        | true, c ->
            match c.Loader with
            | :? System.IDisposable as d -> d.Dispose()
            | _ -> ()
            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuEnabled))
            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuLoading))

    member this.SetContextMenu(platformHandle, loader: IContextMenuLoader) =
        try
            contextMenuResults.Clear()

            let rec items: Task<List<_>> =
                loader.LoadItems(platformHandle, fun newResult newResultIdx ->
                    if not items.IsCompletedSuccessfully then
                        Log.Error "Cannot insert new result in context menu before it is loaded."
                    else
                        newResult.OnLoaded.Subscribe(fun _ -> this.RefreshResults()) |> ignore
                        Dispatcher.UIThread.Post(
                            fun () ->
                                items.Result.Insert(
                                    newResultIdx,
                                    newResult |> ContextMenuEntryData.create
                                )
                                this.RefreshResults()
                            ,
                            DispatcherPriority.Background
                        )
                )
                |> Task.map (
                    Seq.map (fun entry ->
                        entry.OnLoaded.Subscribe(fun _ -> this.RefreshResults()) |> ignore
                        ContextMenuEntryData.create entry
                    )
                    >> List
                )

            contextMenuStack.Push(struct {| Items = items; Loader = loader |})
            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuEnabled))
            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuLoading))
        with e ->
            Log.Error(e, "Failed to load context menu")

    member this.Query(text: string) =
        cts.Cancel()
        cts <- new CancellationTokenSource()

        lastQueryText <- ValueSome text
        // stopwatch.Restart()

        match contextMenuStack.TryPeek() with
        | false, _ -> ()
        | true, c ->
            let normalizedText =
                text
                |> TextNormalization.String.normalize
                |> Array.map System.Text.Rune.ToLowerInvariant

            task {
                let! items = c.Items
                if cts.Token.IsCancellationRequested then () else

                let newResults =
                    match text with
                    | "" ->
                        // Show all search engine results
                        let r = new List<_>(items.Count)

                        let mutable lastTookIsSeparator = true // Prevent starting with a separator
                        for i = 0 to items.Count-1 do
                            let entry = items[i]
                            let v =
                                not entry.IsFailed &&
                                (not entry.IsSeparator
                                || entry.IsSeparator && not lastTookIsSeparator)

                            if v then
                                entry.AccentuationMap <- null
                                lastTookIsSeparator <- entry.IsSeparator
                                r.Add entry

                        while r.Count > 0 && r[r.Count-1].IsSeparator do // Remove ending separator
                            r.RemoveAt(r.Count-1)

                        r :> _ seq
                    | _ ->
                        // Show matching results
                        items
                        |> Seq.filter (fun entry ->
                            entry.Result.Status = ContextMenuStatus.Loaded
                            && fuzzyMatchResult normalizedText entry
                        )
                        |> Seq.sortWith contextMenuComparer

                Dispatcher.UIThread.Post(
                    fun () ->
                        contextMenuResults.Clear()
                        contextMenuResults.AddRange newResults
                        contextMenuResults.NotifyChanged()
                        propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuLoading))
                    ,
                    DispatcherPriority.Loaded
                )
            } |> ignore

        // stopwatch.Stop()
        // stopwatch.Elapsed |> loadingTimes.OnNext

    member this.RefreshResults() =
        match lastQueryText with
        | ValueNone -> ()
        | ValueSome text -> this.Query(text)
