module Starter.ApplicationSearchEngine.Linux.Theme.IconLookup

open System
open System.IO
open System.Threading.Tasks
open System.Collections.Generic

open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine.Linux.Theme

let private extensions = [| "svg"; "png"; "xpm" |]

/// Directories to search for icons
let private iconDirectories =
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

let private fallbackThemes =
    iconDirectories
    |> Array.choose (fun dir ->
        let themePath = Path.Combine(dir, "hicolor")
        match Directory.Exists themePath, File.Exists(Path.Combine(themePath, "index.theme")) with // Prevent searching multiple times in a theme
        | true, false ->
            themePath
            |> IconThemeParser.parseFromDirectory
            |> Some
        | _ -> None
    )


fallbackThemes
|> Array.map (fun x -> x.Name, x.ThemePath)
|> sprintf "%A"
|> Starter.ApplicationSearchEngine.Logger.logger.Debug

/// Returns a dictionary per theme matching the name provided.
/// Each dictionary contains the theme and its parents.
let buildIconLookupDb (iconThemeName: string) : struct (string * Dictionary<string, IconTheme>) array Task =
    task {
        let! themesOnSystem =
            iconDirectories
            |> Array.collect (fun directory ->
                directory
                |> Directory.EnumerateDirectories
                |> Seq.choose (fun themeFolder ->
                    let indexPath = Path.Combine(themeFolder, "index.theme")

                    match File.Exists indexPath with
                    | true -> Some indexPath
                    | false -> None
                )
                |> Seq.toArray
            )
            |> Array.map IconThemeParser.parseIndexTheme
            |> Task.WhenAll
            |> Task.map (Array.choose id)

        let themes = themesOnSystem |> Array.filter (fun theme -> theme.Name = iconThemeName)

        return themes |> Array.map (fun theme ->
            let d =
                { new IEqualityComparer<string> with
                    member _.Equals(x, y) = x.Equals(y, StringComparison.InvariantCultureIgnoreCase)
                    member _.GetHashCode s = s.GetHashCode StringComparison.InvariantCultureIgnoreCase }
                |> Dictionary
            d.Add(theme.Name, theme)

            let rec addParents theme =
                theme.ParentThemes |> Array.iter (fun parent ->
                    themesOnSystem
                    |> Array.tryFind (fun theme -> theme.Name.Equals(parent, StringComparison.InvariantCultureIgnoreCase))
                    |> Option.iter (fun theme ->
                        d.TryAdd(theme.Name, theme) |> ignore
                        addParents theme
                    )
                )

            addParents theme
            struct (theme.Name, d)
        )
    }

/// Look up icon in a specific theme
let private lookupIconInTheme (iconName: string) size scale theme =
    let inline fileExists directory iconName extension =
        let iconPath = Path.Combine(directory, $"{iconName}.{extension}")
        match File.Exists iconPath with
        | true -> Some iconPath
        | false -> None

    // First pass: exact size match
    let exactMatch = theme.Directories |> Array.tryPick (fun dir ->
        match dir |> IconThemeDirectory.matchesSize size scale with
        | false -> None
        | true -> extensions |> Array.tryPick (fileExists dir.Path iconName)
    )

    match exactMatch with
    | Some path -> ValueSome path
    | None ->
        // Second pass: closest size match
        theme.Directories
        |> Array.fold
            (fun struct (minDistance, closestPath) dir ->
                let distance = dir |> IconThemeDirectory.getSizeDistance size scale

                match distance < minDistance with
                | false -> struct (minDistance, closestPath)
                | true ->
                    extensions
                    |> Array.tryPick (fileExists dir.Path iconName)
                    |> function
                        | Some path -> struct (distance, ValueSome path)
                        | None -> struct (minDistance, closestPath)
            )
            struct (Int32.MaxValue, ValueNone)
        |> fun struct (_, closestPath) -> closestPath

let lookupIconInDb (iconName: string) size scale db =
    let struct (mainTheme, themes: Dictionary<string, IconTheme>) = db

    orElse {
        return! themes[mainTheme] |> lookupIconInTheme iconName size scale
        return! themes[mainTheme].ParentThemes |> Array.tryPickV (fun parentTheme ->
            themes[parentTheme.ToLowerInvariant()] |> lookupIconInTheme iconName size scale
        )
    }

let lookupIconInFallbackDirectories (iconName: string) size scale =
    orElse {
        return! fallbackThemes |> Array.tryPickV (lookupIconInTheme iconName size scale)
        return! extensions |> Array.tryPickV (fun ext ->
            Directory.EnumerateFiles("/usr/share/pixmaps", $"{iconName}.{ext}")
            |> Seq.tryHeadV
        )
    }