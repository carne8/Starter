module Starter.Program

open System
open Avalonia

[<CompiledName "BuildAvaloniaApp">]
let buildAvaloniaApp () =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .UseSkia()
        .With(Win32PlatformOptions(WinUICompositionBackdropCornerRadius = 14f))
        .LogToTrace()

[<EntryPoint; STAThread>]
let main args =
    buildAvaloniaApp().StartWithClassicDesktopLifetime(args)
