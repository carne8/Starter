namespace Starter.ApplicationSearchEngine

open Starter.SearchEngine

[<RequireQualifiedAccess>]
type EntryPoint =
    | ShellFile of path: string
    | UwpApp of fullName: string

type Application =
    { Id: string
      Name: string
      EntryPoint: EntryPoint
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = "Application"
        member this.Icon = this.Icon

module Constants =
    open Avalonia

    let [<Literal>] IconSize = 70
    let iconPixelSize = PixelSize(IconSize, IconSize)
