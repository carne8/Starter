module Starter.WorkspaceSearchEngine.WorkspaceSources.JetBrains

open Starter.WorkspaceSearchEngine

open System
open System.IO
open System.Diagnostics
open System.Threading
open System.Xml.Linq

open FsToolkit.ErrorHandling
open R3

let private findWorkspaceDbPath ideName ideProjectsFileName =
    result {
        let! idePaths =
            Path.Combine(
                Environment.GetFolderPath Environment.SpecialFolder.ApplicationData,
                "JetBrains"
            )
            |> Ok
            |> Result.require Directory.Exists "No JetBrains config folder found"

        let! ideDirectories =
            Directory.EnumerateDirectories(idePaths, ideName + "*")
            |> Ok
            |> Result.require (Seq.isEmpty >> not) $"No {ideName} config folder found"

        return!
            ideDirectories
            |> Seq.fold
                (fun state ideConfigDir ->
                    let dirName = ideConfigDir |> Path.GetFileName
                    let version =
                        dirName.Substring(ideName.Length)
                        |> String.filter Char.IsDigit
                        |> int

                    match state with
                    | Some struct (_, stateVersion) when stateVersion >= version -> state
                    | _ ->
                        let configFile = Path.Combine(ideConfigDir, "options", ideProjectsFileName)
                        if configFile |> File.Exists then
                            Some struct (configFile, version)
                        else
                            None
                )
                None
            |> Result.requireSome "No IDE config present"
            |> Result.map (fun struct (path, _) -> path)
    }
    |> Option.ofResult

let private openWorkspace ideExePath workspacePath =
    ProcessStartInfo(FileName = ideExePath, Arguments = workspacePath)
    |> Process.Start
    |> _.Dispose()

let private loadWorkspaces (configFilePath: string) ideExePath =
    task {
        let stream = File.OpenRead configFilePath
        let! document = XElement.LoadAsync(stream, LoadOptions.None, CancellationToken.None)
        stream.Dispose()

        return
            document.Descendants("entry")
            |> Seq.choose (fun e ->
                try
                    let path = e.Attribute("key").Value
                    { Id = path
                      Name = path |> Path.GetFileName
                      Path = path
                      Open = fun () -> openWorkspace ideExePath path } |> Some
                with _ -> None
            )
    }

let private detectWorkspaceChanges (configPath: string) =
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


let private findIde ideName ideExeName = // TODO: Add logs
    seq {
        Path.Combine(Environment.SpecialFolder.ProgramFilesX86 |> Environment.GetFolderPath, "JetBrains/Installations")
        Path.Combine(Environment.SpecialFolder.ProgramFiles |> Environment.GetFolderPath, "JetBrains/Installations")
        Path.Combine(Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath, "JetBrains/Installations")
    }
    |> Seq.tryPick (fun dir -> option {
        do! dir
            |> Directory.Exists
            |> function false -> None | true -> Some ()

        let! directories =
            match Directory.GetDirectories(dir, ideName + "*", EnumerationOptions(MatchCasing = MatchCasing.CaseInsensitive)) with
            | [| |] -> None
            | arr -> Some arr

        let! struct (exePath, _) =
            directories |> Array.fold
                (fun state dir ->
                    let exeFile = Path.Combine(dir, "bin", ideExeName)

                    match exeFile |> File.Exists with
                    | false -> state
                    | true ->
                        let version =
                            dir
                            |> Path.GetFileName
                            |> _.Substring(ideName.Length)
                            |> int

                        match state with
                        | Some struct (_, stateVersion) when stateVersion >= version -> state
                        | _ -> Some struct (exeFile, version)
                )
                None

        return exePath
    })
    |> Option.orElseWith (fun () ->
        let path =
            Path.Combine(
                Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath,
                "Programs", ideName, "bin", ideExeName
            )

        match path |> File.Exists with
        | false -> None
        | true -> Some path
    )

let getBuilder
    (ideName: string)
    (ideShortName: string)
    ideExeName
    workspaceIdeName
    ideProjectsFileName
    iconName
    : WorkspaceSourceBuilder
    =
    { Id = $"workspace-jetbrains-{ideName.ToLowerInvariant()}:"
      Name = "JetBrains " + ideName
      ShortName = ideShortName
      LoadIcon = fun pluginPath -> Icons.loadIcon pluginPath iconName
      FindExecutablePath = fun () -> findIde ideName ideExeName
      FindWorkspacesDb = fun () -> findWorkspaceDbPath workspaceIdeName ideProjectsFileName
      LoadWorkspaces = loadWorkspaces
      GetChangesObservable = detectWorkspaceChanges }
