namespace Starter.WorkspaceSearchEngine

open FsToolkit.ErrorHandling
open Starter.SearchEngine
open System
open System.Threading.Tasks
open R3

type Workspace =
    { Id: string
      Name: string
      Path: string
      Open: unit -> unit }

type WorkspaceSource =
    { Id: string
      Name: string
      ShortName: string
      Icon: StarterIconSource
      LoadWorkspaces: unit -> Task<Workspace seq>
      WorkspacesChanged: Observable<unit>
      /// Needed for the FileSystemWatcher to not be garbage collected
      Watcher: IDisposable }

    interface ISearchEngineActivator with
        member this.Id = this.Id
        member this.Icon = this.Icon
        member this.Name = this.Name
        member this.ShortName = this.ShortName
        member this.SearchEngineId = "WorkspaceSearchEngine" // TODO

type WorkspaceSourceBuilder =
    { Id: string
      Name: string
      ShortName: string
      LoadIcon: string -> StarterIconSource
      FindExecutablePath: unit -> string option
      FindWorkspacesDb: unit -> string option
      LoadWorkspaces: string -> string -> Task<Workspace seq>
      GetChangesObservable: string -> Observable<unit> * IDisposable  }

    static member build pluginPath (builder: WorkspaceSourceBuilder) =
        option {
            let! executablePath = builder.FindExecutablePath()
            let! dbPath = builder.FindWorkspacesDb()
            let workspacesChanged, watcher = builder.GetChangesObservable dbPath

            return
                { Id = builder.Id
                  Name = builder.Name
                  ShortName = builder.ShortName
                  Icon = pluginPath |> builder.LoadIcon
                  LoadWorkspaces = fun () -> builder.LoadWorkspaces dbPath executablePath
                  WorkspacesChanged = workspacesChanged
                  Watcher = watcher }
        }


type SearchResult =
    { Id: string
      Name: string
      Path: string
      Source: WorkspaceSource
      Open: unit -> unit }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = this.Path
        member this.Icon = this.Source.Icon
        member this.ShowIfNoActivator = false
        member this.ActivatorFilter = [| this.Source |]

    static member fromWorkspace (workspaceSource: WorkspaceSource) (workspace: Workspace) =
        { Id = workspaceSource.Id + workspace.Id
          Name = workspace.Name
          Path = workspace.Path
          Source = workspaceSource
          Open = workspace.Open } :> ISearchResult
