namespace Starter.Features.Config.UI.StarterSettings

open Avalonia.Controls
open Avalonia.Controls.Templates
open Avalonia.Data
open Avalonia.Markup.Xaml
open Avalonia.Styling

open Starter.SearchEngine
open FluentAvalonia.UI.Controls
open R3

type StarterSettings() as this =
    inherit UserControl()

    let prefixTextBoxItemTemplate =
        FuncDataTemplate<ActivatorPrefixViewModel>(fun vm _ ->
            let c = TextBox(Text = vm.Prefix)
            c.Bind(TextBox.TextProperty, Binding(nameof vm.Prefix)) |> ignore
            c
        )

    let activatorPrefixItemTemplate =
        FuncDataTemplate<ActivatorPrefixViewModel>(fun vm _ ->
            let createIconSource () = StarterIconSource.buildIconSource (this.ActualThemeVariant = ThemeVariant.Light) vm.Icon

            let c = SettingsExpanderItem(
                IconSource = createIconSource(),
                Content = TextBlock(Text = vm.Name),
                Footer = vm,
                FooterTemplate = prefixTextBoxItemTemplate
            )

            c.PropertyChanged.Add(fun change -> if change.NewValue :? ThemeVariant then c.IconSource <- createIconSource())
            c
        )

    let searchEngineActivatorsItemTemplate =
        FuncDataTemplate<SearchEngineActivatorsViewModel>(fun vm _ ->
            let createIconSource () = StarterIconSource.buildIconSource (this.ActualThemeVariant = ThemeVariant.Light) vm.Icon
            let c = SettingsExpander(
                IconSource = createIconSource(),
                Header = vm.Name,
                ItemTemplate = activatorPrefixItemTemplate,
                FooterTemplate = prefixTextBoxItemTemplate
            )

            let sub = vm.ActivatorPrefixVms.Subscribe(function
                | Choice1Of2 vm ->
                    c.ItemsSource <- null
                    c.Footer <- vm
                | Choice2Of2 vms ->
                    c.ItemsSource <- vms
                    c.Footer <- null
            )
            c.Unloaded.Add(fun _ -> sub.Dispose())
            c.PropertyChanged.Add(fun change -> if change.NewValue :? ThemeVariant then c.IconSource <- createIconSource())
            c
        )

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this

        this.DataContextChanged.Add(fun _ ->
            match this.DataContext with
            | :? ViewModel as vm ->
                let activatorPrefixes = this.GetControl<ItemsControl> "ActivatorPrefixes"

                // Set ActivatorPrefixes item template
                activatorPrefixes.ItemTemplate <- searchEngineActivatorsItemTemplate

                // Bind SearchEnginePrefixes source
                let sub = vm.SearchEngineActivators.Subscribe(fun vms -> activatorPrefixes.ItemsSource <- vms)

                this.Unloaded.Add(ignore >> sub.Dispose)
                this.Loaded.Add(fun _ -> vm.OnOpened())
            | _ -> ()
        )
