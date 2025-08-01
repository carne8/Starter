module Starter.WorkspaceSearchEngine.Engine

open Starter.SearchEngine
open Starter.WorkspaceSearchEngine

open System.Collections.Generic
open FsToolkit.ErrorHandling
open R3

type WorkspaceSearchEngine(pluginPath, configDir, logger) =
    inherit StaticSearchEngine(pluginPath, configDir, logger)

    let workspaces = new BehaviorSubject<_>(ResizeArray<ISearchResult>())
    let workspaceSources = WorkspaceSourceProvider.loadWorkspaceSources pluginPath

    do workspaces.AsObservable().Cast<_, IEnumerable<ISearchResult>>().Subscribe(printfn "aaaaa: %A") |> ignore

    override this.Id = nameof WorkspaceSearchEngine
    override this.Name = "Dev workspaces"
    override this.ShortName = "workspaces"
    override this.Icon = Icons.searchEngineIcon

    override this.LoadResults() =
        workspaceSources |> Array.Parallel.iter (fun source ->
            source.LoadWorkspaces()
            |> Task.map (fun newWorkspaces ->
                newWorkspaces
                |> Seq.map (SearchResult.fromWorkspace source)
                |> workspaces.Value.AddRange

                workspaces.Value |> workspaces.OnNext
            )
            |> ignore
        )

        struct (Seq.empty, workspaces.AsObservable().Cast<_, IEnumerable<ISearchResult>>()) |> Task.singleton

    override this.SearchResultSelected(selectedSearchResult) =
        match selectedSearchResult with
        | :? SearchResult as workspace -> workspace.Open()
        | _ -> ()

    override this.LoadSettingsControl() = null
