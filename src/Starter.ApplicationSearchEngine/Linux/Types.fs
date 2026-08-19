namespace Starter.ApplicationSearchEngine.Linux

open System.Diagnostics
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

    member this.Launch() =
        ProcessStartInfo(
            FileName = "setsid",
            Arguments = this.Exec,
            #if DEBUG // Hide process logs
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            #endif
            CreateNoWindow = true
        )
        |> Process.Start
        |> function null -> () | d -> d.Dispose()

    member this.OpenDesktopFile() =
        ProcessStartInfo(
            FileName = this.DesktopFile,
            UseShellExecute = true
        )
        |> Process.Start
        |> function null -> () | d -> d.Dispose()

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = this.Description
        member this.Keywords = this.Keywords
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
        member app.ContextMenuLoader =
            { new IContextMenuLoader with
                member _.LoadItems(
                    _platformHandle,
                    itemsListed,
                    resultLoaded,
                    resultFailed,
                    completed)
                    =
                    itemsListed.Invoke(2)
                    resultLoaded.Invoke([|
                        struct (
                            { new IContextMenuEntry with
                                member _.Id = null
                                member _.Name = "Open"
                                member _.Description = null
                                member _.Keywords = [| "launch"; "start" |]
                                member _.Icon = StarterIconSource.Empty
                                member _.Invoke() = app.Launch(); null } :> IContextMenuResult,
                            0
                        )

                        struct (
                            { new IContextMenuEntry with
                                member _.Id = null
                                member _.Name = "Open .desktop file"
                                member _.Description = "Open the .desktop associated file"
                                member _.Keywords = [| "file"; ".desktop" |]
                                member _.Icon = StarterIconSource.Empty
                                member _.Invoke() = app.OpenDesktopFile(); null }  :> IContextMenuResult,
                            1
                        )
                    |])
            }
