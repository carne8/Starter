module Starter.Features.Logging

open System
open System.Collections.Generic
open Serilog
open Serilog.Core
open Serilog.Events
open R3

type ObservableSink() =
    inherit Observable<LogEvent>()

    let mutable disposed = false
    let logs = List<LogEvent>()
    let observers = List<Observer<LogEvent>>()

    member this.Logs = logs

    override this.SubscribeCore(observer) =
        if disposed then
            "The Observable sink is disposed"
            |> ObjectDisposedException
            |> raise

        observers.Add observer
        logs |> Seq.iter observer.OnNext

        { new IDisposable with
            member this.Dispose() = observers.Remove observer |> ignore }

    interface ILogEventSink with
        member this.Emit(logEvent) =
            observers |> Seq.iter _.OnNext(logEvent)
            logEvent |> logs.Add

    interface IDisposable with
        member this.Dispose() =
            if not disposed then
                observers |> Seq.iter _.OnCompleted()
                disposed <- true


let private observableSink = new ObservableSink()
let logs = observableSink

[<Literal>]
let logTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Context}] {Message:lj}{NewLine}{Exception}"

let logger =
    LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Async(fun c ->
            c.File(
                Constants.LogFilePath,
                rollingInterval = RollingInterval.Day,
                retainedFileCountLimit = 10,
                outputTemplate = logTemplate
            )
            |> ignore
        )
        .WriteTo.Async(fun c -> c.Sink(observableSink) |> ignore)
        #if DEBUG || DEBUG_LOGS
        .WriteTo.Console(outputTemplate = logTemplate, theme = Serilog.Sinks.SystemConsole.Themes.ConsoleTheme.None)
        #endif
        .CreateLogger()
        .ForContext("Context", "Starter")
