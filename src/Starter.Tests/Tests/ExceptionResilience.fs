module Starter.Tests.ExceptionResilience

open System
open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Input
open Starter.Features.Config
open Starter.SearchEngine
open Starter.Tests.Common

[<AvaloniaTest>]
let testStaticSearchEngineCrash_OnLoadResults () =
    let results1 =
        [| Mock.searchResult "Result 0/1"
           Mock.searchResult "Result 0/2"
           Mock.searchResult "Result 0/3"
           Mock.searchResult "Result 0/4" |]
    let results2 =
        [| Mock.searchResult "Result 1/1"
           Mock.searchResult "Result 1/2"
           Mock.searchResult "Result 1/3"
           Mock.searchResult "Result 1/4" |]

    let searchEngines : ISearchEngine array =
        [| Mock.staticSearchEngine "crashing-engine-id" [] (fun () -> failwith "Sorry, but not sorry")
           Mock.staticSearchEngine "engine-id-1" [] (fun () -> results1)
           Mock.dynamicSearchEngine "engine-id-2" [] false (fun _ _ _ -> results2) |]

    Assert.DoesNotThrow(
        Action(fun () ->
            SearchResultFiltering.testDisplayedResults
                (Array.append results1 results2)
                [||]
                searchEngines
                Configuration.Default
                (fun window -> window.KeyTextInput "Result")
        ),
        "Starter should not throw when an engine throws."
    )

[<AvaloniaTest>]
let testDynamicSearchEngineCrash_OnLoadResults () =
    let results1 =
        [| Mock.searchResult "Result 0/1"
           Mock.searchResult "Result 0/2"
           Mock.searchResult "Result 0/3"
           Mock.searchResult "Result 0/4" |]
    let results2 =
        [| Mock.searchResult "Result 1/1"
           Mock.searchResult "Result 1/2"
           Mock.searchResult "Result 1/3"
           Mock.searchResult "Result 1/4" |]

    let searchEngines : ISearchEngine array =
        [| Mock.dynamicSearchEngine "crashing-engine-id" [] false (fun _ _ _ -> failwith "Sorry, but not sorry")
           Mock.staticSearchEngine "engine-id-1" [] (fun () -> results1)
           Mock.dynamicSearchEngine "engine-id-2" [] false (fun _ _ _ -> results2) |]

    Assert.DoesNotThrow(
        Action(fun () ->
            SearchResultFiltering.testDisplayedResults
                (Array.append results1 results2)
                [||]
                searchEngines
                Configuration.Default
                (fun window -> window.KeyTextInput "Result")
        ),
        "Starter should not throw when an engine throws."
    )


[<AvaloniaTest>]
let testStaticSearchEngineCrash_OnSelectResult () =
    let searchEngine =
        { Id = "engine-id"
          OnLoadResults = fun () -> [| Mock.searchResult "Result" |]
          OnSearchResultSelected = fun _ -> failwith "Sorry, but not sorry"
          Activators = [||] }

    Assert.DoesNotThrow(
        Action(fun () ->
            Mock.withWindowConfig Configuration.Default [ searchEngine ] (fun window _ ->
                window.KeyTextInput "result"
                window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
                window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
            )
        ),
        "Starter should not throw when an engine throws."
    )


[<AvaloniaTest>]
let testDynamicSearchEngineCrash_OnSelectResult () =
    let searchEngine =
        { new IDynamicSearchEngine with
            member this.SearchResultSelected result = failwith "Sorry, but not sorry"
            member this.Search(query, ct, activator) = [ Mock.searchResult "Result" ], R3.Observable.Empty()
            member this.Id = "crashing-engine-id"
            member this.Name = "Engine name: crashing-engine-id"
            member this.ShortName = "Engine short name: crashing-engine-id"
            member this.Icon = StarterIconSource.Empty
            member this.Activators = [| |]
            member this.ResultsPriority = ResultPriority.Search
            member this.BufferResults = false
            member this.add_Changed _ = ()
            member this.remove_Changed _ = () }

    Assert.DoesNotThrow(
        Action(fun () ->
            Mock.withWindowConfig Configuration.Default [ searchEngine ] (fun window _ ->
                window.KeyTextInput "result"
                window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
                window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
            )
        ),
        "Starter should not throw when an engine throws."
    )
