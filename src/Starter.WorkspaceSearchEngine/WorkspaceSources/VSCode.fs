module Starter.WorkspaceSearchEngine.WorkspaceSources.VsCode

open Starter.WorkspaceSearchEngine

open System
open System.IO
open System.Text
open System.Text.Json
open System.Diagnostics

let formatPath (path: string) =
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

let openWorkspace vsCodePath workspacePath =
    ProcessStartInfo(FileName = vsCodePath, Arguments = workspacePath)
    |> Process.Start
    |> _.Dispose()

let loadWorkspaces vsCodePath insiders =
    task {
        let configFilePath =
            Path.Combine(
                Environment.GetFolderPath Environment.SpecialFolder.ApplicationData,
                (if insiders then "Code - Insiders" else "Code"),
                "User/globalStorage/storage.json"
            )

        use stream = File.OpenRead configFilePath
        let! json = JsonDocument.ParseAsync(stream)
        let workspacesJson = json.RootElement.GetProperty("profileAssociations").GetProperty("workspaces")
        let mutable workspaceEnumerator = workspacesJson.EnumerateObject()
        let workspaces = ResizeArray()

        let idPrefix = if insiders then "VsCode-insiders" else "VsCode"

        while workspaceEnumerator.MoveNext() do
            let property = workspaceEnumerator.Current
            let file = property.Name

            match Uri.TryCreate(file, UriKind.Absolute) with
            | false, _ -> ()
            | true, uri ->
                let path = uri.LocalPath |> formatPath

                { Id = idPrefix + file
                  Name = path |> Path.GetFileName
                  Path = path
                  Open = fun () -> openWorkspace vsCodePath path }
                |> workspaces.Add

        return workspaces :> _ seq
    }

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
