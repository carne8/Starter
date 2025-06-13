namespace Starter.Features.Logger.View

open System
open System.IO
open System.Text

open Avalonia.Controls
open Avalonia.Markup.Xaml

open Starter.Features.Logging
open Serilog.Formatting
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

    do
        logs.Subscribe(fun logEvent ->
            formatter.Format(logEvent, textWriter)
            stringBuilder.ToString() |> text.OnNext
            this.RaisePropertyChanged(nameof this.Text)
        )
        |> ignore

    member this.Text = text.Value

type LogsView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
