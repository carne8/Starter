namespace Starter.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

type Settings() as this =
    inherit Window()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
        #if DEBUG
        this.AttachDevTools()
        #endif
