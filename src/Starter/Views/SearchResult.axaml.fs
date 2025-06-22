namespace Starter.Controls

open Starter.ViewModels
open Starter.SearchEngine

open Avalonia.Controls
open Avalonia.Markup.Xaml

type SearchResultControl() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
