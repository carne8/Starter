namespace Starter.Features.InternalSearchEngines.Settings.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Templates
open Avalonia.Markup.Xaml
open Avalonia.Platform
open Avalonia.Styling

open FluentAvalonia.UI.Controls
open System
open R3
open Starter.Features.InternalSearchEngines.Settings.ViewModels
open Starter.Controls
open Starter.SearchEngine

type SettingsWindow() as this =
    inherit TranslucentWindow()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
        #if DEBUG
        this.AttachDevTools()
        #endif

        this.DataContextChanged.Add(fun _ ->
            match this.DataContext with
            | :? SettingsWindowViewModel as vm -> this.DataContextLoaded(vm)
            | _ -> ()
        )

        if OperatingSystem.IsLinux() then
            this.ExtendClientAreaToDecorationsHint <- false
            this.GetControl("Title").IsVisible <- false
            this.GetControl("NavigationView").Margin <- Thickness(0, 10, 0, 0)

        let navigationView = this.GetControl<NavigationView> "NavigationView"
        navigationView.MenuItemTemplate <-
            FuncDataTemplate<MenuItemVM>(fun vm _ ->
                let c =
                    NavigationViewItem(
                        Content = vm.Name,
                        FontFamily = (unbox this.Resources["JetBrainsMono"]),
                        IconSource = (vm.Icon |> StarterIconSource.buildIconSource (this.ActualThemeVariant = ThemeVariant.Light))
                    )
                c.PropertyChanged.Add(fun change ->
                    match change.NewValue with
                    | :? ThemeVariant as theme ->
                        c.IconSource <- vm.Icon |> StarterIconSource.buildIconSource (theme = ThemeVariant.Light)
                    | _ -> ()
                )
                c
            )

    member this.DataContextLoaded(vm: SettingsWindowViewModel) =
        // Bind MenuItems
        let navigationView = this.GetControl<NavigationView> "NavigationView"
        let sub1 = vm.MenuItems.Subscribe(fun vms -> navigationView.MenuItemsSource <- vms)

        // Bind settings control
        let contentControl = this.GetControl<Border> "ContentControl"
        let sub2 = contentControl.Bind(Border.ChildProperty, Data.Binding("SelectedPage.Control"))

        // Bind background transparency
        let sub3 = vm.Configuration.Subscribe(fun config -> this.BackgroundKind <- config.Background)
        this.BackgroundKind <- vm.BaseConfiguration.Background

        this.Unloaded.Add(fun _ ->
            sub1.Dispose()
            sub2.Dispose()
            sub3.Dispose()
        )
