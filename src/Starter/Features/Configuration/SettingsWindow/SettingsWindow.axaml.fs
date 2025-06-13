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
            | :? WindowViewModel as vm -> this.DataContextLoaded(vm)
            | _ -> ()
        )

    member this.DataContextLoaded(vm: WindowViewModel) =
        // Bind MenuItems
        let navigationView = this.GetControl<NavigationView> "NavigationView"
        let sub = vm.MenuItems.Subscribe(fun vms -> navigationView.MenuItemsSource <- vms)
        this.Unloaded.Add(fun _ -> sub.Dispose())

        // Bind settings control
        let contentControl = this.GetControl<Border> "ContentControl"
        let sub = contentControl.Bind(Border.ChildProperty, Data.Binding("SelectedPage.Control"))
        this.Unloaded.Add(fun _ -> sub.Dispose())

        // Open Starter settings by default // TODO: Run only when opened from Starter
        this.Activated.Add(fun _ ->
            vm.SelectedPage <- vm.MenuItems.Value[0]
        )
