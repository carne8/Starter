module Starter.Tests.KeyboardNavigation

open Avalonia.Controls
open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Input
open Starter.Features
open Starter.Features.Config
open Starter.Tests.Common

[<AvaloniaTest>]
let ensureKeyboardResultSelectionWorks () =
    let results =
        [| Mock.searchResult "Result 1"
           Mock.searchResult "Result 2"
           Mock.searchResult "Result 3"
           Mock.searchResult "Result 4" |]

    let searchEngines =
        { Id = "engine-id"
          OnLoadResults = fun () -> results
          OnSearchResultSelected = ignore
          Activators = Array.empty }

    Mock.withWindowConfig Configuration.Default [ searchEngines ] (fun window _ ->
        // Type text
        window.KeyTextInput "result"

        let resultList = window |> Helpers.getControl<ListBox> "ResultList"
        let assertSelectedItem expectedResultIdx =
            Assert.AreEqual(expectedResultIdx, resultList.SelectedIndex, "Incorrect selected result index")
            Assert.AreEqual(
                results[expectedResultIdx],
                resultList.SelectedItem
                |> unbox<SearchResultData>
                |> _.SearchResult,
                "The results are not displayed in the correct order"
            )

        assertSelectedItem 0

        for i = 1 to results.Length-1 do
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null)
            window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null)
            assertSelectedItem i

        // Assert going down one more time does not change the selected result
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null)
        window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null)
        assertSelectedItem (results.Length-1)
    )
