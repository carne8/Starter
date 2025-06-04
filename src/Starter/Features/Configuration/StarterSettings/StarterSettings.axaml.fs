namespace Starter.Features.Config.UI.StarterSettings

open Avalonia.Controls
open Avalonia.Markup.Xaml

open FluentAvalonia.UI.Controls
open R3

type StarterSettings() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this

        this.DataContextChanged.Add(fun _ ->
            match this.DataContext with
            | :? ViewModel as vm ->
                let searchEnginePrefixes = this.GetControl<SettingsExpander> "SearchEnginePrefixes"

                let sub = vm.SearchEnginePrefixes.Subscribe(fun vms -> searchEnginePrefixes.ItemsSource <- vms)
                this.Unloaded.Add(ignore >> sub.Dispose)

                this.Loaded.Add(fun _ -> vm.OnOpened())
            | _ -> ()
        )
