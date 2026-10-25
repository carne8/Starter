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

type TestStaticSearchEngine(
    args: {| Id: string
             OnLoadResults: unit -> SearchResult seq
             OnSearchResultSelected: SearchResult -> unit
             Activators: SearchEngineActivator seq |}
    ) =
    inherit StaticSearchEngine()

    override this.LoadResults() = args.OnLoadResults () |> ValueTask.FromResult
    override this.SearchResultSelected result = args.OnSearchResultSelected result
    override this.Id = this.Id
    override this.Name = $"Engine name: {this.Id}"
    override this.Icon = StarterIconSource.Empty
    override this.Activators = this.Activators |> Seq.toArray

module Mock =
    type MockSearchResult(name, ?priority, ?keywords, ?showIfNoActivator, ?activatorFilter, ?contextMenuLoader) =
        inherit SearchResult()

        override this.Id = name
        override this.Name = name
        override this.Description = name
        override this.Keywords = keywords |> Option.defaultValue null
        override this.Priority = priority |> Option.defaultValue ResultPriority.Static
        override this.Icon = StarterIconSource.Empty
        override this.ShowIfNoActivator = showIfNoActivator |> Option.defaultValue true
        override this.ActivatorFilter = activatorFilter |> Option.defaultValue null
        override this.ContextMenuLoader = contextMenuLoader |> Option.defaultValue null

    type MockActivator(name, engineId) =
        inherit SearchEngineActivator()

        override this.Id = name
        override this.SearchEngineId = engineId
        override this.Name = $"Activator name: {name}"
        override this.Icon = StarterIconSource.Empty

    type MockStaticSearchEngine(id, activators, onLoadResults, cb) =
        inherit StaticSearchEngine()

        override this.LoadResults() = onLoadResults () |> ValueTask.FromResult
        override this.SearchResultSelected result = cb result
        override this.Id = id
        override this.Name = $"Engine name: {id}"
        override this.Icon = StarterIconSource.Empty
        override this.Activators = activators |> Seq.toArray

    type MockDynamicSearchEngine(id, activators, buffer, onQueryResults, cb) =
        inherit DynamicSearchEngine()

        override this.SearchResultSelected result = cb result
        override this.Search(query, ct, activator) = onQueryResults query ct activator, Observable.Empty()
        override this.Id = id
        override this.Name = $"Engine name: {id}"
        override this.Icon = StarterIconSource.Empty
        override this.Activators = activators |> Seq.toArray
        override this.BufferResults = buffer


    // Create instances
    let platform () =
        { new IPlatformInterop with
            member this.IsLaunchAtStartupEnabled() = false
            member this.EnsureConfigCompatibility(config) = config
            member this.SupportBackground(background) = true
            member this.RegisterHotkey shortcut window = Task.FromResult true
            member this.SetupHotkeyCallback(window) = ()
            member this.ToggleLaunchAtStartup(enable) = ()
            member this.HotkeyRegistrable = true }

    /// Create a score database where saving to a file does nothing
    let scoreDb () =
        let scoreDb : IScoreDb = ScoreDb(Dictionary<_, _>(), Constants.ScoresMaxAging)

        { new IScoreDb with
            member this.GetResultScore(resultId) = scoreDb.GetResultScore(resultId)
            member this.IncreaseResultScore(resultId) = scoreDb.IncreaseResultScore(resultId)
            member this.RunMaxAgingPolicy() = scoreDb.RunMaxAgingPolicy()
            member this.SaveToFile(filePath) = Task.FromResult() }

    let searchResult name = MockSearchResult(name) :> SearchResult
    let searchResultWithPriority name priority = MockSearchResult(name, priority) :> SearchResult

    let searchResultWithActivator name showIfNoActivator activators =
        MockSearchResult(name, showIfNoActivator = showIfNoActivator, activatorFilter = activators)
         :> SearchResult

    let activator name engineId = MockActivator(name, engineId) :> SearchEngineActivator
    let staticSearchEngine id activators onLoadResults cb = MockStaticSearchEngine(id, activators, onLoadResults, cb) :> StaticSearchEngine
    let dynamicSearchEngine id activators buffer onQueryResults cb = MockDynamicSearchEngine(id, activators, buffer, onQueryResults, cb) :> DynamicSearchEngine

    let searchEngineStore (engines: SearchEngine seq) =
        let store = SearchEngineStore()
        engines |> Seq.iter (function
            | :? StaticSearchEngine as e -> store.AddSearchEngine e
            | :? DynamicSearchEngine as e -> store.AddSearchEngine e
            | other -> failwithf "Invalid search engine type: %A" other
        )
        store

    let private logger () =
        { new Serilog.ILogger with
            member this.Write(logEvent) = () }

    let launcher onUri onFile =
        { new Avalonia.Platform.Storage.ILauncher with
            member this.LaunchUriAsync(uri) = onUri uri; Task.FromResult true
            member this.LaunchFileAsync(storageItem) = onFile storageItem; Task.FromResult true }

    // Helpers
    let withWindowConfig config searchEngines test =
        use config = new BehaviorSubject<_>(config)
        use activatorStore = new ActivatorStore(config)
        searchEngines |> Seq.iter activatorStore.AddSearchEngineActivators

        let fuzzyMatchResults1, fuzzyMatchResults2 = Dictionary<_, _>(), Dictionary<_, _>()
        let vm = ViewModels.MainWindowViewModel(
            config,
            scoreDb (),
            searchEngineStore searchEngines,
            activatorStore,
            fuzzyMatchResults1,
            fuzzyMatchResults2
        )
        let window = Views.MainWindow(
            platform (),
            fuzzyMatchResults1,
            fuzzyMatchResults2,
            DataContext = vm
        )

        // Show window
        Dispatcher.UIThread.RunJobs() // Let window acknowledge about vm
        window.Show()
        Dispatcher.UIThread.RunJobs() // Let textbox grab focus

        test window vm

    let selectResult (window: Window) (result: SearchResult) =
        window.KeyTextInput (result.Name.ToLowerInvariant())
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.Show()
        for _ = 0 to result.Name.Length-1 do
            window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
            window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
