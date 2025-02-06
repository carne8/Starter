module Starter.Program

open System
open Avalonia

[<CompiledName "BuildAvaloniaApp">]
let buildAvaloniaApp () =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .LogToTrace()

[<EntryPoint; STAThread>]
let main args =
    buildAvaloniaApp().StartWithClassicDesktopLifetime(args)
