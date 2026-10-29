module Starter.UrlSearchEngine

open Starter.UrlSearchEngine.Regex
open Starter.SearchEngine

open System
open System.Diagnostics
open System.Text.RegularExpressions
open Avalonia
open Avalonia.Media
open Avalonia.Threading
open R3

let createIcon lightMode =
    let pen =
        Pen(
            (if lightMode then Brushes.Black else Brushes.White),
            1.5,
            lineCap = PenLineCap.Round,
            lineJoin = PenLineJoin.Round
        )

    let group = DrawingGroup()
    group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M10 13a5 5 0 0 0 7.54.54l3-3a5 5 0 0 0-7.07-7.07l-1.72 1.71"))
    group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M14 11a5 5 0 0 0-7.54-.54l-3 3a5 5 0 0 0 7.07 7.07l1.71-1.71"))

    DrawingImage(group, Viewbox = Rect(0, 0, 24, 24))

let icon =
    Dispatcher.UIThread.Invoke(fun () ->
        StarterIconSource(createIcon true, createIcon false)
    )

type UrlSearchResult(uri: Uri) =
    inherit SearchResult()

    member this.Uri = uri
    override this.Id = uri.AbsoluteUri
    override this.Name = "Open link"
    override this.Description = $"Open: {uri.AbsoluteUri}"
    override this.Keywords = Array.empty
    override this.Icon = icon
    override this.Priority = ResultPriority.Fallback
    override this.ShowIfNoActivator = true
    override this.ActivatorFilter = Array.empty
    override this.ContextMenuLoader = null

type UrlSearchEngine(icon) =
    inherit DynamicSearchEngine()
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
    override this.Icon = icon
    override this.Activators = [| DefaultSearchEngineActivator(this) |]
    override this.BufferResults = false

    override this.Search(query, _ct, _) =
        query
        |> regex.Matches
        |> Seq.collect (
            tryParseUri
            >> Option.map (fun uri ->
                match uri.Host with
                | "localhost" ->
                    [| UrlSearchResult uri :> SearchResult
                       UrlSearchResult(Uri("https://localhost:8080"))
                       UrlSearchResult(Uri("https://localhost:5174")) |] // TODO: Allow the user to set custom values
                | _ -> [| UrlSearchResult uri |]
            )
            >> Option.defaultValue Array.empty
        ),
        Observable.Empty()

    override this.SearchResultSelected(searchResult) =
        match searchResult with
        | :? UrlSearchResult as sr ->
            ProcessStartInfo(
                FileName = sr.Uri.AbsoluteUri,
                UseShellExecute = true
            )
            |> Process.Start
            |> function null -> () | d -> d.Dispose()
        | _ -> ()

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| nameof UrlSearchEngine |]
    override this.LoadSearchEngine(_, _, _, _) = UrlSearchEngine(icon), null
