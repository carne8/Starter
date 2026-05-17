module Starter.Tests.Program

open Avalonia
open Avalonia.Headless
open NUnit.Framework

type TestAppBuilder() =
    static member BuildAvaloniaApp() =
        AppBuilder
            .Configure(fun () -> Starter.App(IsTestMode = true))
            .UseHeadless(AvaloniaHeadlessPlatformOptions())

[<assembly: AvaloniaTestApplication(typeof<TestAppBuilder>)>] ()

[<SetUp>]
let setup () = ()

[<EntryPoint>]
let main _ = 0
