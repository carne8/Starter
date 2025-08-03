module Starter.WorkspaceSearchEngine.WorkspaceSources.VsCode

open Starter.WorkspaceSearchEngine

open System
open System.IO
open System.Text
open System.Text.Json
open System.Diagnostics
open R3

let private formatPath (path: string) =
    if OperatingSystem.IsWindows() then
        let stringBuilder = StringBuilder(path.Length - 1)

        path[1]
        |> Char.ToUpperInvariant
        |> stringBuilder.Append
        |> ignore

        for i in 2..path.Length-1 do
            path[i] |> stringBuilder.Append |> ignore

        stringBuilder.ToString()
    else
        path

let private getWorkspaceDbPath insiders =
    Path.Combine(
        Environment.GetFolderPath Environment.SpecialFolder.ApplicationData,
        (if insiders then "Code - Insiders" else "Code"),
        "User/globalStorage/storage.json"
    )

let private openWorkspace vsCodePath workspacePath =
    ProcessStartInfo(FileName = vsCodePath, Arguments = workspacePath)
    |> Process.Start
    |> _.Dispose()

let loadWorkspaces vsCodePath insiders =
    task {
        let configFilePath = getWorkspaceDbPath insiders

        let! bytes = File.ReadAllBytesAsync configFilePath
        let json = JsonDocument.Parse(bytes)

        let workspacesJson = json.RootElement.GetProperty("profileAssociations").GetProperty("workspaces")
        let mutable workspaceEnumerator = workspacesJson.EnumerateObject()
        let workspaces = ResizeArray()

        while workspaceEnumerator.MoveNext() do
            let property = workspaceEnumerator.Current
            let file = property.Name

            match Uri.TryCreate(file, UriKind.Absolute) with
            | false, _ -> ()
            | true, uri ->
                let path = uri.LocalPath |> formatPath

                { Id = path
                  Name = path |> Path.GetFileName
                  Path = path
                  Open = fun () -> openWorkspace vsCodePath path }
                |> workspaces.Add

        return workspaces :> _ seq
    }

let detectWorkspaceChanges insiders =
    let configPath = getWorkspaceDbPath insiders
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
    ).Debounce(TimeSpan.FromMilliseconds 300),
    watcher :> IDisposable


let findVsCode insiders =
    let relativeInstallPath =
        match insiders with
        | false -> "Microsoft VS Code\Code.exe"
        | true -> "Microsoft VS Code Insiders\Code - Insiders.exe"

    let global64Path = Path.Combine("C:\Program Files", relativeInstallPath)
    let global32Path = Path.Combine("C:\Program Files (x86)", relativeInstallPath)

    if global64Path |> File.Exists then Some global64Path
    elif global32Path |> File.Exists then Some global32Path
    else
        let localPath =
            Path.Combine(
                Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath,
                "Programs",
                relativeInstallPath
            )

        if localPath |> File.Exists then Some localPath
        else None
