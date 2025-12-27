namespace Starter.Features.InternalSearchEngines.Settings.Views

open Avalonia.Input
open Starter.Features.InternalSearchEngines.Settings.ViewModels
open Starter.Features.InternalSearchEngines.Settings.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml

open R3

type Settings() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this

        this.DataContextChanged.Add(fun _ ->
            match this.DataContext with
            | :? SettingsViewModel as vm ->
                let activatorPrefixes = this.GetControl<ItemsControl> "ActivatorPrefixes"

                // Set ActivatorPrefixes item template
                activatorPrefixes.ItemTemplate <- ActivatorPrefixes.searchEngineActivatorsItemTemplate this

                // Bind SearchEnginePrefixes source
                let sub = vm.SearchEngineActivators.Subscribe(fun vms -> activatorPrefixes.ItemsSource <- vms)

                this.Unloaded.Add(ignore >> sub.Dispose)
                this.Loaded.Add(fun _ -> vm.OnOpened())
            | _ -> ()
        )

    member this.KeyboardShortcutKeyDown(_sender: obj, args: KeyEventArgs) =
        match this.DataContext with
        | :? SettingsViewModel as dc -> dc.KeyDooooown args.PhysicalKey
        | _ -> ()

    member this.KeyboardShortcutKeyUp(_sender: obj, args: KeyEventArgs) =
        // match this.DataContext with
        // | :? SettingsViewModel as dc -> dc.KeyDooooown args.PhysicalKey
        // | _ -> ()
        ()
