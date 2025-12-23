module Starter.WorkspaceSearchEngine.WorkspaceSources.AndroidStudio

open System
open System.IO
open System.Xml.Linq
open System.Threading

open Starter.WorkspaceSearchEngine
open Starter.WorkspaceSearchEngine.Logger
open FsToolkit.ErrorHandling

let private findWorkspaceDbPath () =
    result {
        let! ideDirectory =
            Path.Combine(
                Environment.GetFolderPath Environment.SpecialFolder.ApplicationData,
                "Google"
            )
            |> Ok
            |> Result.require Directory.Exists "No Google config folder found"

        let! ideDirectories =
            Directory.EnumerateDirectories(ideDirectory, "AndroidStudio" + "*")
            |> Seq.sortByDescending (fun ideConfigDir -> // Max by version number
                ideConfigDir
                |> Path.GetFileName
                |> _.Substring("AndroidStudio".Length)
                |> String.filter Char.IsDigit
                |> fun v ->
                    match Int32.TryParse v with
                    | true, v -> v
                    | false, _ -> 0
            )
            |> Ok
            |> Result.require (Seq.isEmpty >> not) $"No AndroidStudio config folder found"

        return
            ideDirectories
            |> Seq.tryPick (fun ideConfigDir ->
                let configFile = Path.Combine(ideConfigDir, "options", "recentProjects.xml")
                match File.Exists configFile with
                | true -> Some configFile
                | false -> None
            )
            |> Option.defaultWith (fun () ->
                // If no workspaces file found, predict where it will spawn
                // (because when installing the ide the first time, the file may not exists)
                Path.Combine(
                    ideDirectories |> Seq.head,
                    "options",
                    "recentProjects.xml"
                )
            )
    }
    |> Result.teeError (fun e -> logger.Debug $"Failed to find workspaces file for Android Studio: {e}")
    |> Option.ofResult

let private findIdeExecutable () = // TODO: Add logs
    match OperatingSystem.IsLinux() with
    | true -> Common.findCommandPath "studio"
    | false ->
        seq {
            Path.Combine(Environment.SpecialFolder.ProgramFilesX86 |> Environment.GetFolderPath, "Android/Android Studio/bin/studio64.exe")
            Path.Combine(Environment.SpecialFolder.ProgramFiles |> Environment.GetFolderPath, "Android/Android Studio/bin/studio64.exe")
            Path.Combine(Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath, "Programs/Android Studio/bin/studio64.exe")
        }
        |> Seq.tryFind File.Exists

let loadWorkspaces (configFilePath: string) ideExePath =
    task {
        let stream = File.OpenRead configFilePath
        let! document = XElement.LoadAsync(stream, LoadOptions.None, CancellationToken.None)
        stream.Dispose()

        return
            document.Descendants "entry"
            |> Seq.choose (fun e ->
                try
                    let rawPath = e.Attribute("key").Value
                    let displayPath = rawPath.Replace("$USER_HOME$", "~")
                    let path = rawPath.Replace("$USER_HOME$", Environment.GetFolderPath Environment.SpecialFolder.UserProfile)
                    let name =
                        e.Descendants "RecentProjectMetaInfo"
                        |> Seq.tryHead
                        |> Option.bind (fun e ->
                            match e.Attribute "frameTitle" with
                            | null -> None
                            | attr -> Some attr.Value
                        )
                        |> Option.defaultWith (fun () -> Path.GetFileName path)

                    { Id = path
                      Name = name
                      Path = displayPath
                      Open = fun () -> JetBrains.openWorkspace ideExePath path } |> Some
                with _ -> None
            )
    }

let builder : WorkspaceSourceBuilder =
    { Id = "workspace-android-studio"
      Name = "Android Studio"
      ShortName = "studio"
      LoadIcon = fun pluginPath -> Icons.loadIcon pluginPath Icons.IconName.androidStudio
      FindExecutablePath = findIdeExecutable
      FindWorkspacesDb = findWorkspaceDbPath
      LoadWorkspaces = loadWorkspaces
      GetChangesObservable = JetBrains.detectWorkspaceChanges }
