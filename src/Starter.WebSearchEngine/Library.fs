module Starter.WebSearchEngine

open System
open System.IO
open System.Text.Json
open System.Threading
open System.Net.Http
open System.Net.Http.Json
open System.Diagnostics

open System.Threading.Tasks
open Starter.SearchEngine
open Avalonia.Svg.Skia
open R3

type SearchEngine =
    | Google
    | Qwant
    | DuckDuckGo
    | Bing
    | Ecosia

    static member getName =
        function
        | Google -> "Google"
        | Qwant -> "Qwant"
        | DuckDuckGo -> "DuckDuckGo"
        | Bing -> "Bing"
        | Ecosia -> "Ecosia"

    static member getIconFilename =
        function
        | Google -> "Google.svg"
        | Qwant -> "Qwant.svg"
        | DuckDuckGo -> "DuckDuckGo.svg"
        | Bing -> "Bing.svg"
        | Ecosia -> "Ecosia.svg"

    static member getQueryUrl (query: string) (se: SearchEngine) =
        let escapedQuery = Uri.EscapeDataString query
        match se with
        | Google -> $"https://www.google.com/search?q={escapedQuery}"
        | Qwant -> $"https://www.qwant.com/?q={escapedQuery}"
        | DuckDuckGo -> $"https://duckduckgo.com/?q={escapedQuery}"
        | Bing -> $"https://www.bing.com/search?q={escapedQuery}"
        | Ecosia -> $"https://https://www.ecosia.org/search?q={escapedQuery}"

type SearchResult =
    { Name: string
      Description: string
      Uri: string
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Uri
        member this.Name = this.Name
        member this.Description = this.Description
        member this.Icon = this.Icon

type WebSearchEngine(pluginPath, logger) =
    inherit DynamicSearchEngine(pluginPath, logger)

    let searchEngine = Google
    let searchEngineName = searchEngine |> SearchEngine.getName
    let iconPath =
        searchEngine
        |> SearchEngine.getIconFilename
        |> fun filename -> Path.Combine(pluginPath, "Images", filename)

    let icon =
        let s = SvgSource.Load(iconPath)
        SvgImage(Source = s)
        :> Avalonia.Media.IImage
        |> StarterIconSource

    let httpClient = new HttpClient()
    let suggestionRequests = new Subject<string * CancellationToken>()
    let suggestions = new Subject<ISearchResult array>()

    let loadSuggestions (query: string) (httpClient: HttpClient) (ct: CancellationToken) (se: SearchEngine) =
        task {
            let escapedQuery = Uri.EscapeDataString query

            let url =
                match se with
                | Google -> $"https://suggestqueries.google.com/complete/search?client=firefox&q={escapedQuery}"
                | Qwant -> failwith "Not implemented"
                | DuckDuckGo -> failwith "Not implemented"
                | Bing -> failwith "Not implemented"
                | Ecosia -> failwith "Not implemented"

            try
                let! json = httpClient.GetFromJsonAsync<JsonElement array>(url, ct)
                let mutable enumerator = json[1].EnumerateArray()
                return
                    [| while enumerator.MoveNext() do
                        let s = enumerator.Current.GetString()
                        if s <> query then
                            s |]
            with
            | :? OperationCanceledException
            | :? TaskCanceledException -> return failwith "Task cancelled"
            | e ->
                logger.Error(e, "Failed to load suggestions")
                return failwith "Failed to load suggestions"
        }

    do suggestionRequests
        .Debounce(TimeSpan.FromMilliseconds 100)
        .Subscribe(fun (query, ct) ->
            if query |> String.IsNullOrEmpty |> not then
                task {
                    let! newSuggestions = searchEngine |> loadSuggestions query httpClient ct
                    newSuggestions
                    |> Array.map (fun s ->
                        { Name = s
                          Description = "Using " + searchEngineName
                          Uri = searchEngine |> SearchEngine.getQueryUrl s
                          Icon = icon }
                        :> ISearchResult
                    )
                    |> suggestions.OnNext
                }
                |> ignore
        )
        |> ignore

    override this.Id = nameof(WebSearchEngine)
    override this.Name = "Web search"
    override this.ShortName = searchEngineName
    override this.Icon = icon
    override this.ImportantResults = false

    member this.SimpleSearch(query) =
        let r =
            if query |> String.IsNullOrEmpty then Array.empty
            else
                { Name = $"Search \"{query}\""
                  Description = "Using " + searchEngineName
                  Uri = searchEngine |> SearchEngine.getQueryUrl query
                  Icon = icon }
                :> ISearchResult
                |> Array.singleton

        struct (r, Observable.Empty())

    member this.SuggestionsSearch(query, ct) =
        let r =
            if query = "" then Array.empty
            else
                { Name = query
                  Description = "Using " + searchEngineName
                  Uri = searchEngine |> SearchEngine.getQueryUrl query
                  Icon = icon }
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

    override this.LoadSettingsControl() = null
