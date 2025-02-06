namespace Starter.Views

open Starter
open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml
open System

[<AutoOpen>]
module Helpers =
    type IObservable<'a> with
        member this.SubscribeOnUIThread(f) =
            this.Subscribe(fun value ->
                Threading.Dispatcher.UIThread.Post(fun _ ->
                    f value
                )
            ) |> ignore

type MainWindow () as this =
    inherit Window ()

    let wndProcCallback =
        Win32Properties.CustomWndProcHookCallback(
            fun (_hWnd: nativeint) (msg: uint32) (_wParam: nativeint) (_lParam: nativeint) _ ->
                if msg = uint Native.Windows.Api.WINDOW_MESSAGE.WM_HOTKEY then
                    this.Show()
                0
        )

    do this.InitializeComponent()

    member this.ViewModel = this.DataContext :?> ViewModels.MainWindowViewModel
    member this.TextBox = this.Get<TextBox> "TextBox"
    member this.ResultList = this.Get<ListBox> "ResultList"

    member private this.InitializeComponent() =
        AvaloniaXamlLoader.Load(this)
        #if DEBUG
        this.AttachDevTools()
        #endif

        Win32Properties.AddWndProcHookCallback(this, wndProcCallback)

        this.Loaded.Add(fun _ -> this.SetupKeyboardShortcuts())

        this.Activated.Add (fun _ ->
            this.TextBox.Focus() |> ignore
            this.TextBox.SelectAll()
            this.ResultList.SelectedIndex <- -1 // Reset selection
        )
        // this.Deactivated.Add (fun _ -> this.Hide())

    member private this.SetupKeyboardShortcuts() =
        this.ViewModel.HideCommand.SubscribeOnUIThread(fun _ -> this.Hide())

        let r = this.ResultList
        this.ViewModel.FocusUpCommand.SubscribeOnUIThread(fun _ ->
            r.SelectedIndex <- r.SelectedIndex - 1
        )
        this.ViewModel.FocusDownCommand.SubscribeOnUIThread(fun _ ->
            r.SelectedIndex <- min (r.SelectedIndex + 1) (r.ItemCount - 1)
        )
