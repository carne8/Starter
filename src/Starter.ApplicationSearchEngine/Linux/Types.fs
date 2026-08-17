namespace Starter.ApplicationSearchEngine.Linux

open Starter.SearchEngine

type DesktopApplication =
    { Id: string
      Name: string
      Exec: string
      DesktopFile: string
      WorkingDirectory: string ValueOption
      Icon: StarterIconSource
      Description: string
      Keywords: string array | null }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = this.Description
        member this.Keywords = this.Keywords
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
        member this.HasContextMenu = false
        member this.GetContextMenu() = null
