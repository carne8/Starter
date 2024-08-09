module Program

open System
open Avalonia

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