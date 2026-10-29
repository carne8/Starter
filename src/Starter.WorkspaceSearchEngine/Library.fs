module Starter.WorkspaceSearchEngine.Engine

open System.Threading.Tasks
open Avalonia.Controls.Templates
open Starter.SearchEngine
open Starter.WorkspaceSearchEngine

open System.Threading

open R3

type WorkspaceSearchEngine(workspaceSources) as this =
    inherit StaticSearchEngine()

    let workspaces = ResizeArray<SearchResult>()
    let semaphore = new SemaphoreSlim(1, 1)

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
                |> Seq.map (fun w ->
                    w
                    |> WorkspaceSearchResult.fromWorkspace source
                    :> SearchResult
                )
                |> workspaces.AddRange

                this.OnResultsChanged()
            finally semaphore.Release() |> ignore
        } |> ignore

    member this.OnResultsChanged() = this.OnResultsChanged(workspaces)

    override this.Id = Constants.searchEngineId
    override this.Name = "Dev workspaces"
    override this.Icon = Icons.searchEngineIcon
    override this.Activators = workspaceSources |> Array.map (fun s -> s :> SearchEngineActivator)

    override this.LoadResults() =
        // Load workspaces for each source
        workspaceSources |> Array.iter (fun source ->
            source |> loadWorkspaces
            source.WorkspacesChanged.Subscribe(fun () -> source |> loadWorkspaces) |> ignore
        )

        ValueTask.FromResult Seq.empty

    override this.SearchResultSelected(selectedSearchResult) =
        match selectedSearchResult with
        | :? WorkspaceSearchResult as workspace -> workspace.Open()
        | _ -> ()


type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| Constants.searchEngineId |]

    override this.LoadSearchEngine(_, pluginConfigDirectory, logger, _) =
        Logger.logger <- logger

        let filePath = pluginConfigDirectory |> Settings.getFilePath
        let settings = filePath |> Settings.loadSettings

        let workspaceSources = WorkspaceSourceProvider.loadWorkspaceSources pluginPath settings // Load sources
        let settingsVm = Views.SettingsViewModel(workspaceSources, filePath, settings)

        WorkspaceSearchEngine(workspaceSources),
        SearchEngineFactory.SearchEngineSettings(
            settingsVm,
            FuncDataTemplate<Views.SettingsViewModel>(fun vm _ ->
                Views.SettingsView(DataContext = vm)
            )
        )
