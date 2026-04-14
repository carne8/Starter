module Starter.WorkspaceSearchEngine.WorkspaceSources.JetBrains

open Starter.WorkspaceSearchEngine
open Starter.WorkspaceSearchEngine.Logger

open System
open System.IO
open System.Diagnostics
open System.Threading
open System.Xml.Linq

open FsToolkit.ErrorHandling
open R3

[<RequireQualifiedAccess>]
type JetBrainsIDE =
    | Rider
    | PyCharm
    | IntelliJ of ultimateEdition: bool
    | GoLand
    | PhpStorm
    | WebStorm
    | RubyMine
    | RustRover
    | CLion

    static member getName = function
        | Rider -> "Rider"
        | PyCharm -> "PyCharm"
        | IntelliJ false -> "IntelliJ IDEA Community Edition"
        | IntelliJ true -> "IntelliJ IDEA Ultimate"
        | GoLand -> "GoLand"
        | PhpStorm -> "PhpStorm"
        | WebStorm -> "WebStorm"
        | RubyMine -> "RubyMine"
        | RustRover -> "RustRover"
        | CLion -> "CLion"

    static member getShortName = function
        | Rider -> "Rider"
        | PyCharm -> "PyCharm"
        | IntelliJ _ -> "IntelliJ"
        | GoLand -> "GoLand"
        | PhpStorm -> "PhpStorm"
        | WebStorm -> "WebStorm"
        | RubyMine -> "RubyMine"
        | RustRover -> "RustRover"
        | CLion -> "CLion"

    static member getExecutableNameWindows = function
        | Rider -> "rider64.exe"
        | PyCharm -> "pycharm64.exe"
        | IntelliJ _ -> "idea64.exe"
        | GoLand -> "goland64.exe"
        | PhpStorm -> "phpstorm64.exe"
        | WebStorm -> "webstorm64.exe"
        | RubyMine -> "rubymine64.exe"
        | RustRover -> "rustrover64.exe"
        | CLion -> "clion64.exe"

    static member getExecutableNameLinux = function
        | Rider -> "rider"
        | PyCharm -> "pycharm"
        | IntelliJ _ -> "idea"
        | GoLand -> "goland"
        | PhpStorm -> "phpstorm"
        | WebStorm -> "webstorm"
        | RubyMine -> "rubymine"
        | RustRover -> "rustrover"
        | CLion -> "clion"

    static member getWorkspaceIdeName = function
        | Rider -> "Rider"
        | PyCharm -> "PyCharm"
        | IntelliJ _ -> "IdeaIC"
        | GoLand -> "GoLand"
        | PhpStorm -> "PhpStorm"
        | WebStorm -> "WebStorm"
        | RubyMine -> "RubyMine"
        | RustRover -> "RustRover"
        | CLion -> "CLion"

    static member getWorkspacesFileName = function
        | Rider -> "recentSolutions.xml"
        | _ -> "recentProjects.xml"

    static member getIconName = function
        | Rider -> Icons.IconName.rider
        | PyCharm -> Icons.IconName.pyCharm
        | IntelliJ _ -> Icons.IconName.intelliJ
        | GoLand -> Icons.IconName.goLand
        | PhpStorm -> Icons.IconName.phpStorm
        | WebStorm -> Icons.IconName.webStorm
        | RubyMine -> Icons.IconName.rubyMine
        | RustRover -> Icons.IconName.rustRover
        | CLion -> Icons.IconName.cLion

let private findWorkspaceDbPath ide =
    let ideName = JetBrainsIDE.getWorkspaceIdeName ide

    result {
        let! idesPath =
            Path.Combine(
                Environment.GetFolderPath Environment.SpecialFolder.ApplicationData,
                "JetBrains"
            )
            |> Ok
            |> Result.require Directory.Exists "No JetBrains config folder found"

        let! ideDirectories =
            Directory.EnumerateDirectories(idesPath, ideName + "*")
            |> Seq.sortByDescending (fun ideConfigDir -> // Max by version number
                ideConfigDir
                |> Path.GetFileName
                |> _.Substring(ideName.Length)
                |> String.filter Char.IsDigit
                |> fun v ->
                    match Int32.TryParse v with
                    | true, v -> v
                    | false, _ -> 0
            )
            |> Ok
            |> Result.require (Seq.isEmpty >> not) $"No {ideName} config folder found"

        return
            ideDirectories
            |> Seq.tryPick (fun ideConfigDir ->
                let configFile = Path.Combine(ideConfigDir, "options", JetBrainsIDE.getWorkspacesFileName ide)
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
                    JetBrainsIDE.getWorkspacesFileName ide
                )
            )
    }
    |> Result.teeError (fun e -> logger.Debug $"Failed to find workspaces file for {ideName}: {e}")
    |> Option.ofResult

let openWorkspace ideExePath workspacePath = // TODO: Use setsid on Linux
    ProcessStartInfo(FileName = ideExePath, Arguments = $"\"{workspacePath}\"")
    |> Process.Start
    |> function null -> () | d -> d.Dispose()

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
                            match e.Attribute "displayName" with
                            | null -> None
                            | attr -> Some attr.Value
                        )
                        |> Option.defaultWith (fun () -> Path.GetFileName path)

                    { Id = path
                      Name = name
                      Path = displayPath
                      Open = fun () -> openWorkspace ideExePath path } |> Some
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
    ).Debounce(TimeSpan.FromMilliseconds 300L),
    watcher :> IDisposable


let private findIdeExecutable ide =
    match OperatingSystem.IsLinux() with
    | true ->
        ide
        |> JetBrainsIDE.getExecutableNameLinux
        |> Common.findCommandPath
    | false ->
        let ideName = ide |> JetBrainsIDE.getName
        let ideExecutableName = ide |> JetBrainsIDE.getExecutableNameWindows

        seq {
            Path.Combine(Environment.SpecialFolder.ProgramFilesX86 |> Environment.GetFolderPath, "JetBrains/Installations"), ideName + "*"
            Path.Combine(Environment.SpecialFolder.ProgramFilesX86 |> Environment.GetFolderPath, "JetBrains/Installations"), $"JetBrains {ideName} *"
            Path.Combine(Environment.SpecialFolder.ProgramFiles |> Environment.GetFolderPath, "JetBrains/Installations"), ideName + "*"
            Path.Combine(Environment.SpecialFolder.ProgramFiles |> Environment.GetFolderPath, "JetBrains/Installations"), $"JetBrains {ideName} *"
            Path.Combine(Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath, "JetBrains/Installations"), ideName + "*"
            Path.Combine(Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath, "JetBrains/Installations"), $"JetBrains {ideName} *"
            Path.Combine(Environment.SpecialFolder.ProgramFilesX86 |> Environment.GetFolderPath, "JetBrains"), ideName + "*"
            Path.Combine(Environment.SpecialFolder.ProgramFilesX86 |> Environment.GetFolderPath, "JetBrains"), $"JetBrains {ideName} *"
            Path.Combine(Environment.SpecialFolder.ProgramFiles |> Environment.GetFolderPath, "JetBrains"), ideName + "*"
            Path.Combine(Environment.SpecialFolder.ProgramFiles |> Environment.GetFolderPath, "JetBrains"), $"JetBrains {ideName} *"
            Path.Combine(Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath, "JetBrains"), ideName + "*"
            Path.Combine(Environment.SpecialFolder.LocalApplicationData |> Environment.GetFolderPath, "JetBrains"), $"JetBrains {ideName} *"
        }
        |> Seq.tryPick (fun (dir, pattern) -> option {
            do! dir
                |> Directory.Exists
                |> function false -> None | true -> Some ()

            let! directories =
                match Directory.GetDirectories(dir, pattern, EnumerationOptions(MatchCasing = MatchCasing.CaseInsensitive)) with
                | [| |] -> None
                | arr -> Some arr

            let! struct (exePath, _) =
                directories |> Array.fold
                    (fun state dir ->
                        let exeFile = Path.Combine(dir, "bin", ideExecutableName)

                        match exeFile |> File.Exists with
                        | false -> state
                        | true ->
                            let version =
                                dir
                                |> Path.GetFileName
                                |> _.Substring(ideName.Length)
                                |> fun v ->
                                    match Int32.TryParse v with
                                    | true, v -> v
                                    | false, _ -> 0

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
                    "Programs", ideName, "bin", ideExecutableName
                )

            match path |> File.Exists with
            | false -> None
            | true -> Some path
        )

let builder ide : WorkspaceSourceBuilder =
    { Id = $"workspace-jetbrains-{ide |> JetBrainsIDE.getName |> _.ToLowerInvariant()}"
      Name = "JetBrains " + (ide |> JetBrainsIDE.getName)
      ShortName = ide |> JetBrainsIDE.getShortName
      LoadIcon = fun pluginPath -> Icons.loadIcon pluginPath (JetBrainsIDE.getIconName ide)
      FindExecutablePath = fun () -> findIdeExecutable ide
      FindWorkspacesDb = fun () -> findWorkspaceDbPath ide
      LoadWorkspaces = loadWorkspaces
      GetChangesObservable = detectWorkspaceChanges }
