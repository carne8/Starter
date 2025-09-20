namespace Starter.Features.InternalSearchEngines.Settings.Views

open Starter.Features.InternalSearchEngines.Settings.ViewModels
open Starter.SearchEngine

open Avalonia.Controls
open Avalonia.Controls.Templates
open Avalonia.Data
open Avalonia.Styling

open FluentAvalonia.UI.Controls
open R3

module ActivatorPrefixes =
    let prefixTextBoxItemTemplate =
        FuncDataTemplate<ActivatorPrefixViewModel>(fun vm _ ->
            let c = TextBox(Text = vm.Prefix)
            c.Bind(TextBox.TextProperty, Binding(nameof vm.Prefix)) |> ignore
            c
        )

    let activatorPrefixItemTemplate (parent: Control) =
        FuncDataTemplate<ActivatorPrefixViewModel>(fun vm _ ->
            let createIconSource () = StarterIconSource.buildIconSource (parent.ActualThemeVariant = ThemeVariant.Light) vm.Icon

            let c = SettingsExpanderItem(
                IconSource = createIconSource(),
                Content = TextBlock(Text = vm.Name),
                Footer = vm,
                FooterTemplate = prefixTextBoxItemTemplate
            )

            c.PropertyChanged.Add(fun change -> if change.NewValue :? ThemeVariant then c.IconSource <- createIconSource())
            c
        )

    let searchEngineActivatorsItemTemplate (parent: Control) =
        FuncDataTemplate<SearchEngineActivatorsViewModel>(fun vm _ ->
            let createIconSource () = StarterIconSource.buildIconSource (parent.ActualThemeVariant = ThemeVariant.Light) vm.Icon
            let c = SettingsExpander(
                IconSource = createIconSource(),
                Header = vm.Name,
                ItemTemplate = activatorPrefixItemTemplate parent,
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
