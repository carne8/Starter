namespace Starter.WorkspaceSearchEngine

open System.Threading.Tasks
open Starter.SearchEngine
open R3

type Workspace =
    { Id: string
      Name: string
      Path: string
      Open: unit -> unit }

type WorkspaceSource =
    { Icon: StarterIconSource
      LoadWorkspaces: unit -> Task<Workspace seq>
      WorkspacesChanged: Observable<unit> }

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

    static member fromWorkspace (workspaceSource: WorkspaceSource) (workspace: Workspace) =
        { Id = workspace.Id
          Name = workspace.Name
          Path = workspace.Path
          Icon = workspaceSource.Icon
          Open = workspace.Open } :> ISearchResult
