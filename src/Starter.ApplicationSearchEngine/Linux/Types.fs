namespace Starter.ApplicationSearchEngine.Linux

open System.Diagnostics
open System.Threading.Tasks
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

    member this.Launch(asAdmin) =
        let display = System.Environment.GetEnvironmentVariable "DISPLAY"
        let xAuth = System.Environment.GetEnvironmentVariable "XAUTHORITY"

        let startInfo =
            ProcessStartInfo(
                FileName = "setsid",
                Arguments = (if asAdmin then $"pkexec env DISPLAY={display} XAUTHORITY={xAuth} {this.Exec}" else this.Exec),
                #if DEBUG // Hide process logs
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                #endif
                CreateNoWindow = true
            )

        startInfo
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
                member _.LoadItems(_platformHandle, _addItem) =
                    [| { new IContextMenuEntry with
                            member _.Loaded = true
                            member _.add_OnLoaded value = ()
                            member _.remove_OnLoaded value = ()
                            member _.IsSeparator = false
                            member _.Id = null
                            member _.Name = "Open"
                            member _.Description = null
                            member _.Keywords = [| "launch"; "start" |]
                            member _.Icon = StarterIconSource.Empty
                            member _.Invoke() = app.Launch(false); null }

                       { new IContextMenuEntry with
                            member _.Loaded = true
                            member _.IsSeparator = false
                            member _.add_OnLoaded value = ()
                            member _.remove_OnLoaded value = ()
                            member _.Id = null
                            member _.Name = "Open as root"
                            member _.Description = null
                            member _.Keywords = [| "launch"; "start"; "root"; "sudo" |]
                            member _.Icon = StarterIconSource.Empty
                            member _.Invoke() = app.Launch(true); null }

                       { new IContextMenuEntry with
                            member _.Loaded = true
                            member _.add_OnLoaded value = ()
                            member _.remove_OnLoaded value = ()
                            member _.IsSeparator = false
                            member _.Id = null
                            member _.Name = "Open .desktop file"
                            member _.Description = "Open the .desktop associated file"
                            member _.Keywords = [| "file"; ".desktop" |]
                            member _.Icon = StarterIconSource.Empty
                            member _.Invoke() = app.OpenDesktopFile(); null } |]
                    |> Task.FromResult
            }
