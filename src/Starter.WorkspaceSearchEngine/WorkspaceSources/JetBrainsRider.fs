module Starter.WorkspaceSearchEngine.WorkspaceSources.JetBrains.Rider

open Starter.WorkspaceSearchEngine

open System
open System.IO
open System.Diagnostics
open System.Threading
open System.Xml.Linq

open FsToolkit.ErrorHandling
open R3

let getWorkspaceDbPath () =
    result {
        let! idePaths =
            Path.Combine(
                Environment.GetFolderPath Environment.SpecialFolder.ApplicationData,
                "JetBrains"
            )
            |> Ok
            |> Result.require Directory.Exists "No IDE config present"

        let! riderDirectories =
            Directory.EnumerateDirectories(idePaths, "Rider*")
            |> Ok
            |> Result.require (Seq.isEmpty >> not) "No Rider config found"

        return!
            riderDirectories
            |> Seq.fold
                (fun state riderConfigDir ->
                    let dirName = riderConfigDir |> Path.GetFileName
                    let version =
                        dirName.Substring("Rider".Length)
                        |> String.filter Char.IsDigit
                        |> int

                    match state with
                    | Some struct (_, stateVersion) when stateVersion >= version -> state
                    | _ ->
                        let configFile = Path.Combine(riderConfigDir, "options", "recentSolutions.xml")
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

let private openWorkspace riderPath workspacePath =
    ProcessStartInfo(FileName = riderPath, Arguments = workspacePath)
    |> Process.Start
    |> _.Dispose()

let loadWorkspaces (configFilePath: string) riderPath =
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
                      Open = fun () -> openWorkspace riderPath path } |> Some
                with _ -> None
            )
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


let findRider () = // TODO: Add logs
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
            match Directory.GetDirectories(dir, "rider*") with
            | [| |] -> None
            | arr -> Some arr

        let! struct (exePath, _) =
            directories |> Array.fold
                (fun state dir ->
                    let exeFile = Path.Combine(dir, "bin/rider64.exe")

                    match exeFile |> File.Exists with
                    | false -> state
                    | true ->
                        let version =
                            dir
                            |> Path.GetFileName
                            |> _.Substring("rider".Length)
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
                "Programs/Rider/bin/rider64.exe"
            )

        match path |> File.Exists with
        | false -> None
        | true -> Some path
    )
