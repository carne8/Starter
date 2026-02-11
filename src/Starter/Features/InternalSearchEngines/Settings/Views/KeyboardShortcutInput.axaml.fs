namespace Starter.Features.InternalSearchEngines.Settings.Views

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Markup.Xaml
open Starter.Features.InternalSearchEngines.Settings.ViewModels

type KeyboardShortcutInput() as this =
    inherit UserControl()

    let mutable dataContext = null
    let mutable subscription = ValueNone

    static let ControlToFocusProperty = AvaloniaProperty.Register<KeyboardShortcutInput, Control | null>("ControlToFocus", null)

    do this.InitializeComponent()

    member this.ControlToFocus
        with get () = this.GetValue(ControlToFocusProperty)
        and set v = this.SetValue(ControlToFocusProperty, v) |> ignore

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this

    override this.Finalize() =
        subscription |> ValueOption.iter (fun d -> (d :> IDisposable).Dispose())

    override this.OnDataContextChanged _ =
        subscription |> ValueOption.iter (fun d -> (d :> IDisposable).Dispose())

        match this.DataContext with
        | :? KeyboardShortcutInputViewModel as dc ->
            dataContext <- dc
            subscription <-
                dc.StoppedListeningKeys.Subscribe(fun _ ->
                    match this.ControlToFocus with
                    | null -> ()
                    | c -> c.Focus() |> ignore
                )
                |> ValueSome
        | _ -> ()

    member this.TextBoxKeyDown(_sender: obj, args: KeyEventArgs) =
        match this.DataContext with
        | :? KeyboardShortcutInputViewModel as dc ->
            dc.KeyDown args.Key
            args.Handled <- true
        | _ -> ()

    member this.TextBoxGotFocus(_sender: obj, _args: RoutedEventArgs) =
        match this.DataContext with
        | :? KeyboardShortcutInputViewModel as dc -> dc.StartListeningKeys()
        | _ -> ()

    member this.TextBoxLostFocus(_sender: obj, _args: RoutedEventArgs) =
        match this.DataContext with
        | :? KeyboardShortcutInputViewModel as dc -> dc.Cancel()
        | _ -> ()
