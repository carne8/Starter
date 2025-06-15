namespace Starter.WebSearchEngine.Views

open Starter.WebSearchEngine

open Avalonia.Controls
open Avalonia.Markup.Xaml

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


type Settings(viewModel) as this =
    inherit UserControl(DataContext = viewModel)

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
