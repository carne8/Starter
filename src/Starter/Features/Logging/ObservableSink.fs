namespace Starter.Features.Logger

open System
open System.Collections.Generic

open Serilog.Core
open Serilog.Events

open R3

type ObservableSink() =
    inherit Observable<LogEvent>()

    let mutable disposed = false
    let logs = List<LogEvent>()
    let observers = List<Observer<LogEvent>>()

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
