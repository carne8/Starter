namespace Starter.Views

open Starter
open System
open Avalonia
open Avalonia.Input
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.VisualTree
open Vanara.PInvoke

[<AutoOpen>]
module Helpers =
    type IObservable<'a> with
        member this.SubscribeOnUIThread(f) =
            this.Subscribe(fun value ->
                Threading.Dispatcher.UIThread.Post(fun _ ->
                    f value
                )
            ) |> ignore

type MainWindow() as this =
    inherit Window()

    let wndProcCallback =
        Win32Properties.CustomWndProcHookCallback(
            fun (_hWnd: nativeint) (msg: uint32) (_wParam: nativeint) (_lParam: nativeint) _ ->
                if msg = uint User32.WindowMessage.WM_HOTKEY then
                    this.Show()
                0
        )

    do this.InitializeComponent()

    member this.ViewModel =
        match this.DataContext with
        | null -> failwith "No DataContext attached"
        | dc -> dc :?> ViewModels.MainWindowViewModel
    member this.TextBox = this.Get<TextBox> "TextBox"
    member this.ResultList = this.Get<ListBox> "ResultList"

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        #if DEBUG
        this.AttachDevTools()
        #endif

        Win32Properties.AddWndProcHookCallback(this, wndProcCallback)

        this.Loaded.Add(fun _ ->
            this.SetupKeyboardShortcuts()
            // match Application.Current with
            // | null -> ()
            // | app ->
            //     let x = this.TryFindResource("UIWindowBackgroundBrushActive")
            //     x |> printfn "%A"
            //     app.Resources["UIWindowBorderColorActive"] <-
            //         match this.PlatformSettings with
            //         | null -> Media.Colors.Transparent
            //         | platformSettings -> platformSettings.GetColorValues().AccentColor1
        )

        this.Activated.Add (fun _ ->
            this.TextBox.Focus() |> ignore
            this.TextBox.SelectAll()
            this.ResultList.Selection.Select 0 // Reset selection
        )
        // this.Deactivated.Add (fun _ -> this.Hide())

    member private this.SetupKeyboardShortcuts() =
        // Add hide key binding
        KeyBinding(
            Command = this.ViewModel.HideCommand,
            Gesture = KeyGesture.Parse "Escape"
        )
        |> this.KeyBindings.Add

        // Subscribe to hide command
        this.ViewModel.HideCommand.SubscribeOnUIThread(fun _ -> this.Hide())

        // Set keyboard navigation
        let r = this.ResultList
        this.TextBox.KeyDown.Add(fun e ->
            let newSelectedIdx =
                match e.Key.ToNavigationDirection() |> Option.ofNullable with
                | Some NavigationDirection.Up ->
                    (r.SelectedIndex - 1)
                    |> max 0
                    |> Some
                | Some NavigationDirection.Down ->
                    (r.SelectedIndex + 1)
                    |> min (r.ItemCount - 1)
                    |> Some
                | _ -> None

            match newSelectedIdx with
            | None -> ()
            | Some newSelectedIdx ->
                if newSelectedIdx = r.ItemCount - 1 then // Scroll to bottom to preserve the bottom padding of the listbox
                    r.GetVisualDescendants()
                    |> Seq.tryFind (fun visual -> visual.Name = "PART_ScrollViewer")
                    |> Option.iter (fun visual -> (visual :?> ScrollViewer).ScrollToEnd())

                r.Selection.SelectedIndex <- newSelectedIdx
                e.Handled <- true
        )
