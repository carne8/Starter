module Starter.Tests.ContextMenu

open System.Collections.ObjectModel
open System.ComponentModel
open System.Threading.Tasks
open Avalonia.Controls
open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Threading
open Starter.Features.Config
open Starter.SearchEngine
open Starter.Tests.Common

type ContextMenuEntry(name, invoke) =
    let mutable status = ContextMenuStatus.Loading
    let propertyChangedEvent = Event<_, _>()

    member this.SetStatus(newStatus) =
        status <- newStatus
        let entry = (this :> IContextMenuEntry)
        propertyChangedEvent.Trigger(
            this,
            PropertyChangedEventArgs(nameof entry.Status)
        )

    interface IContextMenuEntry with
        member this.Invoke() = invoke ()
        member this.Status = status
        member this.Id = null
        member this.Name = name
        member this.Description = name
        member this.Keywords = null
        member this.Icon = StarterIconSource.Empty

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChangedEvent.Publish

module Helpers =
    let createLoadedContextMenuEntries args =
        args
        |> List.map (fun arg ->
            let entry = ContextMenuEntry arg
            entry.SetStatus ContextMenuStatus.Loaded
            entry :> IContextMenuEntry
        )
        |> List.toArray

    let createContextMenuLoader loadItems =
        { new IContextMenuLoader with
            member this.LoadItems(platformHandle) = loadItems () }

[<AvaloniaTest>]
let selectContextMenuEntry () =
    // Tests:
    // - Try to open context menu when the search result does not provide any
    // - Open a context menu when the search result does provide one
    // - Instant context menu opening when task is already completed
    // - Select a context menu entry
    // - Ensure context menu is exited after invocation.
    // - Select a failing context menu entry
    // - Escape context menu and restore query and selection

    let mutable resultInvoked = false

    let contextMenuEntries =
        Helpers.createLoadedContextMenuEntries
            [ "Result A", fun () -> resultInvoked <- true; null
              "Result B", fun () -> failwith "Expected fail" ]

    let cmLoader =
        Helpers.createContextMenuLoader (fun () ->
            contextMenuEntries
            |> ObservableCollection
            |> Task.FromResult
        )

    let results =
        [| Mock.searchResultWithContextMenu "Result A" null
           Mock.searchResultWithContextMenu "Result B" cmLoader |]

    let engines =
        [ Mock.staticSearchEngine "static" [] (fun () -> results) ignore
          :> SearchEngine ]

    Mock.withWindowConfig Configuration.Default engines (fun window vm ->
        let query = "result"
        window.KeyTextInput query

        // Select first result (should not be able to open context menu)
        window.PressTab()
        Assert.IsFalse(vm.ContextMenu.ContextMenuEnabled, "Context menu should be as first result does not provide a context menu.")

        // Select second result (context menu should open and not be loading)
        window.PressDown()
        window.PressTab()
        Assert.IsTrue(vm.ContextMenu.ContextMenuEnabled, "Context menu should be enabled.")
        Assert.IsFalse(vm.ContextMenu.ContextMenuLoading, "Context menu should not be loading.")
        Assert.IsTrue(window.ContextMenuResultList.IsVisible, "Context menu result list should be visible.")
        Assert.AreEqual(contextMenuEntries.Length, window.ContextMenuResultList.ItemCount, "Context menu results should all be displayed.")

        // Select Result A
        Assert.AreEqual(contextMenuEntries[0], window.ContextMenuResultList.SelectedItem, "First context menu entry should be selected.")
        Assert.IsFalse(resultInvoked, "Result should not have been invoked yet.")
        window.PressEnter()
        Assert.IsTrue(resultInvoked, "Result should have been invoked.")
        Assert.IsFalse(window.IsVisible, "Window should have been hidden.")

        // Ensure context menu exited
        window.Show()
        Assert.IsFalse(vm.ContextMenu.ContextMenuEnabled, "Context menu should be disabled after invocation.")
        Assert.IsFalse(vm.ContextMenu.ContextMenuLoading, "Context menu should not be loading after invocation.")
        Assert.IsFalse(window.ContextMenuResultList.IsVisible, "Context menu should not be visible after invocation as it is disabled.")
        Assert.IsTrue(window.ResultList.IsVisible, "Search result list should be visible.")
        Dispatcher.UIThread.RunJobs();
        Assert.AreEqual(query, window.TextBox.SelectedText, "Query should be fully selected.")

        // Select Result B
        window.KeyTextInput "result"
        Assert.AreEqual(results[1], window.ResultList.SelectedItem, "Result B should be selected as it is the last one used.")
        window.PressTab()
        window.PressDown()
        Assert.AreEqual(1, window.ContextMenuResultList.SelectedIndex, "Second context menu entry should be selected.")
        Assert.AreEqual(contextMenuEntries[1], window.ContextMenuResultList.SelectedItem, "Second context menu entry should be selected.")
        Assert.DoesNotThrow(
            (fun () -> window.PressEnter()),
            "Starter should not throw when context menu entry fails invocation."
        )

        // Test exiting context menu
        window.PressEscape()
        Assert.AreEqual(query, window.TextBox.Text, "After exiting context menu, query should have been restored.")
        Assert.AreEqual(results[1], window.ResultList.SelectedItem, "After exiting context menu, selection should have been restored.")
    )

[<AvaloniaTest>]
let openSubContextMenuAndExit () =
    // Tests:
    // - Open sub context menu
    // - Escape context menu and restore query and selection
    // - Select a context menu entry

    let mutable resultInvoked = false

    let subContextMenuEntries =
        [ "Result A.2", fun () -> resultInvoked <- true; null ]
        |> Helpers.createLoadedContextMenuEntries

    let subContextMenuLoader =
        Helpers.createContextMenuLoader (fun () ->
            subContextMenuEntries
            |> ObservableCollection
            |> Task.FromResult
        )

    let contextMenuEntries =
        [ "Result A.1", fun () -> null
          "Result A.1 with sub menu", fun () -> subContextMenuLoader ]
        |> Helpers.createLoadedContextMenuEntries

    let contextMenuLoader =
        Helpers.createContextMenuLoader (fun () ->
            contextMenuEntries
            |> ObservableCollection
            |> Task.FromResult
        )

    let results =
        [| Mock.searchResultWithContextMenu "Result A.0" contextMenuLoader |]

    let engines =
        [ Mock.staticSearchEngine "static" [] (fun () -> results) ignore
          :> SearchEngine ]

    Mock.withWindowConfig Configuration.Default engines (fun window vm ->
        window.KeyTextInput "result"

        // Open first context menu
        window.PressTab()
        Assert.IsTrue(vm.ContextMenu.ContextMenuEnabled, "Context menu should be opened.")
        Assert.IsFalse(vm.ContextMenu.ContextMenuLoading, "Context menu should not be loading.")
        Assert.IsTrue(window.ContextMenuResultList.IsVisible, "Context menu result list should be visible.")
        Assert.AreEqual(contextMenuEntries.Length, window.ContextMenuResultList.ItemCount, "Context menu results should all be displayed.")

        // Open sub menu
        window.KeyTextInput "Resu"
        window.PressDown()
        window.PressTab()
        Assert.IsTrue(vm.ContextMenu.ContextMenuEnabled, "Sub context menu should be opened.")
        Assert.IsFalse(vm.ContextMenu.ContextMenuLoading, "Sub context menu should not be loading.")
        Assert.IsTrue(window.ContextMenuResultList.IsVisible, "Sub context menu result list should be visible.")
        Assert.AreEqual(subContextMenuEntries.Length, window.ContextMenuResultList.ItemCount, "Sub context menu results should all be displayed.")

        // Exit sub context menu
        window.PressEscape()
        Assert.AreEqual("Resu", window.TextBox.Text, "After exiting sub context menu, query should have been restored.")
        Assert.AreEqual(contextMenuEntries[1], window.ContextMenuResultList.SelectedItem, "After exiting sub context menu, selection should have been restored.")

        // Exit context menu
        window.PressEscape()
        Assert.AreEqual("result", window.TextBox.Text, "After exiting context menu, query should have been restored.")
        Assert.AreEqual(results[0], window.ResultList.SelectedItem, "After exiting context menu, selection should have been restored.")

        // Select sub context menu entry
        window.PressTab()
        window.PressDown()
        window.PressTab()
        window.PressEnter()
        Assert.IsTrue(resultInvoked, "Result should have been invoked.")
        Assert.IsFalse(window.IsVisible, "Window should have been hidden.")

        // Ensure context menu exited
        window.Show()
        Assert.IsFalse(vm.ContextMenu.ContextMenuEnabled, "Context menu should be disabled after invocation.")
        Assert.IsFalse(vm.ContextMenu.ContextMenuLoading, "Context menu should not be loading after invocation.")
        Assert.IsFalse(window.ContextMenuResultList.IsVisible, "Context menu should not be visible after invocation as it is disabled.")
        Assert.IsTrue(window.ResultList.IsVisible, "Search result list should be visible.")
        Dispatcher.UIThread.RunJobs();
        Assert.AreEqual("result", window.TextBox.SelectedText, "Query should be fully selected.")
    )

[<AvaloniaTest>]
let openAsyncContextMenu_noQuery () =
    // Tests:
    // - Open loading context menu -> checks loading
    // - On loaded -> checks results are visible
    // - Gradually set results loaded

    let entries =
        [| ContextMenuEntry("Entry 0", fun () -> null)
           ContextMenuEntry("Entry 1", fun () -> null)
           ContextMenuEntry("Entry 2", fun () -> null)
           ContextMenuEntry("Entry 3", fun () -> null) |]

    let observableCollection =
        entries
        |> Seq.cast<IContextMenuEntry>
        |> ObservableCollection

    let tcs = TaskCompletionSource<_>()
    let contextMenuLoader = Helpers.createContextMenuLoader (fun () -> tcs.Task)

    let results = [| Mock.searchResultWithContextMenu "Result" contextMenuLoader |]
    let engines =
        [ Mock.staticSearchEngine "static" [] (fun () -> results) ignore
          :> SearchEngine ]

    let findControlFromEntry (window: Starter.Views.MainWindow) (entry: ContextMenuEntry) =
        window.ContextMenuResultList.ItemsPanelRoot.Children
        |> Seq.map (fun c ->
            (c :?> ListBoxItem).Presenter.Child
            :?> Starter.Controls.ContextMenuResultView
        )
        |> Seq.tryFind (fun e -> e.DataContext = entry)
        |> function
            | None -> failwithf "Failed to find control associated with %A" entry
            | Some control -> control

    Mock.withWindowConfig Configuration.Default engines (fun window vm ->
        // Open context menu
        window.KeyTextInput "result"
        window.PressTab()
        Assert.IsTrue(vm.ContextMenu.ContextMenuEnabled, "Context menu should be opened.")
        Assert.IsTrue(vm.ContextMenu.ContextMenuLoading, "Context menu should be loading.")
        Assert.IsFalse(window.ContextMenuResultList.IsVisible, "Context menu entries list should not be displayed while loading.")

        // Set context menu as loaded
        tcs.SetResult observableCollection
        Dispatcher.UIThread.RunJobs()

        Assert.IsTrue(vm.ContextMenu.ContextMenuEnabled, "Context menu should be opened.")
        Assert.IsFalse(vm.ContextMenu.ContextMenuLoading, "Context menu should not be loading anymore.")
        Assert.IsTrue(window.ContextMenuResultList.IsVisible, "Context menu entries list should be displayed.")
        Assert.AreEqual(entries.Length, window.ContextMenuResultList.ItemCount, "Context menu results should all be displayed.")

        // Ensure all results are displayed as loading
        entries |> Seq.iter (fun entry ->
            let control = findControlFromEntry window entry
            Assert.IsTrue(control.LoadingControl.IsVisible, "Context menu update not reflected on UI.")
            Assert.IsFalse(control.LoadedControl.IsVisible, "Context menu update not reflected on UI.")
            Assert.IsFalse(control.SeparatorControl.IsVisible, "Context menu update not reflected on UI.")
            Assert.IsFalse(control.FailedControl.IsVisible, "Context menu update not reflected on UI.")
        )

        // Set entry 0 as loaded
        entries[0].SetStatus ContextMenuStatus.Loaded
        let control = findControlFromEntry window entries[0]
        Assert.IsFalse(control.LoadingControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsTrue(control.LoadedControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.SeparatorControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.FailedControl.IsVisible, "Context menu update not reflected on UI.")

        // Set entry 1 as separator
        entries[1].SetStatus ContextMenuStatus.Separator
        let control = findControlFromEntry window entries[1]
        Assert.IsFalse(control.LoadingControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.LoadedControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsTrue(control.SeparatorControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.FailedControl.IsVisible, "Context menu update not reflected on UI.")

        // Set entry 2 as loaded
        entries[2].SetStatus ContextMenuStatus.Failed
        let control = findControlFromEntry window entries[2]
        Assert.IsFalse(control.LoadingControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.LoadedControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.SeparatorControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsTrue(control.FailedControl.IsVisible, "Context menu update not reflected on UI.")

        // Remove entry 2
        observableCollection.Remove entries[2] |> ignore
        Dispatcher.UIThread.RunJobs()
        Assert.IsTrue(
            Seq.forall2
                (=)
                observableCollection
                (Seq.cast<IContextMenuEntry> window.ContextMenuResultList.Items),
            "Context menu results should all be displayed."
        )

        // Move 0 to 2 and 3 to 1
        observableCollection.Move(
            observableCollection.IndexOf entries[0],
            2
        )
        observableCollection.Move(
            observableCollection.IndexOf entries[3],
            1
        )
        Dispatcher.UIThread.RunJobs()
        Assert.IsTrue(
            Seq.forall2
                (=)
                observableCollection
                (Seq.cast<IContextMenuEntry> window.ContextMenuResultList.Items),
            "Context menu results should all be displayed and correctly ordered."
        )

        // Add entry 2
        observableCollection.Insert(3, entries[2])
        Dispatcher.UIThread.RunJobs()
        Assert.IsTrue(
            Seq.forall2
                (=)
                observableCollection
                (Seq.cast<IContextMenuEntry> window.ContextMenuResultList.Items),
            "Context menu results should all be displayed and correctly ordered."
        )

        // Reset collection
        observableCollection.Clear()
        entries |> Seq.iter observableCollection.Add
        Dispatcher.UIThread.RunJobs()
        Assert.IsTrue(
            Seq.forall2
                (=)
                observableCollection
                (Seq.cast<IContextMenuEntry> window.ContextMenuResultList.Items),
            "Context menu results should all be displayed and correctly ordered."
        )
    )

[<AvaloniaTest>]
let openAsyncContextMenu_withQuery () =
    // Tests:
    // - Open loading context menu -> checks loading
    // - Write query
    // - On loaded -> checks matching results are visible
    // - Gradually set results loaded

    let entries =
        [| ContextMenuEntry("Entry aaaa", fun () -> null)
           ContextMenuEntry("Entry aaab", fun () -> null)
           ContextMenuEntry("Entry abab", fun () -> null)
           ContextMenuEntry("Entry bbbb", fun () -> null) |]

    let observableCollection =
        entries
        |> Seq.cast<IContextMenuEntry>
        |> ObservableCollection

    let tcs = TaskCompletionSource<_>()
    let contextMenuLoader = Helpers.createContextMenuLoader (fun () -> tcs.Task)

    let results = [| Mock.searchResultWithContextMenu "Result" contextMenuLoader |]
    let engines =
        [ Mock.staticSearchEngine "static" [] (fun () -> results) ignore
          :> SearchEngine ]

    let findControlFromEntry (window: Starter.Views.MainWindow) (entry: ContextMenuEntry) =
        window.ContextMenuResultList.ItemsPanelRoot.Children
        |> Seq.map (fun c ->
            (c :?> ListBoxItem).Presenter.Child
            :?> Starter.Controls.ContextMenuResultView
        )
        |> Seq.tryFind (fun e -> e.DataContext = entry)
        |> function
            | None -> failwithf "Failed to find control associated with %A" entry
            | Some control -> control

    Mock.withWindowConfig Configuration.Default engines (fun window vm ->
        // Open context menu
        window.KeyTextInput "result"
        window.PressTab()
        Assert.IsTrue(vm.ContextMenu.ContextMenuEnabled, "Context menu should be opened.")
        Assert.IsTrue(vm.ContextMenu.ContextMenuLoading, "Context menu should be loading.")
        Assert.IsFalse(window.ContextMenuResultList.IsVisible, "Context menu entries list should not be displayed while loading.")

        // Enter query
        window.KeyTextInput "ab" // Result 2 and 1 match

        // Set context menu as loaded
        tcs.SetResult observableCollection
        Dispatcher.UIThread.RunJobs()

        Assert.IsTrue(vm.ContextMenu.ContextMenuEnabled, "Context menu should be opened.")
        Assert.IsFalse(vm.ContextMenu.ContextMenuLoading, "Context menu should not be loading anymore.")
        Assert.IsTrue(window.ContextMenuResultList.IsVisible, "Context menu entries list should be displayed.")
        Assert.AreEqual(0, window.ContextMenuResultList.ItemCount, "No context menu results should be displayed as they are all loading.")

        // Set entry 0 as loaded
        entries[0].SetStatus ContextMenuStatus.Loaded
        Assert.AreEqual(0, window.ContextMenuResultList.ItemCount, "A non matching context menu results is displayed and should not.")

        // Set entry 3 as loaded
        entries[3].SetStatus ContextMenuStatus.Loaded
        Assert.AreEqual(0, window.ContextMenuResultList.ItemCount, "A non matching context menu results is displayed and should not.")

        // Set entry 1 as loaded
        entries[1].SetStatus ContextMenuStatus.Loaded
        Dispatcher.UIThread.RunJobs()
        let control = findControlFromEntry window entries[1]
        Assert.IsFalse(control.LoadingControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsTrue(control.LoadedControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.SeparatorControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.FailedControl.IsVisible, "Context menu update not reflected on UI.")

        // Set entry 2 as loaded
        entries[2].SetStatus ContextMenuStatus.Failed
        Dispatcher.UIThread.RunJobs()
        Assert.Throws(
            (fun () -> findControlFromEntry window entries[2] |> ignore),
            "Context menu entry should not be displayed as it is failed."
        ) |> ignore

        // Remove entry 2 and readd loaded
        observableCollection.Remove entries[2] |> ignore
        Dispatcher.UIThread.RunJobs()

        entries[2].SetStatus ContextMenuStatus.Loading
        observableCollection.Add entries[2]
        Dispatcher.UIThread.RunJobs()

        entries[2].SetStatus ContextMenuStatus.Loaded
        Dispatcher.UIThread.RunJobs()
        let control = findControlFromEntry window entries[2]
        Assert.IsFalse(control.LoadingControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsTrue(control.LoadedControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.SeparatorControl.IsVisible, "Context menu update not reflected on UI.")
        Assert.IsFalse(control.FailedControl.IsVisible, "Context menu update not reflected on UI.")
    )
