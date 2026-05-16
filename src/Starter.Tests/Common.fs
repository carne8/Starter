module Starter.Tests.Common

open System.Threading.Tasks
open R3
open Starter
open Starter.Features.PlatformInterop
open Starter.SearchEngine


module Mock =
    let platform =
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

    let staticSearchEngine id onLoadResults =
        { new IStaticSearchEngine with
            member this.LoadResults() = onLoadResults () |> ValueTask.FromResult
            member this.SearchResultSelected result = ()
            member this.Id = id
            member this.Name = $"Engine name: {id}"
            member this.ShortName = $"Engine short name: {id}"
            member this.Icon = StarterIconSource.Empty
            member this.Activators = Array.empty
            member this.add_Changed _ = ()
            member this.remove_Changed _ = ()
            member this.add_ResultsChanged _ = ()
            member this.remove_ResultsChanged _ = () }

    let dynamicSearchEngine id buffer onQueryResults =
        { new IDynamicSearchEngine with
            member this.SearchResultSelected result = ()
            member this.Search(query, ct, activator) = onQueryResults query ct activator, Observable.Empty()
            member this.Id = id
            member this.Name = $"Engine name: {id}"
            member this.ShortName = $"Engine short name: {id}"
            member this.Icon = StarterIconSource.Empty
            member this.Activators = Array.empty
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
