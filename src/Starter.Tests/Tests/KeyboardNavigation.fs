module Starter.Tests.KeyboardNavigation

open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Input
open Starter.Features
open Starter.Features.Config
open Starter.Tests.Common

[<AvaloniaTest>]
let ensureKeyboardResultSelectionWorks () =
    let results =
        [| Mock.searchResult "Result 0"
           Mock.searchResult "Result 1"
           Mock.searchResult "Result 2"
           Mock.searchResult "Result 3" |]

    let searchEngine =
        { Id = "engine-id"
          OnLoadResults = fun () -> results
          OnSearchResultSelected = ignore
          Activators = Array.empty }

    Mock.withWindowConfig Configuration.Default [ searchEngine ] (fun window _ ->
        // Type text
        window.KeyTextInput "result"

        let resultList = window.ResultList
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

        // Test down arrow
        for i = 1 to results.Length-1 do
            window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null)
            window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null)
            assertSelectedItem i

        // Assert going down one more time does not change the selected result
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null)
        window.KeyRelease(Key.Down, RawInputModifiers.None, PhysicalKey.ArrowDown, null)
        assertSelectedItem (results.Length-1)

        // Test up arrow
        for i = results.Length-2 downto 0 do
            window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null)
            window.KeyRelease(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null)
            assertSelectedItem i

        // Assert going up one more time does not change the selected result
        window.KeyPress(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null)
        window.KeyRelease(Key.Up, RawInputModifiers.None, PhysicalKey.ArrowUp, null)
        assertSelectedItem 0
    )
