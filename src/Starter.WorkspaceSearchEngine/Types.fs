namespace Starter.WorkspaceSearchEngine

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
        member this.ActivatorFilter = [| this.Source |]

    static member fromWorkspace (workspaceSource: WorkspaceSource) (workspace: Workspace) =
        { Id = workspaceSource.Id + workspace.Id
          Name = workspace.Name
          Path = workspace.Path
          Source = workspaceSource
          Open = workspace.Open } :> ISearchResult
