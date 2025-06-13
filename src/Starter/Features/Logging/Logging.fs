module Starter.Features.Logging

open Starter.Features.Logger
open Serilog
open R3

let private observableSink = new ObservableSink()
let logs = observableSink.AsObservable()

let logger =
    LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Async(fun c ->
            c.File(
                Constants.LogFilePath,
                rollingInterval = RollingInterval.Day,
                retainedFileCountLimit = 10
            )
            |> ignore
        )
        .WriteTo.Async(fun c -> c.Sink(observableSink) |> ignore)
        #if DEBUG_LOGS
        .WriteTo.Console()
        #endif
        .CreateLogger()
