module Starter.WorkspaceSearchEngine.WorkspaceSources.WindowsTerminal

open System
open System.Diagnostics
open System.IO
open R3
open Starter.WorkspaceSearchEngine
open Starter.WorkspaceSearchEngine.WorkspaceSources.Common

open System.Text.Json
open FsToolkit.ErrorHandling

let openWorkspace exePath profile =
    ProcessStartInfo(FileName = exePath, Arguments = $"--profile \"{profile}\"")
    |> Process.Start
    |> function null -> () | d -> d.Dispose()

let findExe () =
    if OperatingSystem.IsWindows() then
        findCommandPath "wt.exe"
    else None

let findWorkspaceDbPath () =
    Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json"
    )
    |> Some
    |> Option.filter File.Exists

let loadWorkspaces configPath wt =
    task {
        use stream = File.Open(configPath, FileMode.Open)
        let! json = JsonDocument.ParseAsync(stream)

        let mutable profilesEnumerator =
            json.RootElement
                .GetProperty("profiles")
                .GetProperty("list")
                .EnumerateArray()
                .GetEnumerator()

        let profiles = ResizeArray()

        while profilesEnumerator.MoveNext() do
            voption {
                let p = profilesEnumerator.Current

                // Check of hidden
                do! p.TryGetProperty("hidden")
                    |> ValueOption.ofPair
                    |> ValueOption.map _.GetBoolean()
                    |> ValueOption.bind (function
                        | false -> ValueSome ()
                        | true -> ValueNone
                    )

                let! guid =
                    p.TryGetProperty("guid")
                    |> ValueOption.ofPair
                    |> ValueOption.map _.GetString()

                let! name =
                    p.TryGetProperty("name")
                    |> ValueOption.ofPair
                    |> ValueOption.map _.GetString()

                // let icon =
                //     p.TryGetProperty("icon")
                //     |> ValueOption.ofPair
                //     |> ValueOption.map _.GetString()

                return { Id = guid
                         Name = name
                         Open = fun () -> openWorkspace wt guid
                         Path = "Open in terminal"  }
            }
            |> ValueOption.iter profiles.Add

        profilesEnumerator.Dispose()
        return profiles :> _ seq
    }

let detectWorkspaceChanges (configPath: string) =
    let watcher =
        new FileSystemWatcher(
            configPath |> Path.GetDirectoryName,
            configPath |> Path.GetFileName,
            NotifyFilter = (NotifyFilters.FileName ||| NotifyFilters.LastWrite),
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        )

    Observable.Merge(
        watcher.Renamed.ToObservable().Select(ignore),
        watcher.Changed.ToObservable().Select(ignore)
    ).Debounce(TimeSpan.FromMilliseconds 300L),
    watcher :> IDisposable

let builder : WorkspaceSourceBuilder =
    { Id = "windows-terminal"
      Name = "Windows Terminal"
      LoadIcon = fun pluginPath -> Icons.loadIcon pluginPath Icons.IconName.windowsTerminal
      FindExecutablePath = findExe
      FindWorkspacesDb = findWorkspaceDbPath
      LoadWorkspaces = loadWorkspaces
      GetChangesObservable = detectWorkspaceChanges }
