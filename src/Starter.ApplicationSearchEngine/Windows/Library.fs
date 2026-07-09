namespace Starter.ApplicationSearchEngine.Windows

open System
open System.Threading.Tasks
open Avalonia.Controls.Templates
open R3
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Logger
open Starter.SearchEngine

type WindowsAppsSearchEngine(config: Observable<FolderConfiguration>) =
    let icon = Constants.getIcon ()

    let resultsChanged = DelegateEvent<EventHandler<ISearchResult seq>>()
    let uwpLoader = UwpLoader.UwpAppsLoader()
    let exeLoader = ExeLoader.ExeAppsLoader()

    do
        // Subscribe to loaders events
        exeLoader.Changed.Subscribe(fun () ->
            logger.Verbose "New Exe apps loaded"
            resultsChanged.Trigger
                [| null
                   Seq.append
                    exeLoader.Apps
                    uwpLoader.Apps |]
        ) |> ignore
        uwpLoader.Changed.Subscribe(fun () ->
            logger.Verbose "New UWP apps loaded"
            resultsChanged.Trigger
                [| null
                   Seq.append
                    exeLoader.Apps
                    uwpLoader.Apps |]
        ) |> ignore

    interface IStaticSearchEngine with
        member this.LoadResults() =
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

        member _.Id = nameof WindowsAppsSearchEngine
        member _.Name = "Applications"
        member _.ShortName = "Apps"
        member _.Icon = icon
        member this.Activators = [| DefaultSearchEngineActivator(this) |]

        member _.SearchResultSelected(searchResult) =
            match searchResult with
            | :? ExeLoader.ExeApplication as app -> ExeLoader.runApp app
            | :? Uwp.UwpApplication as app -> UwpLoader.runApp app
            | _ -> ()

        member this.add_Changed _ = ()
        member this.remove_Changed _ = ()

        [<CLIEvent>]
        member this.ResultsChanged = resultsChanged.Publish

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
