module Starter.ApplicationSearchEngine.Linux.IconLoader

open System
open System.IO
open System.Threading.Tasks

open FsToolkit.ErrorHandling
open Starter.SearchEngine
open Starter.ApplicationSearchEngine.Logger
open Starter.ApplicationSearchEngine.Linux
open Starter.ApplicationSearchEngine.Linux.Theme
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
                StarterIconSource.Empty
        )
        |> _.GetTask()

    let fromPng iconFile =
        try
            use stream = File.OpenRead iconFile
            let bmp = Bitmap.DecodeToWidth(stream, 128)
            StarterIconSource(bmp, bmp)
        with exn ->
            logger.Warning $"Failed to load icon {iconFile}: {exn}"
            StarterIconSource.Empty

let private loadSvgSource (path: string) =
    let svg = File.ReadAllText path
    let builder = Text.StringBuilder(svg.Length)

    let mutable lastCharWasDigit = false
    for i = 0 to svg.Length-1 do
        let c = svg[i]

        if lastCharWasDigit && c = '-' then
            builder.Append ' ' |> ignore

        builder.Append c |> ignore
        lastCharWasDigit <- Char.IsDigit c

    SvgSource.LoadFromSvg (builder.ToString())

type IconLoader(currentTheme: string, database: IconLookup.Database) = // TODO: Maybe make current theme dynamic ?
    let iconSize = 128
    let iconScale = 1

    let findAppIconFile (iconName: string) =
        match iconName |> Path.IsPathFullyQualified with
        | true -> ValueSome iconName
        | false -> IconLookup.lookupIconInDatabase currentTheme iconName iconSize iconScale database

    member _.LoadIcon (desktopEntry: XDGDesktopFileParser.DesktopEntry) =
        let iconFile =
            desktopEntry.IconName
            |> ValueOption.bind findAppIconFile
            |> ValueOption.bind (fun file ->
                match File.ResolveLinkTarget(file, true) with
                | null -> ValueSome file
                | f -> ValueSome f.FullName
            )

        match iconFile with
        | ValueNone ->
            logger.Debug $"Failed to find icon for {desktopEntry.Name}: {desktopEntry.IconName}"
            ValueTask.FromResult StarterIconSource.Empty
        | ValueSome file ->
            match Path.GetExtension file with
            | ".svg" ->
                file
                |> loadSvgSource // This is taking time
                |> StarterIconSource.fromSvgSource
                |> ValueTask<StarterIconSource>
            | _ ->
                file
                |> StarterIconSource.fromPng
                |> ValueTask.FromResult

    static member create () =
        task {
            let themeDirectories =
                let xdgDataDirs =
                    Environment.GetEnvironmentVariable "XDG_DATA_DIRS"
                    |> Option.ofObj
                    |> Option.defaultValue "/usr/local/share:/usr/share"

                [| // $HOME/.icons
                    Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.UserProfile, ".icons")

                    // $HOME/.local/share/icons
                    Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.UserProfile, ".local", "share", "icons")

                    // $XDG_DATA_DIRS/icons
                    for dataDir in xdgDataDirs.Split ':' do Path.Combine(dataDir, "icons") |]
                |> Array.filter Directory.Exists

            let! currentTheme = ThemeDetection.getCurrentIconTheme ()
            let! database = IconLookup.Database.buildIconLookupDb themeDirectories

            return IconLoader(currentTheme, database)
        }
