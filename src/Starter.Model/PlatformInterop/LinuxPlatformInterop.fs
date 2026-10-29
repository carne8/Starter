namespace Starter.Features.PlatformInterop

open System
open System.IO
open System.Threading.Tasks

open Tmds.DBus
open Avalonia.Controls

open Starter.Features
open Starter.Features.Config
open Starter.Features.Logging
open Starter.Features.PlatformInterop.Linux

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

type LinuxPlatformInterop() =
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

    let desktopEnvironment = DesktopEnvironment.detectDesktopEnvironment()
    let hotkeyRegistrable =
        match desktopEnvironment with
        | Gnome -> true
        | _ -> false

    let dbusConnection = new Connection(Address.Session)

    interface IPlatformInterop with
        override this.SupportBackground background =
            match background with
            | Background.None -> true
            | Background.Mica
            | Background.Acrylic -> false

        override this.EnsureConfigCompatibility config =
            { config with Background = Background.None }

        // Launch at startup
        override this.ToggleLaunchAtStartup(enable) =
            match enable, (this :> IPlatformInterop).IsLaunchAtStartupEnabled() with
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

        // Hotkey
        override _.HotkeyRegistrable = hotkeyRegistrable
        override _.RegisterHotkey shortcut _window =
            task {
                let! res = Task.Run<Result<_, _>>(fun () -> KeyboardShortcut.setKeyboardShortcut desktopEnvironment shortcut)

                match res with
                | Ok () -> logger.Information "Successfully set keyboard shortcut."
                | Error err -> logger.Error $"Failed to set keyboard shortcut: {err}"

                return res.IsOk
            }

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
