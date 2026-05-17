module Starter.Tests.Tests.SearchEngineActivator

open Avalonia.Controls
open Avalonia.Headless
open Avalonia.Headless.XUnit
open Avalonia.Input
open Avalonia.Threading
open Starter.Features.Config
open Starter.SearchEngine
open Starter.Tests.Common

[<AvaloniaFact>]
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
        Dispatcher.UIThread.RunJobs() // Let window acknowledge about vm
        window.Show()
        Dispatcher.UIThread.RunJobs() // Let textbox grab focus
        window.KeyTextInput "static-result"

        // Assert activator enabled
        Assert.NotNull(vm.Activator, "MainWindowViewModel should have an activator set")

        // Assert textbox erased the prefix
        let tb = window |> Helpers.getControl<TextBox> "TextBox"
        Assert.Equal("-result", tb.Text, "Textbox should have erased the prefix")

        // Assert activator name is displayed
        let label = window |> Helpers.getControl<TextBlock> "GreetingOrActivatorLabel"
        Assert.Equal(activators[0].Name, label.Text, "Displayed text should be the activator name")

        // Remove activator
        window.KeyPress(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.Control, PhysicalKey.Backspace, null)
        window.KeyPress(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)
        window.KeyRelease(Key.Back, RawInputModifiers.None, PhysicalKey.Backspace, null)

        // Assert activator disabled
        Assert.Null(vm.Activator, "MainWindowViewModel should not have an activator set")

        // Assert activator name is not displayed anymore
        let label = window |> Helpers.getControl<TextBlock> "GreetingOrActivatorLabel"
        Assert.NotEqual(activators[0].Name, label.Text, "Displayed text should be the activator name")
    )
