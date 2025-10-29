module Starter.ApplicationSearchEngine.Linux.IconLoader

open Avalonia.Media.Imaging
open Avalonia.Threading
open Avalonia.Svg.Skia
open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Logger
open Starter.SearchEngine
open System.IO
open System.Text.RegularExpressions

let private directoryIconSizeRegex = Regex(@"\/(\d+)x\d+(?:@\d)?\/", RegexOptions.Compiled)

module private StarterIconSource =
    let fromSvgFile svgPath =
        Dispatcher.UIThread.InvokeAsync(fun () ->
            SvgImage(Source = SvgSource.Load svgPath)
        )
        |> _.GetTask()
        |> Task.map (fun svg -> StarterIconSource(svg, svg))

    let fromBitmapFile bmp = StarterIconSource(bmp, bmp)

// TODO: Rebuild using -> https://specifications.freedesktop.org/icon-theme-spec/latest/

let private findIconFile (folderConfig: FolderConfiguration) iconName = // TODO: Take care of using the folderConfig
    match iconName |> File.Exists with
    | true -> Some iconName
    | false ->
        let files =
            seq {
                if Directory.Exists "/usr/share/icons" then Directory.EnumerateFiles("/usr/share/icons", $"{iconName}.*", SearchOption.AllDirectories)
                if Directory.Exists "/usr/share/pixmaps" then Directory.EnumerateFiles("/usr/share/pixmaps", $"{iconName}.*", SearchOption.AllDirectories)
                if Directory.Exists "/var/lib/flatpak/exports/share/icons/" then Directory.EnumerateFiles("/var/lib/flatpak/exports/share/icons/", $"{iconName}.*", SearchOption.AllDirectories)
            }
            |> Seq.concat

        let svgFile =
            files |> Seq.tryFind (fun path ->
                path |> Path.GetExtension = ".svg"
                && path |> Path.GetFileNameWithoutExtension = iconName
            )

        match svgFile with
        | Some file -> Some file
        | None ->
            files
            |> Seq.filter (Path.GetExtension >> (<>) ".svg")
            |> Seq.sortByDescending (fun path ->
                let match' = directoryIconSizeRegex.Match(path)
                if not match'.Success then 0 else int match'.Groups[1].Value
            )
            |> Seq.tryHead

let loadAppIcon folderConfig (iconName: string) =
    taskOption {
        let! iconFile = iconName |> findIconFile folderConfig

        match iconFile |> Path.GetExtension with
        | ".svg" -> return! iconFile |> StarterIconSource.fromSvgFile
        | _ -> return new Bitmap(iconFile) |> StarterIconSource.fromBitmapFile
    }
    |> Task.catch
    |> Task.map (function
        | Choice1Of2 opt -> opt
        | Choice2Of2 exn ->
            logger.Warning $"Failed to load icon {iconName}: {exn}"
            None
    )
