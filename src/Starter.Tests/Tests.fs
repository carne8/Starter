module Starter.Tests.Tests

open R3
open System.Threading.Tasks
open Avalonia.Controls
open Avalonia.Headless
open Avalonia.Threading
open Avalonia.Headless.XUnit

open Starter
open Starter.Features
open Starter.Features.Config
open Starter.Features.PlatformInterop
open Starter.SearchEngine
open Starter.Tests.Common

type Assert = XunitAssertMessages.AssertM

[<AvaloniaFact>]
let ensureHotkeyRegistration () =
    let mutable hotkeyRegistered = false
    let mutable callbackRegistered = None

    let platform =
        { new IPlatformInterop with
            member this.IsLaunchAtStartupEnabled() = false
            member this.RegisterHotkey shortcut window =
                hotkeyRegistered <- true
                ValueTask.FromResult true
            member this.SetupHotkeyCallback(window) = callbackRegistered <- Some window
            member this.ToggleLaunchAtStartup(var0) = ()
            member this.HotkeyRegistrable = true }

    use config = new BehaviorSubject<_>(Configuration.Default)
    use activatorStore = new ActivatorStore(config)
    let vm = ViewModels.MainWindowViewModel(
        config,
        dict [],
        SearchEngineStore(),
        activatorStore
    )
    let window = Views.MainWindow(platform, DataContext = vm)

    Dispatcher.UIThread.RunJobs();

    Assert.True(hotkeyRegistered, "Hotkey should be registered")
    Assert.True(callbackRegistered.IsSome, "Hotkey callback should be registered")
    Assert.True(callbackRegistered.Value = window, "Window of the callback should be the calling window")

[<AvaloniaFact>]
let ensureHotkeyNotRegisteredWhenNotRegistrable () =
    let mutable hotkeyRegistered = false
    let mutable callbackRegistered = None

    let platform =
        { new IPlatformInterop with
            member this.IsLaunchAtStartupEnabled() = false
            member this.RegisterHotkey shortcut window =
                hotkeyRegistered <- true
                ValueTask.FromResult true
            member this.SetupHotkeyCallback(window) = callbackRegistered <- Some window
            member this.ToggleLaunchAtStartup(var0) = ()
            member this.HotkeyRegistrable = false }

    use config = new BehaviorSubject<_>(Configuration.Default)
    use activatorStore = new ActivatorStore(config)
    let vm = ViewModels.MainWindowViewModel(
        config,
        dict [],
        Mock.searchEngineStore [],
        activatorStore
    )
    Views.MainWindow(platform, DataContext = vm) |> ignore

    Dispatcher.UIThread.RunJobs();

    Assert.False(hotkeyRegistered, "Hotkey should not be registered")
    Assert.True(callbackRegistered.IsSome, "Hotkey callback should be registered")


[<AvaloniaFact>]
let ensureSearchResultsAppear () =
    let mutable called = [| false; false; false |]
    let results =
        [| Array.init 5 (fun i -> Mock.searchResult $"Result: {i}")
           Array.init 5 (fun i -> Mock.searchResult $"Dynamic Result: {i}")
           Array.init 5 (fun i -> Mock.searchResult $"Dynamic buffered Result: {i}") |]

    let engines = [
        Mock.staticSearchEngine "static" (fun () -> called[0] <- true; results[0]) :> ISearchEngine
        Mock.dynamicSearchEngine "dynamic" false (fun _ _ _ -> called[1] <- true; results[1])
        Mock.dynamicSearchEngine "buffered" true (fun _ _ _ -> called[2] <- true; results[2])
    ]

    let platform = Mock.platform
    use config = new BehaviorSubject<_>(Configuration.Default)
    use activatorStore = new ActivatorStore(config)
    let vm = ViewModels.MainWindowViewModel(config, dict [], Mock.searchEngineStore engines, activatorStore)
    let window = Views.MainWindow(platform, DataContext = vm)

    // Assert static engines have been called
    engines |> List.iteri (fun idx engine ->
        match engine with
        | :? IStaticSearchEngine ->
            Assert.True(called[idx], $"Static search engine '{engine.Id}' should have been called")
        | :? IDynamicSearchEngine ->
            Assert.False(called[idx], $"Dynamic search engine '{engine.Id}' should have been called")
        | other -> failwithf "Unexpected search engine type: %A" other
    )

    // Show window
    Dispatcher.UIThread.RunJobs()
    window.Show()

    // Assert text box is focused
    let textBox =
        let c = window.FindControl<TextBox>("TextBox")
        Assert.NotNull c
        unbox<TextBox> c
    Dispatcher.UIThread.RunJobs()
    Assert.True(textBox.IsFocused)

    // Type some text
    window.KeyTextInput("result")
    Assert.Equal(textBox.Text, "result")

    // Assert engines have been called
    engines |> List.iteri (fun idx engine ->
        Assert.True(called[idx], $"Engine '{engine.Id}' should have been called")
    )

    // Assert results are shown
    let resultList =
        let control = window.FindControl<ListBox>("ResultList")
        Assert.NotNull(control)
        unbox<ListBox> control

    Assert.Equal(
        results |> Array.sumBy Array.length,
        resultList.ItemCount,
        "All results should be displayed"
    )
    results |> Array.iter (Array.iter (fun result ->
        let resultIsPresent =
            resultList.Items |> Seq.exists (fun r ->
                r
                |> unbox<SearchResultData>
                |> _.SearchResult = result
            )
        Assert.True(resultIsPresent, $"{result.Name} should be displayed")
    ))
