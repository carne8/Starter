namespace Starter.Features

open System.Threading
open System.Threading.Tasks
open System.ComponentModel
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Collections.Specialized
open Avalonia.Threading
open Serilog
open Starter.Features.CustomCollections
open Starter.SearchEngine
open Starter.TextMatching
open FsToolkit.ErrorHandling

type ContextMenuStore(resultScoreDb, fuzzyMatchResults: Dictionary<IContextMenuEntry, FuzzyMatch.FuzzyResult>) as this =

    let contextMenuComparer =
        fun e1 e2 ->
            let f1, f2 =
                fuzzyMatchResults.TryGetValue e1 |> ValueOption.ofPair,
                fuzzyMatchResults.TryGetValue e2 |> ValueOption.ofPair

            compare
                (ContextMenuEntryHelpers.getWeight resultScoreDb f1 e1)
                (ContextMenuEntryHelpers.getWeight resultScoreDb f2 e2)

    let slab = Memory.Slab.createDefault()
    let fuzzyMatchResult normalizedText (result: IContextMenuEntry) =
        let res = FuzzyMatch.string false true true slab normalizedText result.Name

        match res with
        | ValueSome fusilResult when fusilResult.Score > 0s ->
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


    let syncCollections (source: ObservableCollection<_>) (target: ObservableList<_>) (ct: CancellationToken) = lock source (fun () ->
        source.CollectionChanged.Subscribe(fun evt ->
            Dispatcher.UIThread.Post(
                fun () ->
                    match evt.Action with
                    | NotifyCollectionChangedAction.Add ->
                        let newItem = (nonNull evt.NewItems)[0] |> unbox
                        target.Insert(evt.NewStartingIndex, newItem)

                    | NotifyCollectionChangedAction.Move ->
                        let newItem = (nonNull evt.NewItems)[0] |> unbox
                        target.RemoveAt evt.OldStartingIndex
                        target.Insert(evt.NewStartingIndex, newItem)

                    | NotifyCollectionChangedAction.Remove ->
                        let oldItem = (nonNull evt.OldItems)[0] |> unbox
                        target.Remove oldItem |> ignore

                    | NotifyCollectionChangedAction.Replace ->
                        let oldItem = (nonNull evt.OldItems)[0] |> unbox
                        let oldItemIdx = target.FindIndex oldItem
                        let newItem = (nonNull evt.NewItems)[0] |> unbox
                        target.RemoveAt oldItemIdx
                        target.Insert(oldItemIdx, newItem)

                    | NotifyCollectionChangedAction.Reset ->
                        target.Clear()
                        match evt.NewItems with
                        | null -> ()
                        | newItems ->
                            newItems
                            |> unbox
                            |> target.AddRange

                    | _ -> ()

                    target.NotifyChanged()
                ,
                DispatcherPriority.Loaded
            )
        )
        |> _.Dispose
        |> ct.Register
        |> ignore

        target.Clear()
        target.AddRange source
        target.NotifyChanged()
    )

    let syncCollectionsWithQuery query (source: ObservableCollection<IContextMenuEntry>) (target: ObservableList<_>) (ct: CancellationToken) = lock source (fun () ->
        source.CollectionChanged.Subscribe(fun evt ->
            Dispatcher.UIThread.Post(
                fun () ->
                    match evt.Action with
                    | NotifyCollectionChangedAction.Add ->
                        let newItem = (nonNull evt.NewItems)[0] |> unbox<IContextMenuEntry>
                        if newItem.Status = ContextMenuStatus.Loaded &&
                           fuzzyMatchResult query newItem then
                            let insertIdx = target.BinarySearch(newItem, Comparer.Create contextMenuComparer)
                            if insertIdx < 0 then target.Insert(~~~insertIdx, newItem)

                    | NotifyCollectionChangedAction.Move -> ()

                    | NotifyCollectionChangedAction.Remove ->
                        let oldItem = (nonNull evt.OldItems)[0] |> unbox
                        target.Remove oldItem |> ignore

                    | NotifyCollectionChangedAction.Replace ->
                        let oldItem = (nonNull evt.OldItems)[0] |> unbox
                        let oldItemIdx = target.FindIndex oldItem
                        let newItem = (nonNull evt.NewItems)[0] |> unbox
                        target.RemoveAt oldItemIdx

                        let insertIdx = target.BinarySearch(newItem, Comparer.Create contextMenuComparer)
                        if insertIdx < 0 then target.Insert(~~~insertIdx, newItem)

                    | NotifyCollectionChangedAction.Reset ->
                        target.Clear()
                        evt.NewItems
                        |> nonNull
                        |> unbox<IContextMenuEntry seq>
                        |> Seq.filter (fun entry -> entry.Status = ContextMenuStatus.Loaded && fuzzyMatchResult query entry)
                        |> Seq.sortWith contextMenuComparer
                        |> target.AddRange

                    | _ -> ()

                    target.NotifyChanged()
                ,
                DispatcherPriority.Loaded
            )
        )
        |> _.Dispose
        |> ct.Register
        |> ignore

        target.Clear()
        source
        |> Seq.filter (fun entry ->
            let pass = entry.Status = ContextMenuStatus.Loaded && fuzzyMatchResult query entry

            // Listens updates to refresh
            match entry with
            | :? INotifyPropertyChanged as e ->
                e.PropertyChanged.Subscribe(fun _ ->
                    Dispatcher.UIThread.Post(this.RefreshResults, DispatcherPriority.Loaded)
                )
                |> _.Dispose
                |> ct.Register
                |> ignore
            | _ -> ()

            pass
        )
        |> Seq.sortWith contextMenuComparer
        |> target.AddRange
        target.NotifyChanged()
    )


    let mutable cts = new CancellationTokenSource()
    let mutable contextMenuStack = Stack<struct {| Items: Task<ObservableCollection<_>>; Loader: obj |}>()
    let contextMenuResults = ObservableList<IContextMenuEntry> 20
    let mutable lastQueryText = ValueNone

    let propertyChangedEvent = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChangedEvent.Publish

    member this.ContextMenuResults = contextMenuResults
    member this.ContextMenuEnabled = contextMenuStack.Count <> 0
    member this.ContextMenuLoading =
        match contextMenuStack.TryPeek() with
        | false, _ -> false
        | true, l -> not l.Items.IsCompleted

    member _.FuzzyMatchResults = fuzzyMatchResults

    member this.ExitContextMenu() =
        match contextMenuStack.TryPop() with
        | false, _ -> ()
        | true, c ->
            match c.Loader with
            | :? System.IDisposable as d -> d.Dispose()
            | _ -> ()
            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuEnabled))
            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuLoading))

    member this.ExitAllContextMenu() =
        contextMenuStack |> Seq.iter (fun c ->
            match c.Loader with
            | :? System.IDisposable as d -> d.Dispose()
            | _ -> ()
        )
        contextMenuStack.Clear()
        propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuEnabled))
        propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuLoading))

    member this.SetContextMenu(platformHandle, loader: IContextMenuLoader) =
        try
            contextMenuResults.Clear()

            let items = loader.LoadItems(platformHandle)
            contextMenuStack.Push(struct {| Items = items; Loader = loader |})

            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuEnabled))
            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuLoading))
        with e ->
            Log.Error(e, "Failed to load context menu")

    member this.Query(text: string) =
        cts.Cancel()
        cts <- new CancellationTokenSource()
        let ct = cts.Token

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
                if ct.IsCancellationRequested then () else

                match text with
                | "" -> // Show all results in given order
                    Dispatcher.UIThread.Post(
                        fun () ->
                            fuzzyMatchResults.Clear()
                            syncCollections items contextMenuResults ct
                            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuEnabled))
                            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuLoading))
                        ,
                        DispatcherPriority.Loaded
                    )
                | _ -> // Show matching results
                    Dispatcher.UIThread.Post(
                        fun () ->
                            syncCollectionsWithQuery normalizedText items contextMenuResults ct
                            propertyChangedEvent.Trigger(null, PropertyChangedEventArgs(nameof this.ContextMenuEnabled))
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
