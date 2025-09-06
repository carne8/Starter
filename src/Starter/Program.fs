module Starter.Program

open System
open System.Threading
open Avalonia

[<CompiledName "BuildAvaloniaApp">]
let buildAvaloniaApp () =
    AppBuilder
        .Configure<App>()
        .UsePlatformDetect()
        .UseR3()
        .With(Win32PlatformOptions(WinUICompositionBackdropCornerRadius = 21f))
        .LogToTrace()

let [<Literal>] mutexName = "Starter-426a2d89-cfe9-4554-b9a5-8c7d85417f25"

[<EntryPoint; STAThread>]
let main args =
    let mutable createdNew = false
    use mutex = new Mutex(true, mutexName, &createdNew)

    if not createdNew then
        printfn "Starter is already running."
        0
    else
        mutex.Dispose()
        buildAvaloniaApp().StartWithClassicDesktopLifetime(args)
