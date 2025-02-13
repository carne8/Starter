module Starter.Program

open System
open Avalonia

[<CompiledName "BuildAvaloniaApp">]
let buildAvaloniaApp () =
    AppBuilder
        .Configure<App>()
        .With(Win32PlatformOptions(WinUICompositionBackdropCornerRadius = 12f))
        .UsePlatformDetect()
        .LogToTrace()

[<EntryPoint; STAThread>]
let main args =
    buildAvaloniaApp().StartWithClassicDesktopLifetime(args)
