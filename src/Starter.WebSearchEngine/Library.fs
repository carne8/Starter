namespace Starter.WebSearchEngine

open System.Net.Http
open Starter.SearchEngine
open Starter.WebSearchEngine
open Starter.WebSearchEngine.Logger

open System
open System.Threading
open System.Diagnostics

open R3

type WebSearchEngine(searchEngine: BehaviorSubject<SearchEngine>) as this =
    let suggestionRequests = new Subject<string * CancellationToken>()
    let suggestions = new Subject<ISearchResult seq>()
    let changed = DelegateEvent<EventHandler>()

    do this.Initialize()

    member this.Initialize() =
        searchEngine.Subscribe(fun _ -> changed.Trigger [| null; EventArgs.Empty |]) |> ignore

        suggestionRequests
            .Debounce(TimeSpan.FromMilliseconds 60L)
            .Subscribe(fun (query, ct) ->
                if query |> String.IsNullOrEmpty |> not then
                    task {
                        let se = searchEngine.Value
                        let! newSuggestions = se.LoadSuggestions ct query

                        newSuggestions
                        |> Array.map (fun s ->
                            { Name = s
                              Description = "Using " + se.Name
                              Uri = se.LoadSearchUrl s
                              Icon = se.StarterIcon }
                            :> ISearchResult
                        )
                        |> suggestions.OnNext
                    } |> ignore
            )
        |> ignore

    member this.SimpleSearch(query) : struct (_ * _) =
        let r =
            if query |> String.IsNullOrEmpty then Seq.empty
            else
                { Name = $"Search \"{query}\""
                  Description = "Using " + searchEngine.Value.Name
                  Uri = searchEngine.Value.LoadSearchUrl query
                  Icon = searchEngine.Value.StarterIcon }
                :> ISearchResult
                |> Seq.singleton

        r, Observable.Empty()

    member this.SuggestionsSearch(query, ct) : struct (_ * _) =
        let r =
            if query = "" then Seq.empty
            else
                { Name = query
                  Description = "Using " + searchEngine.Value.Name
                  Uri = searchEngine.Value.LoadSearchUrl query
                  Icon = searchEngine.Value.StarterIcon }
                :> ISearchResult
                |> Seq.singleton

        suggestionRequests.OnNext(query, ct)
        r, suggestions.AsObservable()

    member this.Id = nameof(WebSearchEngine)
    member this.Name = "Web search"
    member this.ShortName = searchEngine.Value.ShortName
    member this.Icon = searchEngine.Value.StarterIcon

    interface IDynamicSearchEngine with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.ShortName = this.ShortName
        member this.Icon = this.Icon
        member this.ResultsPriority = ResultPriority.Fallback
        member this.BufferResults = false
        member this.Activators =
            let evt = DelegateEvent<EventHandler>()
            searchEngine.Subscribe(fun engine -> evt.Trigger([| null; EventArgs.Empty |])) |> ignore

            [| { new ISearchEngineDynamicActivator with
                   member _.Id = this.Id
                   member _.Name = this.Name
                   member _.ShortName = this.ShortName
                   member _.Icon = this.Icon
                   member _.SearchEngineId = this.Id

                   [<CLIEvent>]
                   member _.Changed = evt.Publish } |]

        member this.Search(query, ct, usedActivator) =
            match usedActivator with
            | null -> this.SimpleSearch(query)
            | _ -> this.SuggestionsSearch(query, ct)

        member this.SearchResultSelected(searchResult) =
            match searchResult with
            | :? SearchResult as sr ->
                ProcessStartInfo(
                    FileName = sr.Uri,
                    UseShellExecute = true
                )
                |> Process.Start
                |> function null -> () | d -> d.Dispose()
            | _ -> ()

        [<CLIEvent>]
        member this.Changed = changed.Publish

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| nameof WebSearchEngine |]
    override this.LoadSearchEngine(_, pluginConfigDirectory, logger, _) =
        setLogger logger

        let httpClient = new HttpClient()
        let settings = Views.SettingsViewModel(pluginPath, pluginConfigDirectory, httpClient)
        let searchEngine = settings.SearchEngine

        WebSearchEngine searchEngine, Views.Settings(settings)

    override this.LoadDataTemplates() = null
