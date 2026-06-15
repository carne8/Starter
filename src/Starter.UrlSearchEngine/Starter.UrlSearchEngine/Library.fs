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

[<Struct>]
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

type UrlSearchEngine() =
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

    interface IDynamicSearchEngine with
        member this.Id = nameof UrlSearchEngine
        member this.Name = "Link opener"
        member this.ShortName = "Link"
        member this.Icon = icon
        member this.Activators = [| DefaultSearchEngineActivator(this) |]
        member this.ResultsPriority = ResultPriority.Fallback
        member this.BufferResults = false

        member this.Search(query, _ct, _) =
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

        member this.SearchResultSelected(searchResult) =
            match searchResult with
            | :? SearchResult as sr ->
                ProcessStartInfo(
                    FileName = sr.Uri.AbsoluteUri,
                    UseShellExecute = true
                )
                |> Process.Start
                |> function null -> () | d -> d.Dispose()
            | _ -> ()

        member this.add_Changed _ = ()
        member this.remove_Changed _ = ()

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| nameof UrlSearchEngine |]
    override this.LoadSearchEngine(_, _, _, _) = UrlSearchEngine(), null
