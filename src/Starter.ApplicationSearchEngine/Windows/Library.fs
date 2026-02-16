namespace Starter.ApplicationSearchEngine.Windows

open System
open System.Threading.Tasks
open R3
open Starter.ApplicationSearchEngine
open Starter.SearchEngine

type WindowsAppsSearchEngine(pluginPath, configDir, logger) =
    inherit StaticSearchEngine(pluginPath, configDir, logger)
    static let icon = Constants.icon

    do Logger.logger <- logger

    let settingsViewModel = SettingsViewModel configDir
    let settingsControl = Settings(DataContext = settingsViewModel)
    let config = settingsViewModel.Config

    let resultsObservable = new BehaviorSubject<ISearchResult seq>(Seq.empty)
    let uwpLoader = UwpLoader.UwpAppsLoader()
    let exeLoader = ExeLoader.ExeAppsLoader()

    override this.LoadResults() =
        if not <| OperatingSystem.IsWindows() then
            logger.Warning("This search engine is not supported on this platform.")
        else
            exeLoader.Changed.Subscribe(fun () ->
                logger.Verbose "New Exe apps loaded"
                Seq.append
                    exeLoader.Apps
                    uwpLoader.Apps
                |> resultsObservable.OnNext
            ) |> ignore
            uwpLoader.Changed.Subscribe(fun () ->
                logger.Verbose "New UWP apps loaded"
                Seq.append
                    exeLoader.Apps
                    uwpLoader.Apps
                |> resultsObservable.OnNext
            ) |> ignore

            uwpLoader.LoadApps()
            config
                .Select(Config.FolderConfiguration.normalize)
                .DistinctUntilChanged()
                .Subscribe(fun folderConfig ->
                    logger.Information "Loading exe apps"
                    exeLoader.LoadApps folderConfig
                    exeLoader.ObserveFolders folderConfig
                )
            |> ignore

        struct (Seq.empty, resultsObservable.AsObservable()) |> Task.FromResult

    override _.Id = nameof WindowsAppsSearchEngine
    override _.Name = "Applications"
    override _.ShortName = "Apps"
    override _.Icon = icon
    override this.Activators = [| DefaultSearchEngineActivator(this) |]

    override _.SearchResultSelected(searchResult) =
        match searchResult with
        | :? ExeLoader.ExeApplication as app -> ExeLoader.runApp app
        | :? Uwp.UwpApplication as app -> UwpLoader.runApp app
        | _ -> ()
    override this.LoadSettingsControl() = settingsControl
