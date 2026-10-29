module Starter.Tests.ResultsOrdering

open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Threading
open Starter.Features.Config
open Starter.SearchEngine
open Starter.Tests.Common

[<AvaloniaTest>]
let ensureMostUsedResultsAreTheFirstShowed () =
    let results =
        [| Mock.searchResultWithActivator "Result 0" true Array.empty
           Mock.searchResultWithActivator "Result 1" true Array.empty
           Mock.searchResultWithActivator "Result 2" true Array.empty |]

    let searchEngine = Mock.staticSearchEngine "engine-id" List.empty (fun () -> results) ignore

    Mock.withWindowConfig Configuration.Default [ searchEngine ] (fun window _ ->
        // Type text
        Dispatcher.UIThread.RunJobs() // Let window acknowledge about vm
        window.Show()
        Dispatcher.UIThread.RunJobs() // Let textbox grab focus

        // Select last result
        results[2] |> Mock.selectResult window
        // Select 2 times the second result
        results[1] |> Mock.selectResult window
        results[1] |> Mock.selectResult window

        // Show all results
        window.KeyTextInput "Result"

        // Assert results ordering
        let displayedResults =
            window.ResultList
            |> _.Items
            |> Seq.cast<SearchResult>
            |> Seq.toArray

        Assert.IsTrue(
            [| results[1]; results[2]; results[0] |] = displayedResults,
            "Displayed results are not ordered correctly"
        )
    )
