module Starter.Tests.SearchResultFiltering

open Avalonia.Controls
open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Input
open Avalonia.Threading
open Starter.Features
open Starter.Features.Config
open Starter.SearchEngine
open Starter.Tests.Common

let testDisplayedResults (shouldBeDisplayed: _ array) (shouldNotBeDisplayed: _ array) engines config input =
    Mock.withWindowConfig config engines (fun window _ ->
        // Type text
        Dispatcher.UIThread.RunJobs() // Let window acknowledge about vm
        window.Show()
        Dispatcher.UIThread.RunJobs() // Let textbox grab focus
        window.KeyTextInput input

        // Assert all results are shown
        let displayedResults =
            window
            |> Helpers.getControl<ListBox> "ResultList"
            |> _.Items
            |> Seq.map (unbox<SearchResultData> >> _.SearchResult)
            |> Seq.toArray

        Assert.AreEqual(
            shouldBeDisplayed.Length,
            displayedResults.Length,
            "A unexpected count of results is shown"
        )

        shouldBeDisplayed |> Array.iter (fun r ->
            displayedResults
            |> Array.contains r
            |> fun b -> Assert.IsTrue(b, $"'{r.Name}' should be displayed")
        )

        shouldNotBeDisplayed |> Array.iter (fun r ->
            displayedResults
            |> Array.contains r
            |> fun b -> Assert.IsFalse(b, $"'{r.Name}' should not be displayed")
        )
    )

[<AvaloniaTest>]
let ensureSearchResultsAppear () =
    let results =
        [| Array.init 5 (fun i -> Mock.searchResult $"Result: {i}")
           Array.init 5 (fun i -> Mock.searchResult $"Dynamic Result: {i}")
           Array.init 5 (fun i -> Mock.searchResult $"Dynamic buffered Result: {i}") |]

    let engines = [
        Mock.staticSearchEngine "static" [] (fun () -> results[0]) :> ISearchEngine
        Mock.dynamicSearchEngine "dynamic" [] false (fun _ _ _ -> results[1])
        Mock.dynamicSearchEngine "buffered" [] true (fun _ _ _ -> results[2])
    ]

    testDisplayedResults (Array.concat results) [||] engines Configuration.Default "result"

[<AvaloniaTest>]
let testActivatorFiltering_ActivatorDisabled () =
    let activator = Mock.activator "activator-id" "search-engine-id"

    let shouldBeDisplayedResults =
        [| Mock.searchResultWithActivator "Result 0" true Array.empty
           Mock.searchResultWithActivator "Result 1" true [| activator |]
           Mock.searchResultWithActivator "Result 2" true Array.empty
           Mock.searchResultWithActivator "Result 3" true Array.empty
           Mock.searchResultWithActivator "Result 4" true [| activator |] |]
    let shouldNotBeDisplayedResults =
        [| Mock.searchResultWithActivator "Result 5" false Array.empty
           Mock.searchResultWithActivator "Result 6" false [| activator |]
           Mock.searchResultWithActivator "Result 7" false Array.empty
           Mock.searchResultWithActivator "Result 8" false [| activator |]
           Mock.searchResultWithActivator "Not matching text 0" true Array.empty
           Mock.searchResultWithActivator "Not matching text 1" true [| activator |]
           Mock.searchResultWithActivator "Not matching text 2" false [| activator |] |]

    let searchEngine =
        { Id = activator.SearchEngineId
          OnLoadResults = fun () ->
            Array.append
                shouldBeDisplayedResults
                shouldNotBeDisplayedResults
          OnSearchResultSelected = ignore
          Activators = [ activator ] }

    testDisplayedResults
        shouldBeDisplayedResults
        shouldNotBeDisplayedResults
        [ searchEngine ]
        Configuration.Default
        "result"

[<AvaloniaTest>]
let testActivatorFiltering_StaticEngine_ActivatorEnabled () =
    let activator = Mock.activator "activator-id" "search-engine-id"
    let otherActivator = Mock.activator "activator-id-2" "search-engine-id"

    let shouldBeDisplayedResults =
        [| Mock.searchResultWithActivator "Result 0" true [| activator |]
           Mock.searchResultWithActivator "Result 1" true [| activator |]
           Mock.searchResultWithActivator "Result 2" false [| activator |]
           Mock.searchResultWithActivator "Result 3" false [| activator |]
           Mock.searchResultWithActivator "Result 4" false Array.empty
           Mock.searchResultWithActivator "Result 5" true Array.empty |]
    let shouldNotBeDisplayedResults =
        [| Mock.searchResultWithActivator "Result 6" false [| otherActivator |]
           Mock.searchResultWithActivator "Result 7" true [| otherActivator |]
           Mock.searchResultWithActivator "Result 8" false [| otherActivator |]
           Mock.searchResultWithActivator "Not matching text 0" true Array.empty
           Mock.searchResultWithActivator "Not matching text 1" true [| activator |]
           Mock.searchResultWithActivator "Not matching text 2" false [| activator |]
           Mock.searchResultWithActivator "Not matching text 3" true [| otherActivator |]
           Mock.searchResultWithActivator "Not matching text 4" false [| otherActivator |] |]

    let searchEngine =
        { Id = activator.SearchEngineId
          OnLoadResults = fun () ->
            Array.append
                shouldBeDisplayedResults
                shouldNotBeDisplayedResults
          OnSearchResultSelected = ignore
          Activators = [ activator ] }

    let config =
        { Configuration.Default with
            ActivatorPrefixes = Map.ofList [ activator.Id, "prefix-" ] }

    testDisplayedResults
        shouldBeDisplayedResults
        shouldNotBeDisplayedResults
        [ searchEngine ]
        config
        "prefix-result"

[<AvaloniaTest>]
let testActivatorFiltering_StaticEngine_ActivatorEnabled_EmptyQueryShowAllResults () =
    let activator = Mock.activator "activator-id" "search-engine-id"
    let otherActivator = Mock.activator "activator-id-2" "search-engine-id"

    let shouldBeDisplayedResults =
        [| Mock.searchResultWithActivator "Result 0" true [| activator |]
           Mock.searchResultWithActivator "Result 1" true [| activator |]
           Mock.searchResultWithActivator "Result 2" false [| activator |]
           Mock.searchResultWithActivator "Result 3" false [| activator |]
           Mock.searchResultWithActivator "Result 4" false Array.empty
           Mock.searchResultWithActivator "Result 5" true Array.empty
           Mock.searchResultWithActivator "Not matching text 0" true Array.empty
           Mock.searchResultWithActivator "Not matching text 1" true [| activator |]
           Mock.searchResultWithActivator "Not matching text 2" false [| activator |] |]
    let shouldNotBeDisplayedResults =
        [| Mock.searchResultWithActivator "Result 6" false [| otherActivator |]
           Mock.searchResultWithActivator "Result 7" true [| otherActivator |]
           Mock.searchResultWithActivator "Result 8" false [| otherActivator |]
           Mock.searchResultWithActivator "Not matching text 3" true [| otherActivator |]
           Mock.searchResultWithActivator "Not matching text 4" false [| otherActivator |] |]

    let searchEngine =
        { Id = activator.SearchEngineId
          OnLoadResults = fun () ->
            Array.append
                shouldBeDisplayedResults
                shouldNotBeDisplayedResults
          OnSearchResultSelected = ignore
          Activators = [ activator ] }

    let config =
        { Configuration.Default with
            ActivatorPrefixes = Map.ofList [ activator.Id, "prefix-" ] }

    testDisplayedResults
        shouldBeDisplayedResults
        shouldNotBeDisplayedResults
        [ searchEngine ]
        config
        "prefix-"

[<AvaloniaTest>]
let ensureSearchResultsAreCleared () =
    let results =
        [| Array.init 5 (fun i -> Mock.searchResult $"Result: {i}")
           Array.init 5 (fun i -> Mock.searchResult $"Dynamic Result: {i}")
           Array.init 5 (fun i -> Mock.searchResult $"Dynamic buffered Result: {i}") |]

    let engines = [
        Mock.staticSearchEngine "static" [] (fun () -> results[0]) :> ISearchEngine
        Mock.dynamicSearchEngine "dynamic" [] false (fun _ _ _ -> results[1])
        Mock.dynamicSearchEngine "buffered" [] true (fun _ _ _ -> results[2])
    ]

    Mock.withWindowConfig Configuration.Default engines (fun window _ ->
        // Type text
        Dispatcher.UIThread.RunJobs() // Let window acknowledge about vm
        window.Show()
        Dispatcher.UIThread.RunJobs() // Let textbox grab focus
        window.KeyTextInput "result"
        window.KeyPress(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)

        let displayedResults = window |> Helpers.getControl<ListBox> "ResultList"
        Assert.AreEqual(0, displayedResults.ItemCount, "No results should displayed")
    )


[<AvaloniaTest>]
let ensureSearchResultsAreCleared_WithActivator () =
    let activator = Mock.activator "activator-id" "static"
    let results =
        [| Array.init 5 (fun i -> Mock.searchResult $"Result: {i}")
           Array.init 5 (fun i -> Mock.searchResult $"Dynamic Result: {i}")
           Array.init 5 (fun i -> Mock.searchResult $"Dynamic buffered Result: {i}") |]

    let engines = [
        Mock.staticSearchEngine "static" [ activator ] (fun () -> results[0]) :> ISearchEngine
        Mock.dynamicSearchEngine "dynamic" [] false (fun _ _ _ -> results[1])
        Mock.dynamicSearchEngine "buffered" [] true (fun _ _ _ -> results[2])
    ]

    let config =
        { Configuration.Default with
            ActivatorPrefixes = Map.ofList [ activator.Id, "prefix-" ] }

    Mock.withWindowConfig config engines (fun window _ ->
        // Type text
        Dispatcher.UIThread.RunJobs() // Let window acknowledge about vm
        window.Show()
        Dispatcher.UIThread.RunJobs() // Let textbox grab focus
        window.KeyTextInput "prefix-result"

        // Erase
        let tb = window |> Helpers.getControl<TextBox> "TextBox"
        tb.CaretIndex <- tb.Text |> function null -> 0 | t -> t.Length
        window.KeyPress(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)

        let displayedResults = window |> Helpers.getControl<ListBox> "ResultList"
        Assert.AreEqual(0, displayedResults.ItemCount, "No results should displayed")
    )
