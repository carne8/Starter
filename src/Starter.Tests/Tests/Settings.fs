module Starter.Tests.Settings

open System
open System.Threading.Tasks
open Avalonia.Headless
open Avalonia.Headless.NUnit
open Avalonia.Input
open Starter
open Starter.Features.Config
open Starter.Features.PlatformInterop
open Starter.Tests.Common

let withSettingsWindow launcher (platform: IPlatformInterop) callback =
    let engineStore = Mock.searchEngineStore []
    let config = Configuration.Default |> platform.EnsureConfigCompatibility

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
            member this.RegisterHotkey shortcut window = ValueTask.FromResult true
            member this.SetupHotkeyCallback(window) = ()
            member this.ToggleLaunchAtStartup(enable) =
                isLaunchAtStartupEnabled <- not isLaunchAtStartupEnabled
            member this.HotkeyRegistrable = true }

    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform (fun window settings _ ->
        // Toggle launch at startup
        Assert.IsFalse(isLaunchAtStartupEnabled, "Launch at startup does not match config")

        let launchAtStartupToggle = settings.LaunchAtStartupSwitch
        Assert.IsTrue(launchAtStartupToggle.Focus(), "Failed to focus launch at startup toggle")

        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None) // Toggle
        Assert.IsTrue(isLaunchAtStartupEnabled, "Launch at startup should have been cancelled")

        window.KeyReleaseQwerty(PhysicalKey.Space, RawInputModifiers.None) // Toggle
        Assert.IsFalse(isLaunchAtStartupEnabled, "Launch at startup should have been cancelled")
    )


[<AvaloniaTest>]
let testZoomMode () =
    let platform = Mock.platform ()
    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform (fun window settings vm ->
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

    withSettingsWindow launcher platform (fun _ settings vm ->
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
            member this.RegisterHotkey shortcut window = ValueTask.FromResult true
            member this.SetupHotkeyCallback(window) = ()
            member this.ToggleLaunchAtStartup(enable) = ()
            member this.HotkeyRegistrable = true }

    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform (fun _ settings vm ->
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

    withSettingsWindow launcher platform (fun _ settings vm ->
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

// TODO: Activator prefixes
