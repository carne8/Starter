namespace Starter.Views

open System
open Starter
open Starter.Controls
open Starter.Features
open Starter.Features.Logging
open Starter.Features.PlatformInterop

open System.Collections.Generic
open System.Windows.Input

open Avalonia
open Avalonia.Input
open Avalonia.Interactivity
open Avalonia.Controls
open Avalonia.Markup.Xaml
open Avalonia.VisualTree
open R3

type MainWindow() as this =
    inherit Window()

    static let normalResourceDictionary = ResourceDictionary()
    static let zoomedResourceDictionary = ResourceDictionary()
    static do
        normalResourceDictionary.Add("CornerRadius", CornerRadius 17)
        normalResourceDictionary.Add("SearchResultCornerRadius", CornerRadius 11)
        normalResourceDictionary.Add("ResultsPadding", Thickness(7))
        normalResourceDictionary.Add("SeparatorPadding", Thickness(13, 0))
        normalResourceDictionary.Add("SearchEnginePillMargin", Thickness(-3, 0, 10, 0))
        normalResourceDictionary.Add("TextBoxMargin", Thickness(0, 14, 13, 14))
        normalResourceDictionary.Add("TextBoxFontSize", 18.)
        normalResourceDictionary.Add("TextBoxLineHeight", 24.)
        normalResourceDictionary.Add("GridMargin", Thickness(15, 0, 0, 0))
        normalResourceDictionary.Add("SearchResultIconSize", 30.)
        normalResourceDictionary.Add("SearchResultPadding", Thickness(10, 9))
        normalResourceDictionary.Add("SearchResultFontSize", 13.)

        zoomedResourceDictionary.Add("CornerRadius", CornerRadius 17)
        zoomedResourceDictionary.Add("SearchResultCornerRadius", CornerRadius 10)
        zoomedResourceDictionary.Add("ResultsPadding", Thickness(8))
        zoomedResourceDictionary.Add("SeparatorPadding", Thickness(15, 0))
        zoomedResourceDictionary.Add("SearchEnginePillMargin", Thickness(-3, 0, 10, 0))
        zoomedResourceDictionary.Add("TextBoxMargin", Thickness(0, 14, 15, 15))
        zoomedResourceDictionary.Add("TextBoxFontSize", 20.)
        zoomedResourceDictionary.Add("TextBoxLineHeight", 27.)
        zoomedResourceDictionary.Add("GridMargin", Thickness(18, 0, 0, 0))
        zoomedResourceDictionary.Add("SearchResultIconSize", 35.)
        zoomedResourceDictionary.Add("SearchResultPadding", Thickness(10, 10))
        zoomedResourceDictionary.Add("SearchResultFontSize", 15.)

    do this.InitializeComponent()

    member this.ViewModel =
        match this.DataContext with
        | null -> failwith "No DataContext attached"
        | dc -> dc :?> ViewModels.MainWindowViewModel
    member this.TextBox = this.Get<TextBox> "TextBox"
    member this.SearchEnginePill = this.Get<SearchEnginePill> "SearchEnginePill"

    member this.ResultList = this.Get<ListBox> "ResultList"
    member this.ResultListScrollViewer() =
        this.ResultList.GetVisualDescendants()
        |> Seq.find (fun visual -> visual.Name = "PART_ScrollViewer")
        :?> ScrollViewer

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        #if DEBUG
        this.AttachDevTools()
        #endif

        match PlatformInteropFactory.GetPlatformInterop() with
        | :? Windows as platform -> platform.SetupHotkeyCallback this
        | :? Linux as platform -> platform.SetupHotkeyCallback this
        | _ -> ()

        this.Loaded.Add(fun _ ->
            this.SetupKeyboardShortcuts()

            // Bind config changes
            this.ViewModel.Config.Subscribe(fun config ->
                this.TransparencyLevelHint <-
                    match config.Background with
                    | Config.Background.Acrylic -> [| WindowTransparencyLevel.AcrylicBlur |].AsReadOnly()
                    | Config.Background.Mica -> [| WindowTransparencyLevel.Mica |].AsReadOnly()
                    | Config.Background.None -> [| WindowTransparencyLevel.None |].AsReadOnly()

                this.Resources <-
                    match config.ZoomedMode with
                    | false -> normalResourceDictionary
                    | true -> zoomedResourceDictionary
            )
            |> ignore

            // Bind single-search-engine pill
            this.ViewModel.CurrentActivator.Subscribe(fun singleSeMode ->
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
            this.ViewModel.ClearTextBoxCommand
                .ObserveOnUIThreadDispatcher()
                .Subscribe(fun prefixLength ->
                    this.TextBox.Text <-
                        match this.TextBox.Text with
                        | null -> ""
                        | s -> s.Substring(prefixLength)
                    this.TextBox.CaretIndex <- this.TextBox.CaretIndex - prefixLength
                )
            |> ignore

            // Subscribe to pointer pressed events
            this.ResultList.AddHandler(
                InputElement.PointerReleasedEvent,
                EventHandler<PointerReleasedEventArgs>(fun s e ->
                    e.GetPosition(this)
                    |> this.GetVisualsAt
                    |> Seq.tryFind (fun v -> v.DataContext :? ViewModels.SearchResultViewModel)
                    |> Option.bind (_.DataContext >> Option.ofObj)
                    |> Option.iter (fun dataContext ->
                        dataContext
                        :?>  ViewModels.SearchResultViewModel
                        |> this.ViewModel.ValidateResult
                    )
                ),
                RoutingStrategies.Tunnel
            )
        )

        this.Activated.Add (fun _ ->
            // Center window
            match this.Screens.Primary with
            | null ->
                logger.Error "No screen available. Can't center window"
                failwith "No screen available. Can't center window"
            | screen ->
                this.Position <-
                    PixelPoint(
                        round ((float screen.WorkingArea.Width - (this.Width * screen.Scaling)) / 2.) |> int,
                        float screen.WorkingArea.Height * (5./16.) |> int
                    )

            // Reset focus
            this.TextBox.Focus() |> ignore
            this.TextBox.SelectAll()
            this.ResultList.Selection.Select 0 // Reset selection
            if this.ResultList.ItemCount <> 0 then
                this.ResultListScrollViewer().ScrollToHome() // Scroll to top to preserve the top padding
        )
        #if !DEBUG
        this.Deactivated.Add (fun _ -> this.Hide())
        #endif

    member private this.SetupKeyboardShortcuts() =
        // Subscribe to hide command
        this.ViewModel.HideCommand.Subscribe(fun _ ->
            Threading.Dispatcher.UIThread.Post(fun _ ->
                this.Hide()
            )
        )
        |> ignore

        // Add escape key binding
        let onEscape = // Run when "Escape" is pressed
            ReactiveUI.ReactiveCommand.Create(fun () ->
                if this.ViewModel.CurrentActivator.Value.IsSome then
                    this.ViewModel.ResetActivator()
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
            let newSelectedIdx =
                match e.PhysicalKey with
                | PhysicalKey.Backspace ->
                    if this.ViewModel.CurrentActivator.Value.IsSome && this.TextBox.CaretIndex = 0 then
                        this.ViewModel.ResetActivator()
                        e.Handled <- true

                    None

                | PhysicalKey.Tab when this.ViewModel.SearchResults.Count <> 0 ->
                    match e.KeyModifiers &&& KeyModifiers.Shift = KeyModifiers.Shift with
                    | true -> (r.SelectedIndex - 1) |> max 0 |> Some
                    | _ -> (r.SelectedIndex + 1) |> min (r.ItemCount - 1) |> Some

                | _ when this.ViewModel.SearchResults.Count <> 0 ->
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

                | _ -> None

            match newSelectedIdx with
            | None -> ()
            | Some newSelectedIdx ->
                // Scroll to top or bottom to preserve the paddings
                let scrollViewer = this.ResultListScrollViewer()
                if newSelectedIdx = 0 then
                    scrollViewer.ScrollToHome()
                elif newSelectedIdx = r.ItemCount - 1 then
                    scrollViewer.ScrollToEnd()

                r.Selection.SelectedIndex <- newSelectedIdx
                e.Handled <- true
        )
        this.TextBox.AddHandler(InputElement.KeyDownEvent, d, RoutingStrategies.Tunnel)
