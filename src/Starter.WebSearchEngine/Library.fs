namespace Starter.WebSearchEngine

open Starter.SearchEngine
open Starter.WebSearchEngine
open Starter.WebSearchEngine.Logger

open System
open System.Threading
open System.Net.Http
open System.Diagnostics

open R3

type WebSearchEngine(pluginPath, configDir, logger) =
    inherit DynamicSearchEngine(pluginPath, configDir, logger)

    do setLogger logger
    let httpClient = new HttpClient()

    let settings = Views.SettingsViewModel(pluginPath, configDir, httpClient)
    let searchEngine = settings.SearchEngine

    let suggestionRequests = new Subject<string * CancellationToken>()
    let suggestions = new Subject<ISearchResult array>()

    do
        suggestionRequests
            .Debounce(TimeSpan.FromMilliseconds 60)
            .Subscribe(fun (query, ct) ->
                if query |> String.IsNullOrEmpty |> not then
                    task {
                        let se = searchEngine.Value
                        let! newSuggestions = se.LoadSuggestions ct query

                        newSuggestions
                        |> Array.map (fun s ->
                            { Name = s
                              Description = "Using " + se.Name
                              Uri = se.LoadSearchUrl query
                              Icon = se.StarterIcon }
                            :> ISearchResult
                        )
                        |> suggestions.OnNext
                    }
                    |> ignore
            )
        |> ignore

    override this.Id = nameof(WebSearchEngine)
    override this.Name = "Web search"
    override this.ShortName = searchEngine.Value.ShortName
    override this.Icon = searchEngine.Value.StarterIcon
    override this.ImportantResults = false

    member this.SimpleSearch(query) =
        let r =
            if query |> String.IsNullOrEmpty then Array.empty
            else
                { Name = $"Search \"{query}\""
                  Description = "Using " + searchEngine.Value.Name
                  Uri = searchEngine.Value.LoadSearchUrl query
                  Icon = searchEngine.Value.StarterIcon }
                :> ISearchResult
                |> Array.singleton

        struct (r, Observable.Empty())

    member this.SuggestionsSearch(query, ct) =
        let r =
            if query = "" then Array.empty
            else
                { Name = query
                  Description = "Using " + searchEngine.Value.Name
                  Uri = searchEngine.Value.LoadSearchUrl query
                  Icon = searchEngine.Value.StarterIcon }
                :> ISearchResult
                |> Array.singleton

        suggestionRequests.OnNext(query, ct)
        struct (r, suggestions.AsObservable())

    override this.Search(query, ct, singleSearchEngineModeActivated) =
        match singleSearchEngineModeActivated with
        | false -> this.SimpleSearch(query)
        | true -> this.SuggestionsSearch(query, ct)

    override this.SearchResultSelected(searchResult) =
        match searchResult with
        | :? SearchResult as sr ->
            ProcessStartInfo(
                FileName = sr.Uri,
                UseShellExecute = true
            )
            |> Process.Start
            |> ignore
        | _ -> ()

    override this.LoadSettingsControl() =
        try
            Views.Settings(settings)
        with e ->
            logger.Error(e, "Failed to create settings view")
            failwith "Failed to create settings view"
