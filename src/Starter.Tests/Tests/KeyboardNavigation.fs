module Starter.Tests.KeyboardNavigation

open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Input
open Starter.Features.Config
open Starter.SearchEngine
open Starter.Tests.Common

[<AvaloniaTest>]
let ensureKeyboardResultSelectionWorks () =
    let results =
        [| Mock.searchResult "Result 0"
           Mock.searchResult "Result 1"
           Mock.searchResult "Result 2"
           Mock.searchResult "Result 3" |]

    let searchEngine = Mock.staticSearchEngine "engine-id" Array.empty (fun () -> results) ignore

    Mock.withWindowConfig Configuration.Default [ searchEngine ] (fun window _ ->
        // Type text
        window.KeyTextInput "result"

        let resultList = window.ResultList
        let assertSelectedItem expectedResultIdx =
            Assert.AreEqual(expectedResultIdx, resultList.SelectedIndex, "Incorrect selected result index")
            Assert.AreEqual(
                results[expectedResultIdx],
                resultList.SelectedItem
                |> unbox<SearchResult>,
                "The results are not displayed in the correct order"
            )

        assertSelectedItem 0

        // Test down arrow
        for i = 1 to results.Length-1 do
            window.PressDown()
            assertSelectedItem i

        // Assert going down one more time does not change the selected result
        window.PressDown()
        assertSelectedItem (results.Length-1)

        // Test up arrow
        for i = results.Length-2 downto 0 do
            window.PressUp()
            assertSelectedItem i

        // Assert going up one more time does not change the selected result
        window.PressUp()
        assertSelectedItem 0
    )
