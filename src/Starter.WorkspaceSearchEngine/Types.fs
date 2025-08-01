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
      Icon: StarterIconSource
      LoadWorkspaces: unit -> Task<Workspace seq>
      WorkspacesChanged: Observable<unit>
      /// Needed for the FileSystemWatcher to not be garbage collected
      Watcher: IDisposable }

type SearchResult =
    { Id: string
      Name: string
      Path: string
      Icon: StarterIconSource
      Open: unit -> unit }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = this.Path
        member this.Icon = this.Icon
        member this.ActivatorFilter = Array.empty

    static member prefixId = "workspace:"

    static member fromWorkspace (workspaceSource: WorkspaceSource) (workspace: Workspace) =
        { Id = SearchResult.prefixId + workspaceSource.Id + workspace.Id
          Name = workspace.Name
          Path = workspace.Path
          Icon = workspaceSource.Icon
          Open = workspace.Open } :> ISearchResult
