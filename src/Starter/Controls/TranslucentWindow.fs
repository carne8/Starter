namespace Starter.Controls

open Avalonia
open Avalonia.Controls
open Avalonia.Media
open Avalonia.Metadata
open Starter.Features
open System.Collections.Generic

type TranslucentWindow() =
    inherit Window()

    static let BackgroundKindProperty = AvaloniaProperty.Register<TranslucentWindow, Config.Background | null>("BackgroundKind")
    static let AcrylicBackgroundColorProperty = AvaloniaProperty.Register<TranslucentWindow, IBrush | null>("AcrylicBackgroundColor")
    static let BackgroundColorProperty = AvaloniaProperty.Register<TranslucentWindow, IBrush | null>("BackgroundKind")
    static let ChildProperty = AvaloniaProperty.Register<TranslucentWindow, Control | null>("Child")

    let border = Border()

    do
        base.Content <- border
        base.Background <- null

    member this.BackgroundKind
        with get () = this.GetValue(BackgroundKindProperty)
        and set v = this.SetValue(BackgroundKindProperty, v) |> ignore

    member this.AcrylicBackgroundColor
        with get () = this.GetValue(AcrylicBackgroundColorProperty)
        and set v = this.SetValue(AcrylicBackgroundColorProperty, v) |> ignore

    member this.BackgroundColor
        with get () = this.GetValue(BackgroundColorProperty)
        and set v = this.SetValue(BackgroundColorProperty, v) |> ignore

    [<Content>]
    member this.Child
        with get () = this.GetValue(ChildProperty)
        and set v = this.SetValue(ChildProperty, v) |> ignore

    override this.OnPropertyChanged(change: AvaloniaPropertyChangedEventArgs) =
        base.OnPropertyChanged(change)
        if change.Property = ChildProperty then border.Child <- this.Child

        if change.Property = Window.CornerRadiusProperty then border.CornerRadius <- this.CornerRadius

        if change.Property = BackgroundKindProperty then
            match this.BackgroundKind with
            | null -> ()
            | Config.Background.None ->
                this.TransparencyLevelHint <- [| WindowTransparencyLevel.None |].AsReadOnly()
                border.Background <- this.BackgroundColor
            | Config.Background.Acrylic ->
                this.TransparencyLevelHint <- [| WindowTransparencyLevel.AcrylicBlur; WindowTransparencyLevel.Blur |].AsReadOnly()
                border.Background <- this.AcrylicBackgroundColor
            | Config.Background.Mica ->
                this.TransparencyLevelHint <- [| WindowTransparencyLevel.Mica; WindowTransparencyLevel.Transparent |].AsReadOnly()
                border.Background <- null

        if change.Property = BackgroundColorProperty then
            match this.BackgroundKind with
            | null -> ()
            | Config.Background.None -> border.Background <- this.BackgroundColor
            | _ -> ()

        if change.Property = AcrylicBackgroundColorProperty then
            match this.BackgroundKind with
            | null -> ()
            | Config.Background.Acrylic -> border.Background <- this.AcrylicBackgroundColor
            | _ -> ()
