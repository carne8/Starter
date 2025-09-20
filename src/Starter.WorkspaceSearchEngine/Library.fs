module Starter.WorkspaceSearchEngine.Engine

open Starter.SearchEngine
open Starter.WorkspaceSearchEngine

open System.Collections.Generic
open System.Threading

open FsToolkit.ErrorHandling
open R3

type WorkspaceSearchEngine(pluginPath, configDir, logger) =
    inherit StaticSearchEngine(pluginPath, configDir, logger)
    do Logger.logger <- logger

    let settings = pluginPath |> Settings.loadSettings
    let _settingsSaver = SettingsSaver(settings, pluginPath)
    let workspaceSources = WorkspaceSourceProvider.loadWorkspaceSources pluginPath settings // Load sources
    let settingsVm = Views.SettingsViewModel(workspaceSources, settings)

    let workspaces = new BehaviorSubject<_>(ResizeArray<ISearchResult>())
    let semaphore = new SemaphoreSlim(1, 1)

    /// Loads workspaces from one source and add them to the list
    let loadWorkspaces (source: WorkspaceSource) =
        task {
            do! semaphore.WaitAsync()
            try
                let! newWorkspaces = source.LoadWorkspaces()

                workspaces.Value.RemoveAll(fun searchResult ->
                    searchResult.Id.StartsWith source.Id
                ) |> ignore

                newWorkspaces
                |> Seq.map (SearchResult.fromWorkspace source)
                |> workspaces.Value.AddRange

                workspaces.Value |> workspaces.OnNext
            finally semaphore.Release() |> ignore
        } |> ignore

    override this.Id = nameof WorkspaceSearchEngine
    override this.Name = "Dev workspaces"
    override this.ShortName = "workspaces"
    override this.Icon = Icons.searchEngineIcon

    override this.LoadResults() =
        // Load workspaces for each source
        workspaceSources |> Array.iter (fun source ->
            source |> loadWorkspaces
            source.WorkspacesChanged.Subscribe(fun () -> source |> loadWorkspaces) |> ignore
        )

        // Tell Starter what are the activators
        workspaceSources
        |> Seq.cast<ISearchEngineActivator>
        |> this.Activators.OnNext

        // Returns the loaded workspaces
        struct (Seq.empty, workspaces.AsObservable().Cast<_, IEnumerable<ISearchResult>>()) |> Task.singleton

    override this.SearchResultSelected(selectedSearchResult) =
        match selectedSearchResult with
        | :? SearchResult as workspace -> workspace.Open()
        | _ -> ()

    override this.LoadSettingsControl() = Views.SettingsView(DataContext = settingsVm)
