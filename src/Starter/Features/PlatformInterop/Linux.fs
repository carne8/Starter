namespace Starter.Features.PlatformInterop

open System
open System.IO
open System.Threading.Tasks
open Avalonia.Controls
open Starter.Features
open Tmds.DBus

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
        Path.Combine(
            Environment.SpecialFolder.UserProfile |> Environment.GetFolderPath,
            ".config",
            "autostart"
        )
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
            use writer = File.CreateText startupFile
            writer.Write startupFileContent
        | false, true -> File.Delete startupFile
        | _ -> ()

    override _.IsLaunchAtStartupEnabled() = startupFile |> File.Exists

    member _.SetupHotkeyCallback(window: Window) =
        Task.Run<unit>(fun () -> task {
            try
                let! _ = dbusConnection.ConnectAsync()
                do! dbusConnection.RegisterServiceAsync("com.carne8.Starter")

                let object = StarterLauncher(fun () ->
                    Avalonia.Threading.Dispatcher.UIThread.Post(fun () -> window.Show())
                )
                do! dbusConnection.RegisterObjectAsync(object)
            with e -> printfn "DBus: %s" e.Message
        })
        |> ignore
