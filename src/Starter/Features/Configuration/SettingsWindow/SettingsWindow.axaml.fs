namespace Starter.Features.Config.UI.SettingsWindow

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

open FluentAvalonia.UI.Controls
open R3

type WindowControl() as this =
    inherit Window()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
        #if DEBUG
        this.AttachDevTools()
        #endif

        this.DataContextChanged.Add(fun _ ->
            match this.DataContext with
            | :? WindowViewModel as vm ->
                let navigationView = this.GetControl<NavigationView> "NavigationView"

                let subscription = vm.MenuItems.Subscribe(fun vms -> navigationView.MenuItemsSource <- vms)
                this.Closed.Add(ignore >> subscription.Dispose)
            | _ -> ()
        )
