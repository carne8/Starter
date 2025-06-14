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

    static member getShortName =
        function
        | Google -> "Google"
        | Qwant -> "Qwant"
        | DuckDuckGo -> "DDG"
        | Bing -> "Bing"
        | Ecosia -> "Ecosia"

    static member getIconFilename =
        function
        | Google -> "Google.svg"
        | Qwant -> "Qwant.svg"
        | DuckDuckGo -> "DuckDuckGo.svg"
        | Bing -> "Bing.svg"
        | Ecosia -> "Ecosia.svg"

    static member getQueryUrl (se: SearchEngine) (query: string) =
        let escapedQuery = Uri.EscapeDataString query
        match se with
        | Google -> $"https://www.google.com/search?q={escapedQuery}"
        | Qwant -> $"https://www.qwant.com/?q={escapedQuery}"
        | DuckDuckGo -> $"https://duckduckgo.com/?q={escapedQuery}"
        | Bing -> $"https://www.bing.com/search?q={escapedQuery}"
        | Ecosia -> $"https://www.ecosia.org/search?q={escapedQuery}"

    static member getSuggestionsUrl (se: SearchEngine) (query: string) =
        let escapedQuery = Uri.EscapeDataString query
        match se with
        | Google -> $"https://suggestqueries.google.com/complete/search?client=firefox&q={escapedQuery}"
        | Qwant -> $"https://api.qwant.com/api/suggest/?client=opensearch&q={escapedQuery}"
        | DuckDuckGo -> $"https://duckduckgo.com/ac/?type=list&q={escapedQuery}"
        | Bing -> $"https://www.bingapis.com/api/v7/suggestions?appid=6D0A9B8C5100E9ECC7E11A104ADD76C10219804B&q={escapedQuery}"
        | Ecosia -> $"https://ac.ecosia.org/autocomplete?type=list&q={escapedQuery}"

    static member deserializeSuggestionsRequest (se: SearchEngine) (query: string) (stream: Stream) =
        task {
            let! json = JsonDocument.ParseAsync(stream)

            let mutable enumerator, map =
                match se with
                | Google
                | Qwant
                | Ecosia
                | DuckDuckGo -> json.RootElement[1].EnumerateArray(), id
                | Bing ->
                    (json.RootElement.GetProperty("suggestionGroups")[0])
                        .GetProperty("searchSuggestions")
                        .EnumerateArray(),
                    (fun (jsonElement: JsonElement) -> jsonElement.GetProperty("displayText"))

            return
                [| while enumerator.MoveNext() do
                    let s = enumerator.Current |> map |> _.GetString()
                    if s <> query then s |]
        }

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

    let searchEngine = SearchEngine.Google
    let searchEngineName = searchEngine |> SearchEngine.getName
    let searchEngineShortName = searchEngine |> SearchEngine.getShortName
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
            let url = query |> SearchEngine.getSuggestionsUrl se
            use req = new HttpRequestMessage(HttpMethod.Get, url)
            Headers.ProductInfoHeaderValue("Mozilla", "5.0")
            |> req.Headers.UserAgent.Add
            req.Headers.Accept.Add(Headers.MediaTypeWithQualityHeaderValue("*/*"))

            try
                let! res = httpClient.SendAsync(req, ct)
                res.EnsureSuccessStatusCode() |> ignore
                let! jsonStream = res.Content.ReadAsStreamAsync()

                return! jsonStream |> SearchEngine.deserializeSuggestionsRequest se query
            with
            | :? OperationCanceledException
            | :? TaskCanceledException -> return failwith "Task cancelled"
            | e ->
                logger.Error(e, "Failed to load suggestions\n{Req}", req)
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
                          Uri = s |> SearchEngine.getQueryUrl searchEngine
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
    override this.ShortName = searchEngineShortName
    override this.Icon = icon
    override this.ImportantResults = false

    member this.SimpleSearch(query) =
        let r =
            if query |> String.IsNullOrEmpty then Array.empty
            else
                { Name = $"Search \"{query}\""
                  Description = "Using " + searchEngineName
                  Uri = query |> SearchEngine.getQueryUrl searchEngine
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
                  Uri = query |> SearchEngine.getQueryUrl searchEngine
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
