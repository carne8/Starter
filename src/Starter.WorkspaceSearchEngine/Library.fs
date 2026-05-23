module Starter.WorkspaceSearchEngine.Engine

open System
open System.Threading.Tasks
open Avalonia.Controls.Templates
open Starter.SearchEngine
open Starter.WorkspaceSearchEngine

open System.Threading

open R3

type WorkspaceSearchEngine(workspaceSources, _settingsSaver) = // Kee a reference to the settings saver
    let workspaces = ResizeArray<ISearchResult>()
    let semaphore = new SemaphoreSlim(1, 1)

    let resultsChanged = DelegateEvent<EventHandler<ISearchResult seq>>()

    /// Loads workspaces from one source and add them to the list
    let loadWorkspaces (source: WorkspaceSource) =
        task {
            do! semaphore.WaitAsync()
            try
                let! newWorkspaces = source.LoadWorkspaces()

                workspaces.RemoveAll(fun searchResult ->
                    searchResult.Id.StartsWith source.Id
                ) |> ignore

                newWorkspaces
                |> Seq.map (SearchResult.fromWorkspace source)
                |> workspaces.AddRange

                resultsChanged.Trigger [| null; workspaces |]
            finally semaphore.Release() |> ignore
        } |> ignore

    interface IStaticSearchEngine with
        member this.Id = Constants.searchEngineId
        member this.Name = "Dev workspaces"
        member this.ShortName = "workspaces"
        member this.Icon = Icons.searchEngineIcon
        member this.Activators = workspaceSources |> Array.map (fun s -> s :> ISearchEngineActivator)

        member this.LoadResults() =
            // Load workspaces for each source
            workspaceSources |> Array.iter (fun source ->
                source |> loadWorkspaces
                source.WorkspacesChanged.Subscribe(fun () -> source |> loadWorkspaces) |> ignore
            )

            ValueTask.FromResult Seq.empty

        member this.SearchResultSelected(selectedSearchResult) =
            match selectedSearchResult with
            | :? SearchResult as workspace -> workspace.Open()
            | _ -> ()

        [<CLIEvent>]
        member this.ResultsChanged = resultsChanged.Publish

        member this.add_Changed _ = ()
        member this.remove_Changed _ = ()


type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| Constants.searchEngineId |]

    override this.LoadSearchEngine(_, pluginConfigDirectory, logger, _) =
        Logger.logger <- logger

        let filePath = pluginConfigDirectory |> Settings.getFilePath
        let settings = filePath |> Settings.loadSettings
        let settingsSaver = SettingsSaver(settings, filePath)

        let workspaceSources = WorkspaceSourceProvider.loadWorkspaceSources pluginPath settings // Load sources
        let settingsVm = Views.SettingsViewModel(workspaceSources, settings)

        WorkspaceSearchEngine(workspaceSources, settingsSaver),
        SearchEngineFactory.SearchEngineSettings(
            settingsVm,
            FuncDataTemplate<Views.SettingsViewModel>(fun vm _ ->
                Views.SettingsView(DataContext = vm)
            )
        )
