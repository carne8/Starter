module Starter.Features.Logging

open System
open System.Collections.Generic
open System.Threading.Tasks
open Serilog
open Serilog.Core
open Serilog.Events
open R3

type ObservableSink() =
    inherit Observable<LogEvent>()

    let mutable disposed = false
    let logs = List<LogEvent>()
    let observers = List<Observer<LogEvent>>()

    member _.Logs = logs

    override _.SubscribeCore observer =
        if disposed then
            "The Observable sink is disposed"
            |> ObjectDisposedException
            |> raise

        observers.Add observer
        logs |> Seq.iter observer.OnNext

        { new IDisposable with
            member _.Dispose() = observers.Remove observer |> ignore }

    interface ILogEventSink with
        member _.Emit logEvent =
            observers |> Seq.iter _.OnNext(logEvent)
            logEvent |> logs.Add

    interface IDisposable with
        member _.Dispose() =
            if not disposed then
                observers |> Seq.iter _.OnCompleted()
                disposed <- true


let private observableSink = new ObservableSink()
let logs = observableSink

[<Literal>]
let logTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] [{Context}] {Message:lj}{NewLine}{Exception}"

let logger =
    LoggerConfiguration()
        .MinimumLevel.Verbose()
        .WriteTo.Async(fun c ->
            c.File(
                Constants.LogFilePath,
                rollingInterval = RollingInterval.Day,
                retainedFileCountLimit = 10,
                outputTemplate = logTemplate
            )
            |> ignore
        )
        .WriteTo.Async(fun c -> c.Sink observableSink |> ignore)
        #if DEBUG || DEBUG_LOGS
        .WriteTo.Console(
            outputTemplate = logTemplate,
            theme = Sinks.SystemConsole.Themes.ConsoleTheme.None,
            restrictedToMinimumLevel = LogEventLevel.Verbose
        )
        #endif
        .CreateLogger()
        .ForContext("Context", "Starter")

let setupLogger () =
    Log.Logger <- logger

    // Background thread exceptions
    AppDomain.CurrentDomain.UnhandledException.Add(fun e ->
        Log.Fatal(unbox<Exception> e.ExceptionObject, "Unhandled domain exception")
    )

    TaskScheduler.UnobservedTaskException.Add(fun e ->
        Log.Error(e.Exception, "Unobserved task exception")
        e.SetObserved()
    )
