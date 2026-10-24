namespace Starter.ApplicationSearchEngine.Linux

open System.Diagnostics
open System.Threading.Tasks
open Starter.SearchEngine

type DesktopApplication(
    Id: string,
    Name: string,
    Exec: string,
    DesktopFile: string,
    WorkingDirectory: string ValueOption,
    Icon: StarterIconSource,
    Description: string,
    Keywords: string array | null
) =
    inherit SearchResult()

    member this.Launch(asAdmin) =
        let display = System.Environment.GetEnvironmentVariable "DISPLAY"
        let xAuth = System.Environment.GetEnvironmentVariable "XAUTHORITY"

        let startInfo =
            ProcessStartInfo(
                FileName = "setsid",
                Arguments = (if asAdmin then $"pkexec env DISPLAY={display} XAUTHORITY={xAuth} {Exec}" else Exec),
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
            FileName = DesktopFile,
            UseShellExecute = true
        )
        |> Process.Start
        |> function null -> () | d -> d.Dispose()

    override this.Id = Id
    override this.Name = Name
    override this.Description = Description
    override this.Keywords = Keywords
    override this.Icon = Icon
    override this.ShowIfNoActivator = true
    override this.ActivatorFilter = Array.empty
    override app.ContextMenuLoader =
        { new IContextMenuLoader with
            member _.LoadItems(_platformHandle) =
                [| { new IContextMenuEntry with
                        member _.Status = ContextMenuStatus.Loaded
                        member _.Id = null
                        member _.Name = "Open"
                        member _.Description = null
                        member _.Keywords = [| "launch"; "start" |]
                        member _.Icon = StarterIconSource.Empty
                        member _.Invoke() = app.Launch(false); null }

                   { new IContextMenuEntry with
                        member _.Status = ContextMenuStatus.Loaded
                        member _.Id = null
                        member _.Name = "Open as root"
                        member _.Description = null
                        member _.Keywords = [| "launch"; "start"; "root"; "sudo" |]
                        member _.Icon = StarterIconSource.Empty
                        member _.Invoke() = app.Launch(true); null }

                   { new IContextMenuEntry with
                        member _.Status = ContextMenuStatus.Loaded
                        member _.Id = null
                        member _.Name = "Open .desktop file"
                        member _.Description = "Open the .desktop associated file"
                        member _.Keywords = [| "file"; ".desktop" |]
                        member _.Icon = StarterIconSource.Empty
                        member _.Invoke() = app.OpenDesktopFile(); null } |]
                |> System.Collections.ObjectModel.ObservableCollection
                |> Task.FromResult
        }
