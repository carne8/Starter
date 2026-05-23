module Starter.Tests.SearchEngineActivator

open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Input
open Starter.Features.Config
open Starter.SearchEngine
open Starter.Tests.Common

[<AvaloniaTest>]
let testActivatorPrefix () =
    let activators =
        [ Mock.activator "activator-id-1" "static"
          Mock.activator "activator-id-2" "dynamic" ]

    let engines: ISearchEngine list =
        [ Mock.staticSearchEngine "static" [ activators[0] ] (fun () -> [])
          Mock.dynamicSearchEngine "dynamic" [ activators[1] ] false (fun _ _ _ -> []) ]

    let config =
        { Configuration.Default with
            ActivatorPrefixes =
                activators
                |> List.map (fun a -> a.Id, a.SearchEngineId)
                |> Map.ofList }

    Mock.withWindowConfig config engines (fun window vm ->
        // Type text
        window.KeyTextInput "static-result"

        // Assert activator enabled
        Assert.IsNotNull(vm.Activator, "MainWindowViewModel should have an activator set")

        // Assert textbox erased the prefix
        let tb = window.TextBox
        Assert.AreEqual("-result", tb.Text, "Textbox should have erased the prefix")

        // Assert activator name is displayed
        let label = window.GreetingOrActivatorLabel
        Assert.AreEqual(activators[0].Name, label.Text, "Displayed text should be the activator name")

        // Remove activator
        window.KeyPress(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)

        // Assert activator disabled
        Assert.Null(vm.Activator, "MainWindowViewModel should not have an activator set")

        // Assert activator name is not displayed anymore
        let label = window.GreetingOrActivatorLabel
        Assert.AreNotEqual(activators[0].Name, label.Text, "Displayed text should be the activator name")
    )

[<AvaloniaTest>]
let ensureActivatorIsPassedToDynamicSearchEngine () =
    let activators =
        [ Mock.activator "activator-id" "engine-1"
          Mock.activator "other-activator-id" "engine-2" ]

    let calls = ResizeArray()

    let engines: ISearchEngine list =
        [ Mock.dynamicSearchEngine "engine-1" [ activators[0] ] false (fun _ _ activator -> calls.Add activator; [])
          Mock.dynamicSearchEngine "engine-2" [ activators[1] ] false (fun _ _ _ -> []) ]

    let config =
        { Configuration.Default with
            ActivatorPrefixes =
                activators
                |> List.map (fun a -> a.Id, a.SearchEngineId)
                |> Map.ofList }

    Mock.withWindowConfig config engines (fun window _ ->
        // Type text
        window.KeyTextInput activators[0].SearchEngineId

        Assert.AreEqual(
            activators[0],
            calls |> Seq.last,
            "Dynamic search should have been called with the activator as parameter"
        )

        // Remove activator
        window.KeyPress(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)

        // Type something
        window.KeyTextInput "something"

        Assert.IsNull(
            calls |> Seq.last,
            "Dynamic search should not be called with the activator anymore"
        )

        // Set another activator
        window.KeyPress(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyTextInput activators[1].SearchEngineId

        Assert.AreNotEqual(
            activators[1],
            calls |> Seq.last,
            "Dynamic search engine should not be called"
        )
    )
