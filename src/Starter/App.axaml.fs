namespace Starter

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Data.Core.Plugins
open Avalonia.Markup.Xaml

open Starter.Features
open Starter.ViewModels
open Starter.Views

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

            // Load config
            let config =
                match Config.getConfig Constants.ConfigFile with
                | Error e -> failwithf "Failed to decode configuration: %A" e
                | Ok config -> config

            // Load result scores
            let resultScoreDb = Constants.ResultScoresFile |> ResultScores.ScoreDb.readFromFile

            // Create the window
            let window = MainWindow(DataContext = MainWindowViewModel(config, resultScoreDb))

            // Register hotkey // TODO: Move to platform interop
            match window.TryGetPlatformHandle() with
            | null -> failwith "Failed to retrieve window platform handle"
            | platformHandle ->
                User32.RegisterHotKey(
                    platformHandle.Handle,
                    0, // Hotkey id
                    User32.HotKeyModifiers.MOD_ALT,
                    User32.VK.VK_SPACE |> uint
                ) |> ignore

            #if DEBUG
            printfn "Launched"
            #endif
        | _ -> ()

        base.OnFrameworkInitializationCompleted()
