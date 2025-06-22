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

    let searchEnginePrefixesItemTemplate =
        FuncDataTemplate<SearchEnginePrefixViewModel>(fun vm _ ->
            let createIconSource () = StarterIconSource.buildIconSource (this.ActualThemeVariant = ThemeVariant.Light) vm.Icon

            let footer = TextBox(Text = vm.Prefix)
            footer.Bind(TextBox.TextProperty, Binding("Prefix")) |> ignore

            let c = SettingsExpanderItem(
                IconSource = createIconSource(),
                Content = TextBlock(Text = vm.Name),
                Footer = footer
            )

            c.PropertyChanged.Add(fun change -> if change.NewValue :? ThemeVariant then c.IconSource <- createIconSource())

            c
        )

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this

        this.DataContextChanged.Add(fun _ ->
            match this.DataContext with
            | :? ViewModel as vm ->
                let searchEnginePrefixes = this.GetControl<SettingsExpander> "SearchEnginePrefixes"

                // Set SearchEnginePrefixes item template
                searchEnginePrefixes.ItemTemplate <- searchEnginePrefixesItemTemplate

                // Bind SearchEnginePrefixes source
                let sub = vm.SearchEnginePrefixes.Subscribe(fun vms -> searchEnginePrefixes.ItemsSource <- vms)

                this.Unloaded.Add(ignore >> sub.Dispose)
                this.Loaded.Add(fun _ -> vm.OnOpened())
            | _ -> ()
        )
