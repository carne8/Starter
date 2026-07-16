namespace Starter.ApplicationSearchEngine.Windows.Uwp

open Starter.SearchEngine

type UwpApplication =
    { Id: string
      Name: string
      PackageId: string
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = "Application"
        member this.Keywords = Array.empty
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
        member this.HasContextMenu = false
        member this.GetContextMenu() = null
