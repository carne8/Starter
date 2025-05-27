module Starter.UrlSearchEngine

open Starter.UrlSearchEngine.Regex
open Starter.SearchEngine

open System
open System.Diagnostics
open System.Text.RegularExpressions
open FSharp.Control.Reactive
open FluentAvalonia.UI.Controls

type SearchResult =
    { Uri: Uri }

    interface ISearchResult with
        member this.Id = this.Uri.AbsoluteUri
        member this.Name = "Open link"
        member this.Description = $"Open: {this.Uri.AbsoluteUri}"
        member this.Icon = StarterIconSource(Symbol.Globe)

type UrlSearchEngine(pluginPath) =
    inherit DynamicSearchEngine(pluginPath)

    let regex = UriRegex.Regex()

    let tryParseUri (match': Match) =
        match match'.Success with
        | false -> None
        | true ->
            let s =
                match match'.Groups.TryGetValue "scheme" with
                | true, s when s.Value <> "" -> match'.Value
                | _ -> "https://" + match'.Value

            match Uri.TryCreate(s, UriKind.Absolute) with
            | false, _ -> None
            | true, uri -> Some uri

    do  // Warm-up the regex for faster first result
        regex.Matches "" |> ignore

    override this.Id = nameof UrlSearchEngine
    override this.Name = "Link search engine"
    override this.ShortName = "Link"
    override this.Icon = StarterIconSource(Symbol.Globe)
    override this.ImportantResults = false

    override this.Search(query, _cancellationToken) =
        query
        |> regex.Matches
        |> Seq.toArray
        |> Array.collect (
            tryParseUri
            >> Option.map (fun uri ->
                match uri.Host with
                | "localhost" ->
                    [| { Uri = uri } :> ISearchResult
                       { Uri = Uri("https://localhost:8080") }
                       { Uri = Uri("https://localhost:5174") } |]
                | _ -> [| { Uri = uri } |]
            )
            >> Option.defaultValue Array.empty
        ),
        Observable.empty

    override this.SearchResultSelected(searchResult) =
        match searchResult with
        | :? SearchResult as sr ->
            ProcessStartInfo(
                FileName = sr.Uri.AbsoluteUri,
                UseShellExecute = true
            )
            |> Process.Start
            |> ignore
        | _ -> ()
