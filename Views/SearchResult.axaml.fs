namespace Starter.Controls

open System
open Avalonia.Platform
open Avalonia.Threading
open Starter.ViewModels
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.Media.Imaging
open FsToolkit.ErrorHandling

type SearchResultControl() as this =
    inherit UserControl()

    let mutable bitmap: Bitmap option = None
    static let fallbackBitmap = new Bitmap(AssetLoader.Open <| Uri "avares://Starter/Assets/avalonia-logo.ico")

    let mutable name = ""

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
        this.Loaded.Add(fun _ ->
            let vm =
                match this.DataContext with
                | null -> failwith "No DataContext attached"
                | dc -> dc :?> SearchResultViewModel
            name <- vm.Name

            let imageControl = this.Get<Image> "Icon"

            if imageControl.Source |> isNull then
                vm.LoadIcon() |> Task.map (fun bmp ->
                    Dispatcher.UIThread.Post(fun _ ->
                        match bmp with
                        | null -> imageControl.Source <- fallbackBitmap
                        | bmp ->
                            bitmap <- Some bmp
                            imageControl.Source <- bmp
                    )
                ) |> ignore
        )
