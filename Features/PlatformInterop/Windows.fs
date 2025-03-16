namespace Starter.Features.PlatformInterop

open System
open System.IO
open System.Runtime.InteropServices.ComTypes
open Vanara.PInvoke

type Windows() =
    inherit PlatformInterop()

    static let StartupLink = "Starter.lnk"

    override _.ToggleLaunchAtStartup(enable) =
        let startupFolder =
            Environment.SpecialFolder.Startup
            |> Environment.GetFolderPath

        let startupFile = Path.Combine(startupFolder, StartupLink)

        match enable with
        | false ->
            if File.Exists startupFile then File.Delete startupFile
        | true ->
            let processFile = Environment.ProcessPath

            if not <| File.Exists startupFile then
                let shortcut = Shell32.CShellLinkW() |> unbox<Shell32.IShellLinkW>
                shortcut.SetPath processFile
                shortcut.SetDescription "Starter"
                shortcut.SetWorkingDirectory (Path.GetDirectoryName processFile)
                shortcut.SetIconLocation(processFile, 0)

                (shortcut :?> IPersistFile).Save(startupFile, true)
