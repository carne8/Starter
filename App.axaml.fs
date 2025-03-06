namespace Starter

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Data.Core.Plugins
open Avalonia.Markup.Xaml

open Starter.ViewModels
open Starter.Views
open Starter.Features
open Starter.Features.PlatformInterop

open Vanara.PInvoke

type App() =
    inherit Application()

    override this.Initialize() =
        AvaloniaXamlLoader.Load this

    override this.OnFrameworkInitializationCompleted() =

        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt 0

        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime ->
            let config =
                match Config.getConfig() with
                | Error Config.LoadConfigError.CannotRetrieveProcessPath -> failwith "Cannot retrieve process path"
                | Error Config.LoadConfigError.ConfigFileNotFound ->
                    printfn "Config file not found, creating one"
                    let emptyConfig = Config.Configuration(firstStart = true)
                    let saveRes = emptyConfig |> Config.saveConfig

                    match saveRes with
                    | Error Config.SaveConfigError.CannotRetrieveProcessPath -> failwith "Cannot retrieve process path while saving"
                    | Ok () -> emptyConfig
                | Ok config -> config

            let platformInterop = PlatformInteropFactory.GetPlatformInterop()

            if config.firstStart then
                printfn "Enable launch at startup"
                platformInterop.EnableLaunchAtStartup()
                config.firstStart <- false
                Config.saveConfig config |> ignore

            let window = MainWindow(DataContext = new MainWindowViewModel())

            match window.TryGetPlatformHandle() with
            | null -> failwith "Failed to retrieve window platform handle"
            | platformHandle ->
                User32.RegisterHotKey(
                    platformHandle.Handle,
                    0, // Hotkey id
                    User32.HotKeyModifiers.MOD_ALT,
                    User32.VK.VK_SPACE |> uint
                ) |> ignore
        | _ -> ()

        base.OnFrameworkInitializationCompleted()
