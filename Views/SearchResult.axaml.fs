namespace Starter.Controls

open System
open Avalonia.Platform
open Starter.ViewModels
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Media.Imaging

type SearchResultControl() as this =
    inherit UserControl()

    let mutable bitmap: Bitmap option = None
    static let fallbackBitmap = new Bitmap(AssetLoader.Open <| Uri "avares://Starter/Assets/avalonia-logo.ico")

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
        this.DataContextChanged.Add(fun _ ->
            match this.DataContext with
            | null -> ()
            | dc ->
                let vm = dc :?> SearchResultViewModel

                // Set text
                let textControl = this.Get<TextBlock> "TextBlock"
                textControl.Inlines <- vm.Inlines

                // Set icon
                let imageControl = this.Get<Image> "Icon"
                if imageControl.Source |> isNull then
                    match vm.LoadIcon() with
                    | null -> imageControl.Source <- fallbackBitmap
                    | bmp ->
                        bitmap <- Some bmp
                        imageControl.Source <- bmp
        )
