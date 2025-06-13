namespace Starter.Features.LoggingView

open System.IO
open System.Text

open Avalonia.Controls
open Avalonia.Controls.Documents
open Avalonia.Markup.Xaml

open Avalonia.Media
open Serilog.Events
open Serilog.Formatting
open Starter.Features.Logging
open ReactiveUI
open R3

type LogsViewModel() as this =
    inherit ReactiveObject()

    let lines = InlineCollection()

    [<Literal>]
    let template = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}"
    let formatter = Display.MessageTemplateTextFormatter(template)
    [<Literal>]
    let templateWithException = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
    let formatterWithException = Display.MessageTemplateTextFormatter(templateWithException)

    let brushes =
        [ LogEventLevel.Verbose, SolidColorBrush()
          LogEventLevel.Debug, SolidColorBrush(Color(255uy, 80uy, 161uy, 79uy))
          LogEventLevel.Information, SolidColorBrush(Color(255uy, 1uy, 132uy, 188uy))
          LogEventLevel.Warning, SolidColorBrush(Color(255uy, 193uy, 131uy, 1uy))
          LogEventLevel.Error, SolidColorBrush(Color(255uy, 228uy, 86uy, 73uy))
          LogEventLevel.Fatal, SolidColorBrush(Color(255uy, 166uy, 38uy, 164uy)) ]
        |> dict

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
            let sb = StringBuilder()
            use sw = new StringWriter(sb)

            if lines.Count <> 0 then sb.AppendLine() |> ignore

            if logEvent.Exception = null then
                formatter.Format(logEvent, sw)
            else
                formatterWithException.Format(logEvent, sw)

            Run(sb.ToString(), Foreground = brushes[logEvent.Level])
            |> lines.Add

    let minimumLevelChanged () =
        lines.Clear()
        logs.Logs |> Seq.iter printLogEvent
        this.RaisePropertyChanged(nameof this.Lines)

    do
        logs.ObserveOnUIThreadDispatcher().Subscribe(fun logEvent ->
            logEvent |> printLogEvent
            this.RaisePropertyChanged(nameof this.Lines)
        )
        |> ignore

    member this.Lines = lines

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
