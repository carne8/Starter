namespace Starter.WebSearchEngine

open System.Threading.Tasks
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
    let suggestions = new Subject<ISearchResult seq>()

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
                              Uri = se.LoadSearchUrl s
                              Icon = se.StarterIcon }
                            :> ISearchResult
                        )
                        |> suggestions.OnNext
                    } |> ignore
            )
        |> ignore

    override this.Id = nameof(WebSearchEngine)
    override this.Name = "Web search"
    override this.ShortName = searchEngine.Value.ShortName
    override this.Icon = searchEngine.Value.StarterIcon
    override this.ImportantResults = false

    member this.SimpleSearch(query) =
        let r =
            if query |> String.IsNullOrEmpty then Seq.empty
            else
                { Name = $"Search \"{query}\""
                  Description = "Using " + searchEngine.Value.Name
                  Uri = searchEngine.Value.LoadSearchUrl query
                  Icon = searchEngine.Value.StarterIcon }
                :> ISearchResult
                |> Seq.singleton

        struct (r, Observable.Empty())

    member this.SuggestionsSearch(query, ct) =
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
        struct (r, suggestions.AsObservable())

    override this.Search(query, ct, usedActivator) =
        if usedActivator <> null then
            this.SuggestionsSearch(query, ct)
        else
            this.SimpleSearch(query)

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
