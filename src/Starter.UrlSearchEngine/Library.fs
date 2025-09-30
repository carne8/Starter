module Starter.UrlSearchEngine

open Starter.UrlSearchEngine.Regex
open Starter.SearchEngine

open System
open System.Diagnostics
open System.Text.RegularExpressions
open Avalonia.Media
open R3

let icon = StarterIconSource(StreamGeometry.Parse "M34,14 C39.5228475,14 44,18.4771525 44,24 C44,29.4292399 39.6733292,33.8479317 34.2799048,33.9961582 L34,34 L28.25,34 C27.5596441,34 27,33.4403559 27,32.75 C27,32.1027913 27.4918747,31.5704661 28.1221948,31.5064536 L28.25,31.5 L34,31.5 C38.1421356,31.5 41.5,28.1421356 41.5,24 C41.5,19.9390827 38.2725256,16.6319825 34.2427311,16.5038536 L34,16.5 L28.25,16.5 C27.5596441,16.5 27,15.9403559 27,15.25 C27,14.6027913 27.4918747,14.0704661 28.1221948,14.0064536 L28.25,14 L34,14 Z M19.75,14 C20.4403559,14 21,14.5596441 21,15.25 C21,15.8972087 20.5081253,16.4295339 19.8778052,16.4935464 L19.75,16.5 L14,16.5 C9.85786438,16.5 6.5,19.8578644 6.5,24 C6.5,28.0609173 9.72747441,31.3680175 13.7572689,31.4961464 L14,31.5 L19.75,31.5 C20.4403559,31.5 21,32.0596441 21,32.75 C21,33.3972087 20.5081253,33.9295339 19.8778052,33.9935464 L19.75,34 L14,34 C8.4771525,34 4,29.5228475 4,24 C4,18.5707601 8.32667079,14.1520683 13.7200952,14.0038418 L14,14 L19.75,14 Z M13,22.75 L35,22.75 C35.6903559,22.75 36.25,23.3096441 36.25,24 C36.25,24.6472087 35.7581253,25.1795339 35.1278052,25.2435464 L35,25.25 L13,25.25 C12.3096441,25.25 11.75,24.6903559 11.75,24 C11.75,23.3527913 12.2418747,22.8204661 12.8721948,22.7564536 L13,22.75 L35,22.75 L13,22.75 Z")

type SearchResult =
    { Uri: Uri }

    interface ISearchResult with
        member this.Id = this.Uri.AbsoluteUri
        member this.Name = "Open link"
        member this.Description = $"Open: {this.Uri.AbsoluteUri}"
        member this.Keywords = Array.empty
        member this.Icon = icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty

type UrlSearchEngine(pluginPath, configDir, logger) =
    inherit DynamicSearchEngine(pluginPath, configDir, logger)

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
    override this.Name = "Link opener"
    override this.ShortName = "Link"
    override this.Icon = icon
    override this.ImportantResults = false

    override this.Search(query, _ct, _) =
        query
        |> regex.Matches
        |> Seq.collect (
            tryParseUri
            >> Option.map (fun uri ->
                match uri.Host with
                | "localhost" ->
                    [| { Uri = uri } :> ISearchResult
                       { Uri = Uri("https://localhost:8080") }
                       { Uri = Uri("https://localhost:5174") } |] // TODO: Allow the user to set custom values
                | _ -> [| { Uri = uri } |]
            )
            >> Option.defaultValue Array.empty
        ),
        Observable.Empty()

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

    override this.LoadSettingsControl() = null
