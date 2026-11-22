namespace Starter.Spotify.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

type PlaybackController() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
