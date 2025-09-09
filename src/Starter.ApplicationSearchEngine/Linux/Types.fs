namespace Starter.ApplicationSearchEngine.Linux

open Starter.SearchEngine

type DesktopApplication =
    { Id: string
      Name: string
      Exec: string
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = "Application"
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
