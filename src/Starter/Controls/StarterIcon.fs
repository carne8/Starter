namespace Starter.Controls

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Documents
open Avalonia.Styling
open Starter.SearchEngine

open FluentAvalonia.UI.Controls

type StarterIcon() as this =
    inherit UserControl()

    static let FontSizeProperty = TextElement.FontSizeProperty.AddOwner<StarterIcon>()
    static let IconSourceProperty = DirectProperty.Register<StarterIcon, StarterIconSource>("IconSource", StarterIconSource.Empty)
    static let ExtraPaddingForSymbolProperty = DirectProperty.Register<StarterIcon, float>("ExtraPaddingForSymbol", 0)

    let createIconControl () =
        PathIcon(
            Data = this.IconSource.Geometry,
            Width = this.FontSize - this.ExtraPaddingForSymbol,
            Height = this.FontSize - this.ExtraPaddingForSymbol
        )

    let createImageControl () =
        let lightMode = this.ActualThemeVariant = ThemeVariant.Light
        let image = this.IconSource.GetImage lightMode
        ImageIcon(Source = image)

    override this.OnPropertyChanged(change) =
        if change.Property = IconSourceProperty then
            let control =
                match this.IconSource.Geometry with
                | null -> createImageControl()  :> Control
                | _ -> createIconControl()

            this.Content <- control

        if change.Property = FontSizeProperty || change.Property = ExtraPaddingForSymbolProperty then
            match this.Content with
            | :? PathIcon as c ->
                c.Width <- this.FontSize - this.ExtraPaddingForSymbol
                c.Height <- this.FontSize - this.ExtraPaddingForSymbol
            | _ -> ()

        match change.NewValue with
        | :? ThemeVariant as theme ->
            match this.Content with
            | :? ImageIcon as c ->
                c.Source <- theme = ThemeVariant.Light |> this.IconSource.GetImage
            | _ -> ()
        | _ -> ()

    member this.FontSize
        with get () = this.GetValue(FontSizeProperty)
        and set v = this.SetValue(FontSizeProperty, v) |> ignore

    member this.IconSource
        with get () : StarterIconSource = this.GetValue(IconSourceProperty)
        and set v = this.SetValue(IconSourceProperty, v) |> ignore

    member this.ExtraPaddingForSymbol
        with get () = this.GetValue(ExtraPaddingForSymbolProperty)
        and set v = this.SetValue(ExtraPaddingForSymbolProperty, v) |> ignore
