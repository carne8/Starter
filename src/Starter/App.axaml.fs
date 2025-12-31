namespace Starter

open Avalonia
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.Data.Core.Plugins
open Avalonia.Markup.Xaml

open Starter.Features
open Starter.Features.Config
open Starter.Features.Logging
open Starter.Features.PlatformInterop
open Starter.ViewModels
open Starter.Views

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
            Configuration.ensurePluginsSymlinkExists()

            // Load config
            let config =
                logger.Debug "Loading config"
                match Configuration.loadFromFile Constants.ConfigFile with
                | Error e ->
                    logger.Error $"Failed to decode configuration: {e}"
                    failwith "Failed to decode configuration. For more information, see logs."
                | Ok config ->
                    logger.Information "Config loaded"
                    config

            // Load result scores
            let resultScoreDb = Constants.ResultScoresFile |> ResultScores.ScoreDb.readFromFile

            // Create the window
            let window = MainWindow(DataContext = MainWindowViewModel(config, resultScoreDb))

            // Register hotkey
            PlatformInteropFactory.GetPlatformInterop().RegisterHotkey
                config.KeyboardShortcut
                window
            |> ignore

            logger.Debug "Launched"
        | _ -> ()

        base.OnFrameworkInitializationCompleted()
