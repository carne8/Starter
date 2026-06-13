module Starter.Tests.ExceptionResilience

open System
open Avalonia.Headless
open Avalonia.Headless.NUnit
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
        [| Mock.staticSearchEngine "crash-engine-id" [] (fun () -> failwith "Sorry, but not sorry")
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
        [| Mock.dynamicSearchEngine "crash-engine-id" [] false (fun _ _ _ -> failwith "Sorry, but not sorry")
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
