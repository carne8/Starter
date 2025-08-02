module Starter.WorkspaceSearchEngine.Engine

open Starter.SearchEngine
open Starter.WorkspaceSearchEngine

open System.Collections.Generic
open System.Threading

open FsToolkit.ErrorHandling
open R3

type WorkspaceSearchEngine(pluginPath, configDir, logger) =
    inherit StaticSearchEngine(pluginPath, configDir, logger)

    let workspaces = new BehaviorSubject<_>(ResizeArray<ISearchResult>())
    let workspaceSources = WorkspaceSourceProvider.loadWorkspaceSources pluginPath
    let semaphore = new SemaphoreSlim(1, 1)

    let loadWorkspaces source =
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
        workspaceSources |> Array.Parallel.iter (fun source ->
            source |> loadWorkspaces
            source.WorkspacesChanged.Subscribe(fun () -> source |> loadWorkspaces) |> ignore)

        workspaceSources
        |> Seq.cast<ISearchEngineActivator>
        |> this.Activators.OnNext

        struct (Seq.empty, workspaces.AsObservable().Cast<_, IEnumerable<ISearchResult>>()) |> Task.singleton

    override this.SearchResultSelected(selectedSearchResult) =
        match selectedSearchResult with
        | :? SearchResult as workspace -> workspace.Open()
        | _ -> ()

    override this.LoadSettingsControl() = null
