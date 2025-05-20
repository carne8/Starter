namespace Starter.Views

open Starter
open Starter.Controls
open Starter.Features

open System.Collections.Generic
open System.Windows.Input
open Vanara.PInvoke

open Avalonia
open Avalonia.Input
open Avalonia.Interactivity
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
    member this.SearchEnginePill = this.Get<SearchEnginePill> "SearchEnginePill"

    member this.ResultList = this.Get<ListBox> "ResultList"
    member this.ResultListScrollViewer =
        lazy (this.ResultList.GetVisualDescendants()
              |> Seq.find (fun visual -> visual.Name = "PART_ScrollViewer")
              :?> ScrollViewer)

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        #if DEBUG
        this.AttachDevTools()
        #endif

        Win32Properties.AddWndProcHookCallback(this, wndProcCallback)

        this.Loaded.Add(fun _ ->
            this.SetupKeyboardShortcuts()

            // Bind background kind
            this.ViewModel.Config
            |> Observable.subscribe (fun config ->
                this.TransparencyLevelHint <-
                    match config.Background with
                    | Config.Background.Acrylic -> [| WindowTransparencyLevel.AcrylicBlur |].AsReadOnly()
                    | Config.Background.Mica -> [| WindowTransparencyLevel.Mica |].AsReadOnly()
                    | Config.Background.None -> [| WindowTransparencyLevel.None |].AsReadOnly()
            )
            |> ignore

            // Bind search results
            this.ResultList.ItemsSource <- this.ViewModel.SearchResults

            // Bind single-search-engine pill
            this.ViewModel.SingleSearchEngineMode
            |> Observable.subscribe (fun singleSeMode ->
                match singleSeMode with
                | None ->
                    this.SearchEnginePill.IsVisible <- false
                    this.SearchEnginePill.DataContext <- null
                | Some se ->
                    this.SearchEnginePill.DataContext <- se
                    this.SearchEnginePill.IsVisible <- true
            )
            |> ignore

            // Subscribe to commands
            this.ViewModel.EmptyTextBoxCommand
            |> Observable.subscribe (fun _ ->
                Threading.Dispatcher.UIThread.Post(fun _ -> this.TextBox.Clear())
            )
            |> ignore
        )

        this.Activated.Add (fun _ ->
            this.TextBox.Focus() |> ignore
            this.TextBox.SelectAll()
            this.ResultList.Selection.Select 0 // Reset selection
            this.ResultListScrollViewer.Value.ScrollToHome() // Scroll to top to preserve the top padding
        )
        #if !DEBUG
        this.Deactivated.Add (fun _ -> this.Hide())
        #endif

    member private this.SetupKeyboardShortcuts() =
        // Subscribe to hide command
        this.ViewModel.HideCommand
        |> Observable.subscribe (fun _ ->
            Threading.Dispatcher.UIThread.Post(fun _ ->
                this.Hide()
            )
        )
        |> ignore

        // Add escape key binding
        let onEscape = // Run when "Escape" is pressed
            ReactiveUI.ReactiveCommand.Create(fun () ->
                if this.ViewModel.SingleSearchEngineMode.Value.IsSome then
                    this.ViewModel.ResetSingleSearchEngineMode()
                else
                    (this.ViewModel.HideCommand :> ICommand).Execute()
            )

        KeyBinding(
            Command = onEscape,
            Gesture = KeyGesture.Parse "Escape"
        )
        |> this.KeyBindings.Add

        // Set keyboard navigation
        // -> The goal is to be able to navigate in the listbox without losing the focus on the textbox
        let r = this.ResultList
        let d = System.EventHandler<KeyEventArgs>(fun _ e ->
            match e.PhysicalKey with
            | PhysicalKey.Backspace ->
                if this.ViewModel.SingleSearchEngineMode.Value.IsSome && this.TextBox.Text = "" then
                    this.ViewModel.ResetSingleSearchEngineMode()
                    e.Handled <- true

            | PhysicalKey.Tab -> // Prevent changing focus
                e.Handled <- true

            | _ ->
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
                    if newSelectedIdx = 0 then
                        // Scroll to top to preserve the top padding
                        this.ResultListScrollViewer.Value.ScrollToHome()
                    elif newSelectedIdx = r.ItemCount - 1 then
                        // Scroll to bottom to preserve the bottom padding of the listbox
                        this.ResultListScrollViewer.Value.ScrollToEnd()

                    r.Selection.SelectedIndex <- newSelectedIdx
                    e.Handled <- true
        )
        this.TextBox.AddHandler(InputElement.KeyDownEvent, d, RoutingStrategies.Tunnel)
