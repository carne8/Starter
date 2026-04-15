namespace Starter.WebSearchEngine.Views

open Starter.WebSearchEngine

open Avalonia.Styling
open Avalonia.Controls
open Avalonia.Markup.Xaml

open FluentAvalonia.UI.Controls
open ReactiveUI
open R3

type SettingsViewModel(pluginPath, configDir, httpClient) =
    inherit ReactiveObject()

    let configPath = System.IO.Path.Combine(configDir, Config.ConfigFilename)
    let baseConfig = configPath |> Config.loadConfig
    let config = new BehaviorSubject<Config.Config>(baseConfig)

    let searchEngine =
        new BehaviorSubject<SearchEngine>(
            config.Value.SearchEngine |> SearchEngine.create pluginPath httpClient
        )

    let searchEngineKinds =
        [| Bing
           DuckDuckGo
           Ecosia
           Google
           Qwant |]

    do config.Skip(1).Subscribe(fun newConfig -> newConfig |> Config.saveConfig configPath) |> ignore
       searchEngine.Skip(1).Subscribe(fun se ->
           let newConfig: Config.Config = { SearchEngine = se.Kind }
           newConfig |> Config.saveConfig configPath
       ) |> ignore

    member this.SearchEngine = searchEngine

    // Bindings
    member this.SelectedSeKind
        with get () = searchEngine.Value.Kind
        and set v =
            v
            |> SearchEngine.create pluginPath httpClient
            |> searchEngine.OnNext
            this.RaisePropertyChanged(nameof this.SelectedSeKind)
            this.RaisePropertyChanged(nameof this.Icon)

    member this.SearchEngineKinds = searchEngineKinds
    member this.Icon = searchEngine.Value.Icon


type Settings(viewModel: SettingsViewModel) as this =
    inherit UserControl(DataContext = viewModel)

    do this.InitializeComponent()

    // Prevent Avalonia compiler to warn
    new() = Settings(failwith "Settings control should not be initialized without parameters")

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this

        // Bind icon source
        let expander = this.GetControl<FASettingsExpander> "SearchEngine"
        let setIconSource () =
            expander.IconSource <-
                match this.ActualThemeVariant = ThemeVariant.Light with
                | true -> FAImageIconSource(Source = viewModel.Icon.Light)
                | false -> FAImageIconSource(Source = viewModel.Icon.Dark)

        setIconSource()
        viewModel.PropertyChanged.Add(fun change -> if change.PropertyName = nameof viewModel.Icon then setIconSource())
        this.PropertyChanged.Add(fun change -> if change.NewValue :? ThemeVariant then setIconSource())
