module Starter.Tests.Common

open System.Collections.Generic
open System.Threading.Tasks
open Avalonia.Controls
open Avalonia.Headless
open Avalonia.Input
open Avalonia.Threading
open NUnit.Framework
open R3
open Starter
open Starter.Features
open Starter.Features.PlatformInterop
open Starter.SearchEngine

type Assert = Legacy.ClassicAssert

type TestStaticSearchEngine =
    { Id: string
      OnLoadResults: unit -> ISearchResult seq
      OnSearchResultSelected: ISearchResult -> unit
      Activators: ISearchEngineActivator seq }

    interface IStaticSearchEngine with
        member this.LoadResults() = this.OnLoadResults () |> ValueTask.FromResult
        member this.SearchResultSelected result = this.OnSearchResultSelected result
        member this.Id = this.Id
        member this.Name = $"Engine name: {this.Id}"
        member this.ShortName = $"Engine short name: {this.Id}"
        member this.Icon = StarterIconSource.Empty
        member this.Activators = this.Activators |> Seq.toArray
        member this.add_Changed _ = ()
        member this.remove_Changed _ = ()
        member this.add_ResultsChanged _ = ()
        member this.remove_ResultsChanged _ = ()

module Helpers =
    let getControl<'a when 'a :> Control and 'a: not struct and 'a: not null> name (parent: Control) =
        let control: 'a | null = parent.FindControl<'a> name
        Assert.IsNotNull(control, $"Failed to get control named '{name}'")
        unbox<'a> control

module Mock =
    let platform () =
        { new IPlatformInterop with
            member this.IsLaunchAtStartupEnabled() = false
            member this.RegisterHotkey shortcut window = ValueTask.FromResult true
            member this.SetupHotkeyCallback(window) = ()
            member this.ToggleLaunchAtStartup(var0) = ()
            member this.HotkeyRegistrable = true }

    let searchResult name =
        { new ISearchResult with
           member this.Id = name
           member this.Name = name
           member this.Description = name
           member this.Keywords = null
           member this.Icon = StarterIconSource.Empty
           member this.ShowIfNoActivator = true
           member this.ActivatorFilter = Array.empty }

    let searchResultWithActivator name showIfNoActivator activators =
        { new ISearchResult with
           member this.Id = name
           member this.Name = name
           member this.Description = name
           member this.Keywords = null
           member this.Icon = StarterIconSource.Empty
           member this.ShowIfNoActivator = showIfNoActivator
           member this.ActivatorFilter = activators }

    let activator name engineId =
        { new ISearchEngineActivator with
            member this.Id = name
            member this.SearchEngineId = engineId
            member this.Name = $"Activator name: {name}"
            member this.ShortName = $"Activator short name: {name}"
            member this.Icon = StarterIconSource.Empty }

    let staticSearchEngine id activators onLoadResults =
        { new IStaticSearchEngine with
            member this.LoadResults() = onLoadResults () |> ValueTask.FromResult
            member this.SearchResultSelected result = ()
            member this.Id = id
            member this.Name = $"Engine name: {id}"
            member this.ShortName = $"Engine short name: {id}"
            member this.Icon = StarterIconSource.Empty
            member this.Activators = activators |> Seq.toArray
            member this.add_Changed _ = ()
            member this.remove_Changed _ = ()
            member this.add_ResultsChanged _ = ()
            member this.remove_ResultsChanged _ = () }

    let dynamicSearchEngine id activators buffer onQueryResults =
        { new IDynamicSearchEngine with
            member this.SearchResultSelected result = ()
            member this.Search(query, ct, activator) = onQueryResults query ct activator, Observable.Empty()
            member this.Id = id
            member this.Name = $"Engine name: {id}"
            member this.ShortName = $"Engine short name: {id}"
            member this.Icon = StarterIconSource.Empty
            member this.Activators = activators |> Seq.toArray
            member this.ResultsPriority = ResultPriority.Search
            member this.BufferResults = buffer
            member this.add_Changed _ = ()
            member this.remove_Changed _ = () }

    let searchEngineStore (engines: ISearchEngine seq) =
        let store = SearchEngineStore()
        engines |> Seq.iter (function
            | :? IStaticSearchEngine as e -> store.AddSearchEngine e
            | :? IDynamicSearchEngine as e -> store.AddSearchEngine e
            | other -> failwithf "Invalid search engine type: %A" other
        )
        store

    let withWindowConfig config searchEngines test =
        use config = new BehaviorSubject<_>(config)
        use activatorStore = new ActivatorStore(config)
        searchEngines |> Seq.iter activatorStore.AddSearchEngineActivators

        let vm = ViewModels.MainWindowViewModel(
            config,
            Dictionary<_, _>(),
            searchEngineStore searchEngines,
            activatorStore
        )
        let window = Views.MainWindow(platform (), DataContext = vm)

        // Show window
        Dispatcher.UIThread.RunJobs() // Let window acknowledge about vm
        window.Show()
        Dispatcher.UIThread.RunJobs() // Let textbox grab focus

        test window vm

    let selectResult (window: Window) (result: ISearchResult) =
        window.KeyTextInput (result.Name.ToLowerInvariant())
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.Show()
        for _ = 0 to result.Name.Length-1 do
            window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
            window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
