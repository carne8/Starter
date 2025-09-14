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

let private findWorkspaceDbPath insiders =
    let path = Path.Combine(
        Environment.GetFolderPath Environment.SpecialFolder.ApplicationData,
        (if insiders then "Code - Insiders" else "Code"),
        "User/globalStorage/storage.json"
    )

    match path |> File.Exists with
    | true -> Some path
    | false -> None

let private openWorkspace vsCodePath workspacePath =
    ProcessStartInfo(FileName = vsCodePath, Arguments = workspacePath)
    |> Process.Start
    |> _.Dispose()

let loadWorkspaces configPath vsCodePath =
    task {
        let! bytes = File.ReadAllBytesAsync configPath
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
    ).Debounce(TimeSpan.FromMilliseconds 300),
    watcher :> IDisposable


let findVsCode insiders =
    let relativeInstallPath =
        match insiders with
        | false -> "Microsoft VS Code\Code.exe"
        | true -> "Microsoft VS Code Insiders\Code - Insiders.exe"

    seq {
        Path.Combine("C:\Program Files", relativeInstallPath)
        Path.Combine("C:\Program Files (x86)", relativeInstallPath)
        Path.Combine(
            Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath,
            "Programs",
            relativeInstallPath
        )
    }
    |> Seq.tryFind File.Exists

let builder insiders : WorkspaceSourceBuilder =
    { Id = if insiders then "workspace-vscode-insiders:" else "workspace-vscode:"
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
