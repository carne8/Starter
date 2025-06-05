namespace Starter.Features.PlatformInterop

open System
open System.IO
open Vanara.Windows.Shell

module Constants = Starter.Features.Constants.Platform.Windows

type Windows() =
    inherit PlatformInterop()

    static let startupFolder = Environment.SpecialFolder.Startup |> Environment.GetFolderPath
    static let startupFile = Path.Combine(startupFolder, Constants.StartupFile)
    static let processFile =
        match Environment.ProcessPath with
        | null -> failwith "No process path available"
        | path -> path

    override _.ToggleLaunchAtStartup(enable) =
        match enable with
        | false ->
            if File.Exists startupFile then File.Delete startupFile
        | true ->
            if not <| File.Exists startupFile then
                use shortcut = new ShellLink(
                    Constants.StartupFile,
                    null,
                    startupFolder,
                    TargetPath = processFile,
                    Description = "Starter",
                    IconLocation = IconLocation(processFile, 0)
                )

                shortcut.SaveAs startupFile

    override _.IsLaunchAtStartupEnabled() = File.Exists startupFile
