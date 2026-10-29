namespace Starter.WebSearchEngine

open Starter.SearchEngine
open Starter.WebSearchEngine
open Starter.WebSearchEngine.Logger

open System
open System.Net.Http
open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open System.ComponentModel
open Avalonia.Controls.Templates
open R3

type WebSearchEngineActivator(engine: Starter.SearchEngine.SearchEngine) =
    inherit SearchEngineActivator()
    let propertyChangedEvent = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    override _.Id = engine.Id
    override _.Name = engine.Name
    override _.Icon = engine.Icon
    override _.SearchEngineId = engine.Id

    member this.OnPropertyChanged() =
        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs null)

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChangedEvent.Publish

type WebSearchEngine(searchEngine: BehaviorSubject<SearchEngine>) as this =
    inherit DynamicSearchEngine()

    let suggestionRequests = new Subject<string * CancellationToken>()
    let suggestions = new Subject<SearchResult seq>()
    let propertyChangedEvent = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let activators =
        let activator = WebSearchEngineActivator(this)
        searchEngine.Subscribe(fun engine -> activator.OnPropertyChanged()) |> ignore
        [| activator :> SearchEngineActivator |]

    do this.Initialize()

    member this.Initialize() =
        searchEngine.Subscribe(fun _ -> propertyChangedEvent.Trigger(this, PropertyChangedEventArgs null)) |> ignore

        suggestionRequests
            .Debounce(TimeSpan.FromMilliseconds 150L)
            .Subscribe(fun (query, ct) ->
                if query |> String.IsNullOrEmpty |> not then
                    Task.Run<unit>(fun () -> task {
                        let se = searchEngine.Value
                        match! se.LoadSuggestions ct query with
                        | None -> ()
                        | Some newSuggestions ->
                            newSuggestions
                            |> Array.map (fun s ->
                                WebSearchResult(
                                    name = s,
                                    description = "Using " + se.Name,
                                    uri = se.LoadSearchUrl s,
                                    icon = se.StarterIcon
                                )
                                :> SearchResult
                            )
                            |> suggestions.OnNext
                    }) |> ignore
            )
        |> ignore

    member this.SimpleSearch(query) : struct (_ * _) =
        let r =
            if query |> String.IsNullOrEmpty then Seq.empty
            else
                WebSearchResult(
                    name = $"Search \"{query}\"",
                    description = "Using " + searchEngine.Value.Name,
                    uri = searchEngine.Value.LoadSearchUrl query,
                    icon = searchEngine.Value.StarterIcon
                )
                :> SearchResult
                |> Seq.singleton

        r, Observable.Empty()

    member this.SuggestionsSearch(query, ct) : struct (_ * _) =
        let r =
            if query = "" then Seq.empty
            else
                WebSearchResult(
                    name = query,
                    description = "Using " + searchEngine.Value.Name,
                    uri = searchEngine.Value.LoadSearchUrl query,
                    icon = searchEngine.Value.StarterIcon
                )
                :> SearchResult
                |> Seq.singleton

        suggestionRequests.OnNext(query, ct)
        r, suggestions.AsObservable()

    override this.Id = nameof(WebSearchEngine)
    override this.Name = "Web search"
    override this.Icon = searchEngine.Value.StarterIcon
    override this.BufferResults = false
    override this.Activators = activators

    override this.Search(query, ct, usedActivator) =
        match usedActivator with
        | null -> this.SimpleSearch(query)
        | _ -> this.SuggestionsSearch(query, ct)

    override this.SearchResultSelected(searchResult) =
        match searchResult with
        | :? WebSearchResult as sr ->
            ProcessStartInfo(
                FileName = sr.Uri,
                UseShellExecute = true
            )
            |> Process.Start
            |> function null -> () | d -> d.Dispose()
        | _ -> ()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChangedEvent.Publish

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| nameof WebSearchEngine |]
    override this.LoadSearchEngine(_, pluginConfigDirectory, logger, _) =
        setLogger logger

        let httpClient = new HttpClient()
        let settings = Views.SettingsViewModel(pluginPath, pluginConfigDirectory, httpClient)
        let searchEngine = settings.SearchEngine

        WebSearchEngine searchEngine,
        SearchEngineFactory.SearchEngineSettings(
            settings,
            FuncDataTemplate<Views.SettingsViewModel>(fun vm _ ->
                Views.Settings(DataContext = vm)
            )
        )
