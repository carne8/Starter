module Starter.WebSearchEngine

open System
open System.Diagnostics
open System.IO
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

    static member getQueryUrl (query: string) =
        function
        | Google -> $"https://www.google.com/search?q={query}"
        | Qwant -> $"https://www.qwant.com/?q={query}"
        | DuckDuckGo -> $"https://duckduckgo.com/?q={query}"
        | Bing -> $"https://www.bing.com/search?q={query}"
        | Ecosia -> $"https://https://www.ecosia.org/search?q={query}"

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

    override this.Id = nameof(WebSearchEngine)
    override this.Name = "Web search"
    override this.ShortName = searchEngineName
    override this.Icon = icon
    override this.ImportantResults = false

    override this.Search(query, _ct) =
        let r =
            if query = "" then Array.empty
            else
                { Name = $"Search \"{query}\" on {searchEngineName}"
                  Description = String.Empty
                  Uri = searchEngine |> SearchEngine.getQueryUrl query
                  Icon = icon }
                :> ISearchResult
                |> Array.singleton

        struct (r, Observable.Empty())

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
