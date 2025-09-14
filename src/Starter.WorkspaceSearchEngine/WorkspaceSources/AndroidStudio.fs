module Starter.WorkspaceSearchEngine.WorkspaceSources.AndroidStudio

open Starter.WorkspaceSearchEngine
open System
open System.IO
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
            |> Ok
            |> Result.require (Seq.isEmpty >> not) $"No AndroidStudio config folder found"

        return!
            ideDirectories
            |> Seq.fold
                (fun state ideConfigDir ->
                    let dirName = ideConfigDir |> Path.GetFileName
                    let version =
                        dirName.Substring("AndroidStudio".Length)
                        |> String.filter Char.IsDigit
                        |> function
                            | "" -> 0
                            | v -> try int v with _ -> 0

                    match state with
                    | Some struct (_, stateVersion) when stateVersion >= version -> state
                    | _ ->
                        let configFile = Path.Combine(ideConfigDir, "options/recentProjects.xml")
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

let private findIde () = // TODO: Add logs
    seq {
        Path.Combine(Environment.SpecialFolder.ProgramFilesX86 |> Environment.GetFolderPath, "Android/Android Studio/bin/studio64.exe")
        Path.Combine(Environment.SpecialFolder.ProgramFiles |> Environment.GetFolderPath, "Android/Android Studio/bin/studio64.exe")
        Path.Combine(Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath, "Programs/Android Studio/bin/studio64.exe")
    }
    |> Seq.tryFind File.Exists

let builder : WorkspaceSourceBuilder =
    { Id = "workspace-android-studio:"
      Name = "Android Studio"
      ShortName = "studio"
      LoadIcon = fun pluginPath -> Icons.loadIcon pluginPath Icons.IconName.androidStudio
      FindExecutablePath = findIde
      FindWorkspacesDb = findWorkspaceDbPath
      LoadWorkspaces = JetBrains.loadWorkspaces
      GetChangesObservable = JetBrains.detectWorkspaceChanges }
