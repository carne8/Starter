namespace Starter.Views

open Starter
open Starter.Features

open System.Collections.Generic
open Vanara.PInvoke

open Avalonia
open Avalonia.Input
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.VisualTree

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

            this.ViewModel.Config
            |> Observable.subscribe (fun config ->
                this.TransparencyLevelHint <-
                    match config.Background with
                    | Config.Background.Acrylic -> [| WindowTransparencyLevel.AcrylicBlur |].AsReadOnly()
                    | Config.Background.Mica -> [| WindowTransparencyLevel.Mica |].AsReadOnly()
                    | Config.Background.None -> [| WindowTransparencyLevel.None |].AsReadOnly()
            )
            |> ignore

            this.ResultList.ItemsSource <- this.ViewModel.SearchResults
        )

        this.Activated.Add (fun _ ->
            this.TextBox.Focus() |> ignore
            this.TextBox.SelectAll()
            this.ResultList.Selection.Select 0 // Reset selection
        )
        #if !DEBUG
        this.Deactivated.Add (fun _ -> this.Hide())
        #endif

    member private this.SetupKeyboardShortcuts() =
        // Add hide key binding
        KeyBinding(
            Command = this.ViewModel.HideCommand,
            Gesture = KeyGesture.Parse "Escape"
        )
        |> this.KeyBindings.Add

        this.ViewModel.HideCommand
        |> Observable.subscribe (fun _ ->
            Threading.Dispatcher.UIThread.Post(fun _ ->
                this.Hide()
            )
        )
        |> ignore

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
                if newSelectedIdx = 0 then // Scroll to top to preserve the top padding
                    r.GetVisualDescendants()
                    |> Seq.tryFind (fun visual -> visual.Name = "PART_ScrollViewer")
                    |> Option.iter (fun visual -> (visual :?> ScrollViewer).ScrollToHome())
                elif newSelectedIdx = r.ItemCount - 1 then // Scroll to bottom to preserve the bottom padding of the listbox
                    r.GetVisualDescendants()
                    |> Seq.tryFind (fun visual -> visual.Name = "PART_ScrollViewer")
                    |> Option.iter (fun visual -> (visual :?> ScrollViewer).ScrollToEnd())

                r.Selection.SelectedIndex <- newSelectedIdx
                e.Handled <- true
        )
