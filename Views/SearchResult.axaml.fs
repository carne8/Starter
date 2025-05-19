namespace Starter.Controls

open Avalonia.Controls
open Avalonia.Markup.Xaml

type SearchResultControl() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
