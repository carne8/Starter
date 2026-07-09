module Starter.Tests.HotkeyRegistration

open R3
open System.Threading.Tasks
open Avalonia.Threading
open Avalonia.Headless.NUnit

open Starter
open Starter.Features
open Starter.Features.Config
open Starter.Features.PlatformInterop
open Starter.Tests.Common

[<AvaloniaTest>]
let ensureHotkeyRegistration () =
    let mutable hotkeyRegistered = false
    let mutable callbackRegistered = None

    let platform =
        { new IPlatformInterop with
            member this.IsLaunchAtStartupEnabled() = false
            member this.EnsureConfigCompatibility(config) = config
            member this.SupportBackground(background) = true
            member this.RegisterHotkey shortcut window =
                hotkeyRegistered <- true
                ValueTask.FromResult true
            member this.SetupHotkeyCallback(window) = callbackRegistered <- Some window
            member this.ToggleLaunchAtStartup(enable) = ()
            member this.HotkeyRegistrable = true }

    use config = new BehaviorSubject<_>(Configuration.Default)
    use activatorStore = new ActivatorStore(config)
    let vm = ViewModels.MainWindowViewModel(
        config,
        Mock.scoreDb (),
        SearchEngineStore(),
        activatorStore
    )
    let window = Views.MainWindow(platform, DataContext = vm)

    Dispatcher.UIThread.RunJobs();

    Assert.IsTrue(hotkeyRegistered, "Hotkey should be registered")
    Assert.IsTrue(callbackRegistered.IsSome, "Hotkey callback should be registered")
    Assert.IsTrue(callbackRegistered.Value = window, "Window of the callback should be the calling window")

[<AvaloniaTest>]
let ensureHotkeyNotRegisteredWhenNotRegistrable () =
    let mutable hotkeyRegistered = false
    let mutable callbackRegistered = None

    let platform =
        { new IPlatformInterop with
            member this.IsLaunchAtStartupEnabled() = false
            member this.EnsureConfigCompatibility(config) = config
            member this.SupportBackground(background) = true
            member this.RegisterHotkey shortcut window =
                hotkeyRegistered <- true
                ValueTask.FromResult true
            member this.SetupHotkeyCallback(window) = callbackRegistered <- Some window
            member this.ToggleLaunchAtStartup(enable) = ()
            member this.HotkeyRegistrable = false }

    use config = new BehaviorSubject<_>(Configuration.Default)
    use activatorStore = new ActivatorStore(config)
    let vm = ViewModels.MainWindowViewModel(
        config,
        Mock.scoreDb (),
        Mock.searchEngineStore [],
        activatorStore
    )
    Views.MainWindow(platform, DataContext = vm) |> ignore

    Dispatcher.UIThread.RunJobs();

    Assert.IsFalse(hotkeyRegistered, "Hotkey should not be registered")
    Assert.IsTrue(callbackRegistered.IsSome, "Hotkey callback should be registered")
