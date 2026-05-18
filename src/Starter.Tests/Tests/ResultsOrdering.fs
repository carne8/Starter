module Starter.Tests.ResultsOrdering

open Avalonia.Controls
open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Input
open Avalonia.Threading
open Starter.Features
open Starter.Features.Config
open Starter.Tests.Common

[<AvaloniaTest>]
let ensureMostUsedResultsAreTheFirstShowed () =
    let results =
        [| Mock.searchResultWithActivator "Result 0" true Array.empty
           Mock.searchResultWithActivator "Result 1" true Array.empty
           Mock.searchResultWithActivator "Result 2" true Array.empty |]

    let searchEngine =
        { Id = "engine-id"
          OnLoadResults = fun () -> results
          OnSearchResultSelected = ignore
          Activators = List.empty }

    Mock.withWindowConfig Configuration.Default [ searchEngine ] (fun window _ ->
        // Type text
        Dispatcher.UIThread.RunJobs() // Let window acknowledge about vm
        window.Show()
        Dispatcher.UIThread.RunJobs() // Let textbox grab focus

        // Select last result
        window.KeyTextInput results[2].Name
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.Show()
        for _ = 0 to results[2].Name.Length-1 do
            window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
            window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)

        // Select 2 times the second result
        window.KeyTextInput results[1].Name
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.Show()
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null)
        window.Show()
        for _ = 0 to results[1].Name.Length-1 do
            window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
            window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)

        // Show all results
        window.KeyTextInput "Result"

        // Assert results ordering
        let displayedResults =
            window
            |> Helpers.getControl<ListBox> "ResultList"
            |> _.Items
            |> Seq.map (unbox<SearchResultData> >> _.SearchResult)
            |> Seq.toArray

        Assert.IsTrue(
            [| results[1]; results[2]; results[0] |] = displayedResults,
            "Displayed results are not ordered correctly"
        )
    )
