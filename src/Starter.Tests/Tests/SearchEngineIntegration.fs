module Starter.Tests.SearchEngineIntegration

open Avalonia.Headless.NUnit
open Starter.Features.Config
open Starter.SearchEngine
open Starter.Tests.Common

[<AvaloniaTest>]
let ensureSelectedSearchResultIsCalled_Static () =
    let mutable called = [| null; null |]

    let results =
        [| Mock.searchResult "Result 1"
           Mock.searchResult "Result 2"
           Mock.searchResult "Result 3"
           Mock.searchResult "Result 4" |]

    let searchEngines : SearchEngine array =
        [| Mock.staticSearchEngine
            "engine-id"
            Array.empty
            (fun () -> results)
            (fun result -> called[0] <- result)

           Mock.staticSearchEngine
            "other-engine-id"
            Array.empty
            (fun () -> Array.empty)
            (fun result -> called[1] <- result) |]

    Mock.withWindowConfig Configuration.Default searchEngines (fun window _ ->
        // Select last result
        results
        |> Array.last
        |> Mock.selectResult window

        Assert.IsNotNull(called[0], "Search engine should have been called")
        Assert.AreEqual(called[0], Array.last results, "Search engine should have been called with the correct search engine")

        Assert.IsNull(called[1], "The other search engine should not have been called")
    )
