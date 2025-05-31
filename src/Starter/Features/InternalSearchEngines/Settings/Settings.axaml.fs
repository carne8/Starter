namespace Starter.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Markup.Xaml

open Starter
open System.Collections.Generic
open FluentAvalonia.UI.Controls
open R3

type Settings() as this =
    inherit Window()

    let disposables = List()
    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
        #if DEBUG
        this.AttachDevTools()
        #endif

        this.DataContextChanged.Add(fun _ ->
            match this.DataContext with
            | :? ViewModels.SettingsViewModel as vm ->
                let control = this.GetControl<SettingsExpander> "SearchEnginePrefixes"

                vm.SearchEnginePrefixes.Subscribe(fun vms ->
                    control.ItemsSource <- vms
                )
                |> disposables.Add

            | _ -> ()
        )

        this.Closed.Add(fun _ -> disposables |> Seq.iter _.Dispose())
