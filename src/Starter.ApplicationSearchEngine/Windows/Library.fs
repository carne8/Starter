namespace Starter.ApplicationSearchEngine.Windows

open System
open System.Threading.Tasks
open Avalonia.Controls.Templates
open R3
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Logger
open Starter.SearchEngine

type WindowsAppsSearchEngine(config: Observable<FolderConfiguration>) as this =
    inherit StaticSearchEngine()
    let icon = Constants.getIcon ()

    let uwpLoader = UwpLoader.UwpAppsLoader()
    let exeLoader = ExeLoader.ExeAppsLoader()

    do
        // Subscribe to loaders events
        exeLoader.Changed.Subscribe(fun () ->
            logger.Verbose "New Exe apps loaded"
            this.OnResultsChanged()
        ) |> ignore
        uwpLoader.Changed.Subscribe(fun () ->
            logger.Verbose "New UWP apps loaded"
            this.OnResultsChanged()
        ) |> ignore

    member private this.OnResultsChanged() =
        this.OnResultsChanged(
            Seq.append
                exeLoader.Apps
                uwpLoader.Apps
        )

    override this.LoadResults() =
        if not <| OperatingSystem.IsWindows() then
            logger.Warning("This search engine is not supported on this platform.")
        else
            uwpLoader.LoadApps()
            config
                .Select(Config.FolderConfiguration.normalize)
                .DistinctUntilChanged()
                .Subscribe(fun folderConfig ->
                    logger.Verbose "Loading exe apps"
                    exeLoader.LoadApps folderConfig
                    exeLoader.ObserveFolders folderConfig
                )
            |> ignore

        ValueTask.FromResult Seq.empty

    override _.Id = nameof WindowsAppsSearchEngine
    override _.Name = "Applications"
    override _.Icon = icon
    override this.Activators = [| DefaultSearchEngineActivator(this) |]

    override _.SearchResultSelected(searchResult) =
        match searchResult with
        | :? ExeLoader.ExeApplication as app -> ExeLoader.runApp app
        | :? Uwp.UwpApplication as app -> UwpLoader.runApp app
        | _ -> ()

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| nameof WindowsAppsSearchEngine |]
    override this.LoadSearchEngine(_, pluginConfigDirectory, logger, _) =
        Logger.logger <- logger

        let settingsViewModel = SettingsViewModel pluginConfigDirectory
        let config = settingsViewModel.Config

        WindowsAppsSearchEngine config,
        SearchEngineFactory.SearchEngineSettings(
            settingsViewModel,
            FuncDataTemplate<SettingsViewModel>(fun vm _ ->
                Settings(DataContext = vm)
            )
        )
