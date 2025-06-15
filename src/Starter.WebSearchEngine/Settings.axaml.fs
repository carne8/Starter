namespace Starter.WebSearchEngine.Views

open Starter.WebSearchEngine

open Avalonia.Controls
open Avalonia.Markup.Xaml

open ReactiveUI
open R3

type SettingsViewModel(pluginPath, logger, httpClient) =
    inherit ReactiveObject()

    let searchEngine =
        new BehaviorSubject<SearchEngine>(
            Google |> SearchEngine.create pluginPath logger httpClient
        )

    let searchEngineKinds =
        [| Bing
           DuckDuckGo
           Ecosia
           Google
           Qwant |]

    member this.SearchEngine = searchEngine

    // Bindings
    member this.SelectedSeKind
        with get () = searchEngine.Value.Kind
        and set v =
            v
            |> SearchEngine.create pluginPath logger httpClient
            |> searchEngine.OnNext
            this.RaisePropertyChanged()

    member this.SearchEngineKinds = searchEngineKinds
    member this.Icon = searchEngine.Value.Icon


type Settings(viewModel) as this =
    inherit UserControl(DataContext = viewModel)

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
