namespace Starter.Features.PlatformInterop

open System.Threading.Tasks
open Avalonia.Controls
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

    let dbusConnection = new Connection(Address.Session)

    override _.ToggleLaunchAtStartup(_enable) = failwith "Not implemented"
    override _.IsLaunchAtStartupEnabled() = failwith "Not implemented"

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
