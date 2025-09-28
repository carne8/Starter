namespace Starter.ApplicationSearchEngine.Linux

open Starter.SearchEngine

type DesktopApplication =
    { Id: string
      Name: string
      Exec: string
      Icon: StarterIconSource
      Description: string
      Keywords: string array }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = this.Description
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
