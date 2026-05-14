namespace Starter.Calculator.Controls

open Avalonia.Layout
open Avalonia.Media
open Avalonia.Styling
open CSharpMath.Avalonia

type ResultControl() as this =
    inherit MathView()

    do this.FontSize <- 18f
       this.HorizontalAlignment <- HorizontalAlignment.Left

    override this.OnPropertyChanged(change) =
        base.OnPropertyChanged(change)

        match change.NewValue with
        | :? ThemeVariant as theme ->
            if theme = ThemeVariant.Light then
                this.TextColor <- Color.FromRgb(0uy, 0uy, 0uy)
            else
                this.TextColor <- Color.FromRgb(255uy, 255uy, 255uy)
        | _ -> ()
