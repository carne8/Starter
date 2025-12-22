module Starter.ApplicationSearchEngine.Linux.IconLoader

open Starter.SearchEngine
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Linux.Theme

open System.IO
open FsToolkit.ErrorHandling
open Helpers

open Avalonia.Threading
open Avalonia.Svg.Skia
open Avalonia.Media.Imaging

module private StarterIconSource =
    let fromSvgFile svgPath =
        Dispatcher.UIThread.InvokeAsync(fun () ->
            SvgImage(Source = SvgSource.Load svgPath)
        )
        |> _.GetTask()
        |> Task.map (fun svg -> StarterIconSource(svg, svg))

    let fromBitmapFile bmp = StarterIconSource(bmp, bmp)

let private themeTrees =
    ThemeDetection.getCurrentIconTheme()
    |> Task.bind IconLookup.buildIconLookupDb


let loadAppIcon (iconName: string) =
    taskOption {
        let! themeTrees = themeTrees
        let! iconFile =
            match iconName |> Path.IsPathFullyQualified with
            | true -> Some iconName
            | false ->
                themeTrees
                |> Array.tryPickV (IconLookup.lookupIconInDb iconName 128 1)
                |> ValueOption.orElseWith (fun () -> IconLookup.lookupIconInFallbackDirectories iconName 128 1)
                |> Option.ofValueOption

        match iconFile |> Path.GetExtension with
        | ".svg" -> return! iconFile |> StarterIconSource.fromSvgFile
        | _ -> return new Bitmap(iconFile) |> StarterIconSource.fromBitmapFile
    }
    |> Task.catch (fun exn ->
        Logger.logger.Warning $"Failed to load icon {iconName}: {exn}"
        None
    )
