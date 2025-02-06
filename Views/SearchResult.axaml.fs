namespace Starter.Controls

open System
open System.Threading.Tasks
open Avalonia
open Avalonia.Platform
open Avalonia.Threading
open Starter.ViewModels
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Media.Imaging
open FsToolkit.ErrorHandling

type SearchResultControl () as this =
    inherit UserControl()

    let mutable bitmap: Bitmap option = None
    static let fallbackBitmap = new Bitmap(AssetLoader.Open <| Uri "avares://Starter/Assets/avalonia-logo.ico")

    do
        this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
        this.Loaded.Add(fun _ ->
            let vm = this.DataContext :?> SearchResultViewModel
            let imageControl = this.Get<Image> "Icon"

            match vm.LoadIcon() with
            | null -> imageControl.Source <- fallbackBitmap
            | bmp ->
                bitmap <- Some bmp
                imageControl.Source <- bmp
        )

        this.Unloaded.Add(fun _ ->
            bitmap |> Option.iter _.Dispose()
        )
