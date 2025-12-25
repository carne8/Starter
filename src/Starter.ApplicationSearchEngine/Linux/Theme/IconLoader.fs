module Starter.ApplicationSearchEngine.Linux.IconLoader

open System.IO
open System.Threading.Tasks

open FsToolkit.ErrorHandling
open Starter.SearchEngine
open Starter.ApplicationSearchEngine.Logger
open Starter.ApplicationSearchEngine.Linux
open Starter.ApplicationSearchEngine.Linux.Theme
open Helpers

open Avalonia.Threading
open Avalonia.Svg.Skia
open Avalonia.Media.Imaging

module private StarterIconSource =
    let fromSvgSource svgSource =
        Dispatcher.UIThread.InvokeAsync(fun () ->
            try
                let svg = SvgImage(Source = svgSource)
                StarterIconSource(svg, svg)
            with exn ->
                logger.Warning $"Failed to load svg: {exn}"
                null
        )
        |> _.GetTask()

    let fromPng iconFile =
        try
            use stream = File.OpenRead iconFile

            Bitmap.DecodeToWidth(stream, 128)
            |> fun bmp -> StarterIconSource(bmp, bmp)
            |> ValueSome
        with exn ->
            logger.Warning $"Failed to load icon {iconFile}: {exn}"
            ValueNone

let private loadAppIconFile themes (iconName: string) =
    match iconName |> Path.IsPathFullyQualified with
    | true -> ValueSome iconName
    | false ->
        themes
        |> Array.tryPickV (IconLookup.lookupIconInDb iconName 128 1)
        |> ValueOption.orElseWith (fun () -> IconLookup.lookupIconInFallbackDirectories iconName 128 1)


let loadThemes () =
    ThemeDetection.getCurrentIconTheme()
    |> Task.bind IconLookup.buildIconLookupDb

let loadAppIcon themes (desktopEntry: XDGDesktopFileParser.DesktopEntry) =
    let iconFile =
        desktopEntry.IconName
        |> ValueOption.bind (loadAppIconFile themes)

    match iconFile with
    | ValueNone -> ValueTask.FromResult null
    | ValueSome file ->
        match Path.GetExtension file with
        | ".svg" ->
            file
            |> SvgSource.Load // This is taking time
            |> StarterIconSource.fromSvgSource
            |> ValueTask<StarterIconSource>
        | _ ->
            file
            |> StarterIconSource.fromPng
            |> ValueOption.defaultValue null
            |> ValueTask.FromResult