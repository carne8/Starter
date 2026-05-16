namespace Starter.Tests

open Avalonia
open Avalonia.Headless

type TestAppBuilder() =
    static member BuildAvaloniaApp() =
        AppBuilder
            .Configure(fun () -> Starter.App(IsTestMode = true))
            .UseHeadless(AvaloniaHeadlessPlatformOptions())

[<assembly: AvaloniaTestApplication(typeof<TestAppBuilder>)>]
[<assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)>]
do ()
