namespace Starter.WorkspaceSearchEngine

open System
open System.Threading.Tasks

open Starter.SearchEngine
open FsToolkit.ErrorHandling
open R3

module Logger =
    let mutable logger: Serilog.ILogger = unbox null
open Logger

module Constants =
    let [<Literal>] searchEngineId = "WorkspaceSearchEngine"

/// Represents a workspace from an app like vscode or rider
type Workspace =
    { Id: string
      Name: string
      Path: string
      Open: unit -> unit }

/// Loads workspaces. For instance, it can represent a vscode installation
type WorkspaceSource(
        id: string,
        name: string,
        icon: StarterIconSource,
        loadWorkspaces: unit -> Task<Workspace seq>,
        workspacesChanged: Observable<unit>,
        showIfNoActivator: bool,
        _watcher: IDisposable // Needed for the FileSystemWatcher to not be garbage collected
    ) =
    inherit SearchEngineActivator()

    let mutable showIfNoActivator = showIfNoActivator

    override this.Id = id
    override this.Name = name
    override this.Icon = icon
    override this.SearchEngineId = Constants.searchEngineId

    member this.ShowIfNoActivator
        with get () = showIfNoActivator
        and set v = showIfNoActivator <- v

    member this.LoadWorkspaces() = loadWorkspaces ()
    member this.WorkspacesChanged = workspacesChanged

type WorkspaceSourceBuilder =
    { Id: string
      Name: string
      ShortName: string
      LoadIcon: string -> StarterIconSource
      FindExecutablePath: unit -> string option
      FindWorkspacesDb: unit -> string option
      LoadWorkspaces: string -> string -> Task<Workspace seq>
      GetChangesObservable: string -> Observable<unit> * IDisposable }

    static member build showIfNoActivator pluginPath (builder: WorkspaceSourceBuilder) =
        option {
            let! executablePath =
                builder.FindExecutablePath() |> Option.teeNone (fun () ->
                    logger.Debug $"Executable not found: {builder.Name}"
                )
            let! dbPath =
                builder.FindWorkspacesDb() |> Option.teeNone (fun () ->
                    logger.Debug $"DB path not found while executable exists: {builder.Name}"
                )

            let workspacesChanged, watcher = builder.GetChangesObservable dbPath

            return WorkspaceSource(
                id = builder.Id,
                name = builder.Name,
                icon = (pluginPath |> builder.LoadIcon),
                loadWorkspaces = (fun () -> builder.LoadWorkspaces dbPath executablePath),
                workspacesChanged = workspacesChanged,
                showIfNoActivator = showIfNoActivator,
                _watcher = watcher
            )
        }

type WorkspaceSearchResult(
        id: string,
        name: string,
        path: string,
        source: WorkspaceSource,
        open': unit -> unit
    ) =
    inherit SearchResult()
    let activators = [| source :> SearchEngineActivator |]

    member this.Open() = open' ()

    override this.Id = id
    override this.Name = name
    override this.Description = path
    override this.Keywords = Array.empty
    override this.Icon = source.Icon
    override this.ShowIfNoActivator = source.ShowIfNoActivator
    override this.ActivatorFilter = activators
    override this.ContextMenuLoader = null

    static member fromWorkspace (workspaceSource: WorkspaceSource) (workspace: Workspace) =
        WorkspaceSearchResult(
            id = $"{workspaceSource.Id}:{workspace.Id}",
            name = workspace.Name,
            path = workspace.Path,
            source = workspaceSource,
            open' = workspace.Open
        )
