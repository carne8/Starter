module Starter.Tests.Settings

open System
open System.Threading.Tasks
open Avalonia.Controls
open Avalonia.Controls.Presenters
open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Input
open Avalonia.VisualTree
open Starter
open Starter.Features.Config
open Starter.Features.PlatformInterop
open Starter.SearchEngine
open Starter.Tests.Common

let withSettingsWindow launcher (platform: IPlatformInterop) engines callback =
    let engineStore = Mock.searchEngineStore engines
    let config =
        { Configuration.Default with
            ActivatorPrefixes =
                engines
                |> List.map (fun e -> e.Activators[0].Id, e.Activators[0].Id)
                |> Map.ofList }
        |> platform.EnsureConfigCompatibility

    let keyboardShortcutVm =
        ViewModels.KeyboardShortcutInputViewModel(
            config,
            platform
        )

    let settingsVm =
        ViewModels.SettingsViewModel(
            launcher,
            config,
            engineStore,
            platform,
            keyboardShortcutVm
        )

    let settingsWindowVm =
        ViewModels.SettingsWindowViewModel(
            engineStore,
            settingsVm
        )

    let settingsWindow = Views.SettingsWindow(DataContext = settingsWindowVm)
    settingsWindow.Show()
    let settingsView = settingsWindow.ContentControl.Presenter.Child :?> Views.Settings
    settingsWindow.Dispatcher.RunJobs()

    Assert.Multiple(Action(fun () ->
        callback settingsWindow settingsView settingsVm
    ))

[<AvaloniaTest>]
let testLaunchAtStartup () =
    let mutable isLaunchAtStartupEnabled = false
    let platform =
        { new IPlatformInterop with
            member this.IsLaunchAtStartupEnabled() =
                isLaunchAtStartupEnabled
            member this.EnsureConfigCompatibility(config) = config
            member this.SupportBackground(background) = true
            member this.RegisterHotkey shortcut window = Task.FromResult true
            member this.SetupHotkeyCallback(window) = ()
            member this.ToggleLaunchAtStartup(enable) =
                isLaunchAtStartupEnabled <- not isLaunchAtStartupEnabled
            member this.HotkeyRegistrable = true }

    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform [] (fun window settings _ ->
        // Assert disabled
        Assert.IsFalse(isLaunchAtStartupEnabled, "Launch at startup does not match config")

        let launchAtStartupToggle = settings.LaunchAtStartupSwitch
        Assert.IsTrue(launchAtStartupToggle.Focus(), "Failed to focus launch at startup toggle")

        // Toggle
        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None)
        Assert.IsTrue(isLaunchAtStartupEnabled, "Launch at startup should have been cancelled")

        // Toggle
        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None)
        Assert.IsFalse(isLaunchAtStartupEnabled, "Launch at startup should have been cancelled")
    )


[<AvaloniaTest>]
let testZoomMode () =
    let platform = Mock.platform ()
    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform [] (fun window settings vm ->
        // Toggle zoomed mode
        Assert.IsFalse(vm.Config.Value.ZoomedMode, "Zoom mode does not match config")

        let zoomModeToggle = settings.ZoomedModeSwitch
        Assert.IsTrue(zoomModeToggle.Focus(), "Failed to focus zoom mode toggle")

        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None) // Toggle
        Assert.IsTrue(vm.Config.Value.ZoomedMode, "Zoom mode should have been enabled")

        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None) // Toggle
        Assert.IsFalse(vm.Config.Value.ZoomedMode, "Zoom mode should have been disabled")
    )

[<AvaloniaTest>]
let testBackground_Windows () =
    let platform = Mock.platform ()
    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform [] (fun _ settings vm ->
        // Toggle background mode
        Assert.AreEqual(Background.Mica, vm.Config.Value.Background, "Background does not match config")

        let none, mica, acrylic =
            vm.Backgrounds |> Array.find (fun b -> b.Value = Background.None),
            vm.Backgrounds |> Array.find (fun b -> b.Value = Background.Mica),
            vm.Backgrounds |> Array.find (fun b -> b.Value = Background.Acrylic)

        let comboBox = settings.BackgroundComboBox
        Assert.IsTrue(comboBox.Focus(), "Failed to focus background combo box")

        comboBox.SelectedValue <- none
        Assert.AreEqual(Background.None, vm.Config.Value.Background, "Background has not been set to the correct value.")

        comboBox.SelectedValue <- acrylic
        Assert.AreEqual(Background.Acrylic, vm.Config.Value.Background, "Background has not been set to the correct value.")

        comboBox.SelectedValue <- mica
        Assert.AreEqual(Background.Mica, vm.Config.Value.Background, "Background has not been set to the correct value.")
    )

[<AvaloniaTest>]
let testBackground_Linux () =
    let platform =
        { new IPlatformInterop with
            member this.IsLaunchAtStartupEnabled() = false
            member this.EnsureConfigCompatibility(config) =
                { config with Background = Background.None }
            member this.SupportBackground(background) =
                match background with
                | Background.None -> true
                | Background.Mica
                | Background.Acrylic -> false
            member this.RegisterHotkey shortcut window = Task.FromResult true
            member this.SetupHotkeyCallback(window) = ()
            member this.ToggleLaunchAtStartup(enable) = ()
            member this.HotkeyRegistrable = true }

    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform [] (fun _ settings vm ->
        // Toggle background mode
        Assert.AreEqual(Background.None, vm.Config.Value.Background, "Background does not match config")

        let none, mica, acrylic =
            vm.Backgrounds |> Array.find (fun b -> b.Value = Background.None),
            vm.Backgrounds |> Array.find (fun b -> b.Value = Background.Mica),
            vm.Backgrounds |> Array.find (fun b -> b.Value = Background.Acrylic)

        let comboBox = settings.BackgroundComboBox
        comboBox.IsDropDownOpen <- true

        Assert.IsTrue(
            comboBox.ContainerFromItem(none).IsEnabled,
            "None background should not be disabled on Linux"
        )
        Assert.IsFalse(
            comboBox.ContainerFromItem(mica).IsEnabled,
            "Mica background should be disabled on Linux"
        )
        Assert.IsFalse(
            comboBox.ContainerFromItem(acrylic).IsEnabled,
            "Acrylic background should be disabled on Linux"
        )
    )

[<AvaloniaTest>]
let testAntialiasing () =
    let platform = Mock.platform ()
    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform [] (fun _ settings vm ->
        // Toggle antialiasing mode
        Assert.AreEqual(Antialiasing.Grayscale, vm.Config.Value.Antialiasing, "Antialiasing mode does not match config")

        let alias, grayscale, platformDefault, subpixel =
            ViewModels.SettingsViewModel.Antialiasings
            |> Array.find (fun b -> b.Value = Antialiasing.Alias),
            ViewModels.SettingsViewModel.Antialiasings
            |> Array.find (fun b -> b.Value = Antialiasing.Grayscale),
            ViewModels.SettingsViewModel.Antialiasings
            |> Array.find (fun b -> b.Value = Antialiasing.PlatformDefault),
            ViewModels.SettingsViewModel.Antialiasings
            |> Array.find (fun b -> b.Value = Antialiasing.Subpixel)

        let comboBox = settings.AntialiasingComboBox
        Assert.IsTrue(comboBox.Focus(), "Failed to focus antialiasing combo box")

        comboBox.SelectedValue <- alias
        Assert.AreEqual(Antialiasing.Alias, vm.Config.Value.Antialiasing, "Antialiasing mode has not been set to the correct value.")

        comboBox.SelectedValue <- grayscale
        Assert.AreEqual(Antialiasing.Grayscale, vm.Config.Value.Antialiasing, "Antialiasing mode has not been set to the correct value.")

        comboBox.SelectedValue <- platformDefault
        Assert.AreEqual(Antialiasing.PlatformDefault, vm.Config.Value.Antialiasing, "Antialiasing mode has not been set to the correct value.")

        comboBox.SelectedValue <- subpixel
        Assert.AreEqual(Antialiasing.Subpixel, vm.Config.Value.Antialiasing, "Antialiasing mode has not been set to the correct value.")
    )


// Cannot test folder opening because the launcher is called
// only if the folders exist, but they do not necessarily exist on the test machine
// [<AvaloniaTest>]
// let testAntialiasing () =
//     let mutable lastLaunchedFile = None
//     let launcher =
//         Mock.launcher ignore (fun file ->
//             lastLaunchedFile <-
//                 file.Path.AbsolutePath
//                 |> Path.GetFullPath
//                 |> Path.TrimEndingDirectorySeparator
//                 |> Some
//         )
//
//     let platform = Mock.platform ()
//
//     createSettingsWindow launcher platform (fun window settings vm ->
//         // Open directories
//         Assert.AreEqual(None, lastLaunchedFile, "No folder should have been opened as none of the buttons have been pressed")
//         let configButton = settings.ConfigFolderButton
//         let searchEnginesButton = settings.SearchEnginesFolderButton
//         let logsButton = settings.LogsFolderButton
//
//         let configFolderUri =        Constants.ConfigDirectory  |> Path.GetFullPath |> Some
//         let searchEnginesFolderUri = Constants.PluginsDirectory |> Path.GetFullPath |> Some
//         let logsFolderUri =          Constants.LogDirectory     |> Path.GetFullPath |> Some
//
//         Assert.IsTrue(configButton.Focus(), "Failed to focus config folder button")
//         window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None)
//         Assert.AreEqual(configFolderUri, lastLaunchedFile, "Unexpected opened config folder")
//         lastLaunchedFile <- None
//
//         Assert.IsTrue(searchEnginesButton.Focus(), "Failed to focus config folder button")
//         window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None)
//         Assert.AreEqual(searchEnginesFolderUri, lastLaunchedFile, "Unexpected opened search engines folder")
//         lastLaunchedFile <- None
//
//         Assert.IsTrue(logsButton.Focus(), "Failed to focus config folder button")
//         window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None)
//         Assert.AreEqual(logsFolderUri, lastLaunchedFile, "Unexpected opened logs folder")
//         lastLaunchedFile <- None
//     )

[<AvaloniaTest>]
let testActivatorPrefixes () =
    let platform = Mock.platform ()
    let launcher = Mock.launcher ignore ignore

    let activator1 = Mock.activator "activator-1" "engine-1"
    let activator2 = Mock.activator "activator-2" "engine-1"

    let engines : ISearchEngine list =
        [ Mock.staticSearchEngine "engine-1" [ activator1 ] (fun _ -> Seq.empty)
          Mock.dynamicSearchEngine "engine-2" [ activator2 ] false (fun _ _ _ -> Seq.empty) ]

    withSettingsWindow launcher platform engines (fun window settings vm ->
        let activatorStore = new Features.ActivatorStore(vm.Config)
        engines |> List.iter activatorStore.AddSearchEngineActivators

        Assert.AreEqual(
            ValueSome struct (activator1, activator1.Id),
            activatorStore.GetActivatorFromText(activator1.Id),
            "Activator prefix should be working"
        )

        Assert.AreEqual(
            ValueSome struct (activator2, activator2.Id),
            activatorStore.GetActivatorFromText(activator2.Id),
            "Activator prefix should be working"
        )

        window.Dispatcher.RunJobs()

        let textBoxes =
            settings.ActivatorPrefixes.ItemsPanelRoot.Children
            |> Seq.map (fun control ->
                let expander =
                    control
                    :?> ContentPresenter
                    |> _.Child
                    :?> FluentAvalonia.UI.Controls.FASettingsExpander

                let vm = expander.Footer :?> ViewModels.ActivatorInputFieldViewModel
                let tb = expander.FindDescendantOfType<TextBox>()

                vm.Name, tb
            )
            |> Seq.toArray

        let _, textBox1 = textBoxes |> Seq.find (fun (name, _) -> name = activator1.Name)
        let _, textBox2 = textBoxes |> Seq.find (fun (name, _) -> name = activator2.Name)

        // Change first activator
        let newPrefix1 = "activator-1-new-prefix"
        Assert.IsTrue(textBox1.Focus(), "Should be able to focus the text box")
        textBox1.SelectAll()
        window.KeyTextInput newPrefix1

        Assert.AreEqual(
            ValueSome struct (activator1, newPrefix1),
            activatorStore.GetActivatorFromText(newPrefix1),
            "Activator 1 prefix should have been changed"
        )
        Assert.AreEqual(
            ValueSome struct (activator2, activator2.Id),
            activatorStore.GetActivatorFromText(activator2.Id),
            "Activator 2 prefix should not have been changed"
        )

        // Change second activator
        let newPrefix2 = "activator-2-new-prefix"
        Assert.IsTrue(textBox2.Focus(), "Should be able to focus the text box")
        textBox2.SelectAll()
        window.KeyTextInput newPrefix2

        Assert.AreEqual(
            ValueSome struct (activator2, newPrefix2),
            activatorStore.GetActivatorFromText(newPrefix2),
            "Activator 2 prefix should have been changed"
        )
        Assert.AreEqual(
            ValueSome struct (activator1, newPrefix1),
            activatorStore.GetActivatorFromText(newPrefix1),
            "Activator 1 prefix should not have been changed"
        )
    )


[<AvaloniaTest>]
let testHotkey () =
    let platform = Mock.platform ()
    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform [] (fun window settings vm ->
        let textBox = settings.KeyboardShortcutInput.TextBox

        Assert.AreEqual(
            { Key = Key.Space; Modifiers = [| Key.LeftAlt |] },
            vm.Config.Value.KeyboardShortcut,
            "Default keyboard shortcut does not match"
        )

        let typeKeyCombination keys =
            Assert.IsTrue(textBox.Focus(), "Should be able to focus the text box")
            keys |> List.iter (fun key ->
                window.KeyPressQwerty(key, RawInputModifiers.None)
                window.KeyReleaseQwerty(key, RawInputModifiers.None)
            )

        // Test different key combinations
        typeKeyCombination [ PhysicalKey.ArrowDown; PhysicalKey.MetaLeft; PhysicalKey.Enter]
        Assert.AreEqual(
            { Key = Key.Down; Modifiers = [| Key.LWin |] },
            vm.Config.Value.KeyboardShortcut,
            "New keyboard shortcut does not match"
        )

        typeKeyCombination [ PhysicalKey.A; PhysicalKey.AltRight; PhysicalKey.Enter]
        Assert.AreEqual(
            { Key = Key.A; Modifiers = [| Key.RightAlt |] },
            vm.Config.Value.KeyboardShortcut,
            "New keyboard shortcut does not match"
        )

        // Test exit
        typeKeyCombination [ PhysicalKey.U; PhysicalKey.ControlLeft; PhysicalKey.Escape]
        Assert.AreEqual(
            { Key = Key.A; Modifiers = [| Key.RightAlt |] },
            vm.Config.Value.KeyboardShortcut,
            "Keyboard shortcut should not have been changed"
        )

        typeKeyCombination [ PhysicalKey.U; PhysicalKey.ControlLeft ]
        Assert.IsTrue(
            settings.KeyboardShortcutInput.ControlToFocus.Focus(),
            "Should be able to unfocus keyboard shortcut input"
        )
        Assert.AreEqual(
            { Key = Key.A; Modifiers = [| Key.RightAlt |] },
            vm.Config.Value.KeyboardShortcut,
            "Keyboard shortcut should not have been changed"
        )

        // Test several modifiers
        typeKeyCombination [ PhysicalKey.O; PhysicalKey.ControlRight; PhysicalKey.ControlLeft; PhysicalKey.Enter]
        Assert.AreEqual(
            { Key = Key.O; Modifiers = [| Key.RightCtrl; Key.LeftCtrl |] },
            vm.Config.Value.KeyboardShortcut,
            "New keyboard shortcut does not match"
        )
        typeKeyCombination [ PhysicalKey.S; PhysicalKey.MetaLeft; PhysicalKey.ShiftLeft; PhysicalKey.Enter]
        Assert.AreEqual(
            { Key = Key.S; Modifiers = [| Key.LWin; Key.LeftShift |] },
            vm.Config.Value.KeyboardShortcut,
            "New keyboard shortcut does not match"
        )
    )
