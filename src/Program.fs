module Program

open System
open System.Threading
open SharpHook
open SharpHook.Native

open Avalonia
open Avalonia.Controls

[<CompiledName "BuildAvaloniaApp">]
let buildAvaloniaApp () =
    AppBuilder
        .Configure<Starter.App.Host.App>()
        .UsePlatformDetect()
        .UseSkia()
        .LogToTrace()

[<EntryPoint; STAThread>]
let main argv =
    let app = buildAvaloniaApp()
    app.StartWithClassicDesktopLifetime(argv)