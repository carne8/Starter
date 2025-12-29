namespace Starter.Features.PlatformInterop

open System
open System.IO
open System.Threading.Tasks

open Tmds.DBus
open Avalonia.Controls

open Starter.Features
open Starter.Features.Logging

[<DBusInterface("com.carne8.Starter")>]
type IStarterLauncher =
    inherit IDBusObject
    abstract member LaunchAsync: unit -> Task

type StarterLauncher(onLaunched) =
    static let path = ObjectPath "/com/carne8/Starter"

    interface IStarterLauncher with
        override this.ObjectPath = path
        override this.LaunchAsync() =
            onLaunched()
            Task.FromResult()

type Linux() =
    inherit PlatformInterop()

    static let startupFolder =
        match Environment.GetEnvironmentVariable "XDG_CONFIG_HOME" with
        | null
        | "" ->
            // ~/.config/autostart/
            Path.Combine(
                Environment.GetFolderPath Environment.SpecialFolder.UserProfile,
                ".config",
                "autostart"
            )
        | envVar ->
            // $XDG_CONFIG_HOME/autostart/
            Path.Combine(envVar, "autostart")

    static let startupFile = Path.Combine(startupFolder, Constants.Platform.Linux.StartupFile)

    static let startupFileContent =
        $"""[Desktop Entry]
Type=Application
Name=Starter
Exec={Constants.ProcessExecutableFile}
Comment=Launch Starter at startup
"""

    let dbusConnection = new Connection(Address.Session)

    override this.ToggleLaunchAtStartup(enable) =
        match enable, this.IsLaunchAtStartupEnabled() with
        | true, false ->
            try
                if not <| Directory.Exists startupFolder then
                    startupFolder
                    |> Directory.CreateDirectory
                    |> ignore

                use writer = File.CreateText startupFile
                writer.Write startupFileContent
                logger.Information $"Created autostart file {startupFile}"
            with e ->
                logger.Error(e, $"Failed to create autostart file {startupFile}")
        | false, true ->
            try
                File.Delete startupFile
                logger.Information $"Deleted autostart file {startupFile}"
            with e ->
                logger.Error(e, $"Failed to delete autostart file {startupFile}")
        | _ -> ()

    override _.IsLaunchAtStartupEnabled() = startupFile |> File.Exists

    override this.RegisterHotkey shortcut window = failwith "todo"

    override _.SetupHotkeyCallback(window: Window) =
        Task.Run<unit>(fun () -> task {
            try
                let! _ = dbusConnection.ConnectAsync()
                do! dbusConnection.RegisterServiceAsync("com.carne8.Starter")

                let object = StarterLauncher(fun () ->
                    Avalonia.Threading.Dispatcher.UIThread.Post(fun () -> window.Show())
                )
                do! dbusConnection.RegisterObjectAsync(object)
            with e -> logger.Error(e, "Failed to setup dbus service");
        })
        |> ignore
