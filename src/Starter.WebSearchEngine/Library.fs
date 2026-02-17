namespace Starter.WebSearchEngine

open Starter.SearchEngine
open Starter.WebSearchEngine
open Starter.WebSearchEngine.Logger

open System
open System.Threading
open System.Net.Http
open System.Diagnostics

open R3

type WebSearchEngine(pluginPath, configDir, logger) as this =
    inherit DynamicSearchEngine(pluginPath, configDir, logger)

    do setLogger logger
    let httpClient = new HttpClient()

    let settings = Views.SettingsViewModel(pluginPath, configDir, httpClient)
    let searchEngine = settings.SearchEngine

    let suggestionRequests = new Subject<string * CancellationToken>()
    let suggestions = new Subject<ISearchResult seq>()

    do this.Initialize()

    member this.Initialize() =
        this.OnChanged
        |> searchEngine.Subscribe
        |> ignore

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

    member this.OnChanged _ = base.OnChanged()

    override this.Id = nameof(WebSearchEngine)
    override this.Name = "Web search"
    override this.ShortName = searchEngine.Value.ShortName
    override this.Icon = searchEngine.Value.StarterIcon
    override this.ImportantResults = false
    override this.UseAsyncEnumerable = false
    override this.Activators =
        let evt = DelegateEvent<EventHandler>()
        searchEngine.Subscribe(fun engine -> evt.Trigger([| null; EventArgs.Empty |])) |> ignore

        [| { new ISearchEngineDynamicActivator with
               member _.Id = this.Id
               member _.Icon = this.Icon
               member _.Name = this.Name
               member _.ShortName = this.ShortName
               member _.SearchEngineId = this.Id

               [<CLIEvent>]
               member _.Changed = evt.Publish } |]

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

    override this.SearchAsync(_, _) = failwith "todo"
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
            |> function null -> () | d -> d.Dispose()
        | _ -> ()

    override this.LoadSettingsControl() =
        try
            Views.Settings(settings)
        with e ->
            logger.Error(e, "Failed to create settings view")
            failwith "Failed to create settings view"
