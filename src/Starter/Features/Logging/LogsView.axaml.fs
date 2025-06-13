namespace Starter.Features.LoggingView

open System
open System.IO
open System.Text

open Avalonia.Controls
open Avalonia.Markup.Xaml

open Serilog.Events
open Serilog.Formatting
open Starter.Features.Logging
open ReactiveUI
open R3

type LogsViewModel() as this =
    inherit ReactiveObject()

    let stringBuilder = StringBuilder()
    let textWriter = new StringWriter(stringBuilder)
    let text = new BehaviorSubject<_>(String.Empty)

    [<Literal>]
    let template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    let formatter = Display.MessageTemplateTextFormatter(template)

    let mutable minimumLevel = LogEventLevel.Information
    let logLevels =
        [| LogEventLevel.Verbose
           LogEventLevel.Debug
           LogEventLevel.Information
           LogEventLevel.Warning
           LogEventLevel.Error
           LogEventLevel.Fatal |]

    let printLogEvent (logEvent: LogEvent) =
        if logEvent.Level >= minimumLevel then
            formatter.Format(logEvent, textWriter)

    let minimumLevelChanged () =
        stringBuilder.Clear() |> ignore
        logs.Logs |> Seq.iter printLogEvent

        stringBuilder.ToString() |> text.OnNext
        this.RaisePropertyChanged(nameof this.Text)

    do
        logs.Subscribe(fun logEvent ->
            logEvent |> printLogEvent
            stringBuilder.ToString() |> text.OnNext
            this.RaisePropertyChanged(nameof this.Text)
        )
        |> ignore

    member this.Text = text.Value

    member this.MinimumLevel
        with get () = minimumLevel
        and set v =
            this.RaiseAndSetIfChanged(&minimumLevel, v) |> ignore
            minimumLevelChanged()

    member this.LogLevels = logLevels

type LogsView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
