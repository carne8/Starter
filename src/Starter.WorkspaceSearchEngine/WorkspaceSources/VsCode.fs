module Starter.WorkspaceSearchEngine.WorkspaceSources.VsCode

open Starter.WorkspaceSearchEngine
open Starter.WorkspaceSearchEngine.WorkspaceSources.Common

open System
open System.IO
open System.Text
open System.Text.Json
open System.Diagnostics
open R3
open FsToolkit.ErrorHandling

/// Capitalize the drive letter on Windows
let private formatFilePath (path: string) =
    if OperatingSystem.IsWindows() then
        let stringBuilder = StringBuilder(path.Length)

        path[0]
        |> Char.ToUpperInvariant
        |> stringBuilder.Append
        |> ignore

        stringBuilder.Append(path, 1, path.Length-1) |> ignore
        stringBuilder.ToString()
    else
        path

let private formatRemoteName (remote: string) =
    let linuxIdx = remote.IndexOf("linux", StringComparison.InvariantCultureIgnoreCase)

    match linuxIdx with
    | -1 -> remote
    | i ->
        let sb = StringBuilder(remote)
        sb.Remove(0, 1) |> ignore
        sb.Insert(0, Char.ToUpper remote[0]) |> ignore

        sb.Remove(i, 1) |> ignore
        sb.Insert(i, 'L') |> ignore
        sb.ToString()

let private findWorkspaceDbPath insiders =
    let path = Path.Combine(
        Environment.GetFolderPath Environment.SpecialFolder.ApplicationData,
        (if insiders then "Code - Insiders" else "Code"),
        "User/globalStorage/storage.json"
    )

    match path |> File.Exists with
    | true -> Some path
    | false -> None

let private openWorkspace vsCodePath args =
    ProcessStartInfo(FileName = vsCodePath, Arguments = args)
    |> Process.Start
    |> function null -> () | d -> d.Dispose()

let loadWorkspaces configPath vsCodePath =
    task {
        let! bytes = File.ReadAllBytesAsync configPath
        let json = JsonDocument.Parse(bytes)

        let workspacesJson = json.RootElement.GetProperty("profileAssociations").GetProperty("workspaces")
        let mutable workspaceEnumerator = workspacesJson.EnumerateObject()
        let workspaces = ResizeArray()

        while workspaceEnumerator.MoveNext() do
            let property = workspaceEnumerator.Current
            let file = property.Name |> Uri.UnescapeDataString

            let protocol =
                file.TryIndexOf ':'
                |> ValueOption.map (fun idx -> file.Substring(0, idx))

            match protocol with
            | ValueSome "file" ->
                let pathStartIdx = "file:///".Length
                let path = file.Substring(pathStartIdx)

                { Id = path
                  Name = path |> Path.GetFileName
                  Path = path |> formatFilePath
                  Open = fun () -> openWorkspace vsCodePath $"\"{path}\"" }
                |> workspaces.Add

            | ValueSome "vscode-remote" ->
                    let protocolSeparatorIdx = "vscode-remote://".Length
                    file.TryIndexOf('/', protocolSeparatorIdx + 1)
                    |> ValueOption.iter (fun remoteSeparatorIdx ->
                        let remote = file.Substring(protocolSeparatorIdx, remoteSeparatorIdx - protocolSeparatorIdx)
                        let path = file.Substring(remoteSeparatorIdx)
                        let name =
                            if remote.Contains "+" then
                                let parts = remote.Split '+'
                                $"{parts[0].ToUpper()} - {formatRemoteName parts[1]} - {path |> Path.GetFileName}"
                            else
                                $"{remote} - {path |> Path.GetFileName}"

                        { Id = path
                          Name = name
                          Path = path
                          Open = fun () -> openWorkspace vsCodePath $"--remote {remote} \"{path}\"" }
                        |> workspaces.Add
                    )
            | _ -> ()

        return workspaces :> _ seq
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


let findVsCode insiders =
    match OperatingSystem.IsLinux() with
    | true -> findCommandPath (if insiders then "code-insiders" else "code")
    | false ->
        let relativeInstallPath =
            match insiders with
            | false -> "Microsoft VS Code\\Code.exe"
            | true -> "Microsoft VS Code Insiders\\Code - Insiders.exe"

        seq {
            Path.Combine("C:\\Program Files", relativeInstallPath)
            Path.Combine("C:\\Program Files (x86)", relativeInstallPath)
            Path.Combine(
                Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath,
                "Programs",
                relativeInstallPath
            )
        }
        |> Seq.tryFind File.Exists

let builder insiders : WorkspaceSourceBuilder =
    { Id = if insiders then "workspace-vscode-insiders" else "workspace-vscode"
      Name = if insiders then "Visual Studio Code Insiders" else "Visual Studio Code"
      ShortName = "vscode"
      LoadIcon =
        if insiders then
            fun pluginPath -> Icons.loadIcon pluginPath Icons.IconName.vsCodeInsiders
        else
            fun pluginPath -> Icons.loadIcon pluginPath Icons.IconName.vsCode
      FindExecutablePath = fun () -> findVsCode insiders
      FindWorkspacesDb = fun () -> findWorkspaceDbPath insiders
      LoadWorkspaces = loadWorkspaces
      GetChangesObservable = detectWorkspaceChanges }
