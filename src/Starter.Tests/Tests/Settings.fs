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

let withSettingsWindow launcher platform callback =
    let engineStore = Mock.searchEngineStore []

    let keyboardShortcutVm =
        ViewModels.KeyboardShortcutInputViewModel(
            Configuration.Default,
            platform
        )

    let settingsVm =
        ViewModels.SettingsViewModel(
            launcher,
            Configuration.Default,
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
            member this.RegisterHotkey shortcut window = ValueTask.FromResult true
            member this.SetupHotkeyCallback(window) = ()
            member this.ToggleLaunchAtStartup(var0) =
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
let testBackground () =
    let platform = Mock.platform ()
    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform (fun _ settings vm ->
        // Toggle background mode
        Assert.AreEqual(vm.Config.Value.Background, Background.Mica, "Background does not match config")

        let none, mica, acrylic =
            ViewModels.SettingsViewModel.Backgrounds
            |> Array.find (fun b -> b.Value = Background.None),
            ViewModels.SettingsViewModel.Backgrounds
            |> Array.find (fun b -> b.Value = Background.Mica),
            ViewModels.SettingsViewModel.Backgrounds
            |> Array.find (fun b -> b.Value = Background.Acrylic)

        let comboBox = settings.BackgroundComboBox
        Assert.IsTrue(comboBox.Focus(), "Failed to focus background combo box")

        comboBox.SelectedValue <- none
        Assert.AreEqual(vm.Config.Value.Background, Background.None, "Background has not been set to the correct value.")

        comboBox.SelectedValue <- acrylic
        Assert.AreEqual(vm.Config.Value.Background, Background.Acrylic, "Background has not been set to the correct value.")

        comboBox.SelectedValue <- mica
        Assert.AreEqual(vm.Config.Value.Background, Background.Mica, "Background has not been set to the correct value.")
    )

[<AvaloniaTest>]
let testAntialiasing () =
    let platform = Mock.platform ()
    let launcher = Mock.launcher ignore ignore

    withSettingsWindow launcher platform (fun _ settings vm ->
        // Toggle antialiasing mode
        Assert.AreEqual(vm.Config.Value.Antialiasing, Antialiasing.Grayscale, "Antialiasing mode does not match config")

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
        Assert.AreEqual(vm.Config.Value.Antialiasing, Antialiasing.Alias, "Antialiasing mode has not been set to the correct value.")

        comboBox.SelectedValue <- grayscale
        Assert.AreEqual(vm.Config.Value.Antialiasing, Antialiasing.Grayscale, "Antialiasing mode has not been set to the correct value.")

        comboBox.SelectedValue <- platformDefault
        Assert.AreEqual(vm.Config.Value.Antialiasing, Antialiasing.PlatformDefault, "Antialiasing mode has not been set to the correct value.")

        comboBox.SelectedValue <- subpixel
        Assert.AreEqual(vm.Config.Value.Antialiasing, Antialiasing.Subpixel, "Antialiasing mode has not been set to the correct value.")
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
//         Assert.AreEqual(lastLaunchedFile, None, "No folder should have been opened as none of the buttons have been pressed")
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
