namespace Starter.WebSearchEngine

open System
open System.IO
open System.Net.Http
open System.Text.Json
open System.Threading
open System.Threading.Tasks

open Avalonia.Threading
open Starter.SearchEngine
open Starter.WebSearchEngine.Logger
open Avalonia.Svg.Skia

type SearchEngineKind =
    | Google
    | Qwant
    | DuckDuckGo
    | Bing
    | Ecosia

    override this.ToString() = this |> SearchEngineKind.getName

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

    static member getQueryUrl (se: SearchEngineKind) (query: string) =
        let escapedQuery = Uri.EscapeDataString query
        match se with
        | Google -> $"https://www.google.com/search?q={escapedQuery}"
        | Qwant -> $"https://www.qwant.com/?q={escapedQuery}"
        | DuckDuckGo -> $"https://duckduckgo.com/?q={escapedQuery}"
        | Bing -> $"https://www.bing.com/search?q={escapedQuery}"
        | Ecosia -> $"https://www.ecosia.org/search?q={escapedQuery}"

    static member getSuggestionsUrl (se: SearchEngineKind) (query: string) =
        let escapedQuery = Uri.EscapeDataString query
        match se with
        | Google -> $"https://suggestqueries.google.com/complete/search?client=firefox&q={escapedQuery}"
        | Qwant -> $"https://api.qwant.com/api/suggest/?client=opensearch&q={escapedQuery}"
        | DuckDuckGo -> $"https://duckduckgo.com/ac/?type=list&q={escapedQuery}"
        | Bing -> $"https://www.bingapis.com/api/v7/suggestions?appid=6D0A9B8C5100E9ECC7E11A104ADD76C10219804B&q={escapedQuery}"
        | Ecosia -> $"https://ac.ecosia.org/autocomplete?type=list&q={escapedQuery}"

    static member deserializeSuggestionsRequest (se: SearchEngineKind) (query: string) (stream: Stream) =
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

    static member loadSuggestions (httpClient: HttpClient) (se: SearchEngineKind) (ct: CancellationToken) (query: string) =
        task {
            let url = query |> SearchEngineKind.getSuggestionsUrl se
            use req = new HttpRequestMessage(HttpMethod.Get, url)
            Headers.ProductInfoHeaderValue("Mozilla", "5.0")
            |> req.Headers.UserAgent.Add
            req.Headers.Accept.Add(Headers.MediaTypeWithQualityHeaderValue("*/*"))

            try
                let! res = httpClient.SendAsync(req, ct)
                res.EnsureSuccessStatusCode() |> ignore
                let! jsonStream = res.Content.ReadAsStreamAsync()

                let! suggestions =
                    jsonStream
                    |> SearchEngineKind.deserializeSuggestionsRequest se query

                return Some suggestions
            with
            | :? OperationCanceledException
            | :? TaskCanceledException -> return None
            | e ->
                logger.Warning(e, "Failed to load suggestions\n{Req}", req)
                return None
        }

type SearchEngine =
    { Kind: SearchEngineKind
      Name: string
      Icon:
        {| Light: Avalonia.Media.IImage
           Dark: Avalonia.Media.IImage |}
      StarterIcon: StarterIconSource
      LoadSuggestions: CancellationToken -> string -> Task<string array option>
      LoadSearchUrl: string -> string }

    static member create pluginPath httpClient (seKind: SearchEngineKind) =
        let ìconPath =
            Path.Combine(
                pluginPath,
                "Images",
                seKind |> SearchEngineKind.getIconFilename
            )

        let lightIcon, darkIcon =
            Dispatcher.UIThread.Invoke(fun () ->
                SvgImage(Source = SvgSource.Load ìconPath, Css = ".icon-color { fill: #282b2f; }"),
                SvgImage(Source = SvgSource.Load ìconPath, Css = ".icon-color { fill: #ffffff; }")
            )

        { Kind = seKind
          Name = seKind |> SearchEngineKind.getName
          Icon = {| Light = lightIcon; Dark = darkIcon |}
          StarterIcon = StarterIconSource(lightIcon, darkIcon)
          LoadSuggestions = seKind |> SearchEngineKind.loadSuggestions httpClient
          LoadSearchUrl = seKind |> SearchEngineKind.getQueryUrl }


type SearchResult =
    { Name: string
      Description: string
      Uri: string
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Uri
        member this.Name = this.Name
        member this.Description = this.Description
        member this.Keywords = Array.empty
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
        member this.ContextMenuLoader = null
