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

        if OperatingSystem.IsLinux() then
            this.ExtendClientAreaToDecorationsHint <- false
            this.GetControl("Title").IsVisible <- false
            this.GetControl("NavigationView").Margin <- Thickness(0, 10, 0, 0)

        // Bind settings control
        let contentControl = this.GetControl<ContentControl> "ContentControl"
        let sub = contentControl.Bind(ContentControl.ContentProperty, Data.Binding "SelectedPage.Control")
        this.Unloaded.Add(fun _ -> sub.Dispose())

        // Bind data template
        let navigationView = this.GetControl<NavigationView> "NavigationView"
        navigationView.MenuItemTemplate <-
            FuncDataTemplate<MenuItemVM>(fun vm _ ->
                let c =
                    NavigationViewItem(
                        Content = vm.Name,
                        FontFamily = unbox this.Resources["JetBrainsMono"],
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

    override this.OnDataContextChanged(e) =
        base.OnDataContextChanged(e)
        match this.DataContext with
        | :? SettingsWindowViewModel as dc ->
            this.BackgroundKind <- dc.BaseConfiguration.Background
        | _ -> ()
