module Starter.ApplicationSearchEngine.Linux.Theme.IconLookup

open System
open System.Collections.Concurrent
open System.IO
open System.Threading.Tasks
open System.Collections.Generic

open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine.Linux.Theme

let private extensions = [| "svg"; "png"; "xpm" |]

type Database =
    { /// A dictionary where the keys are the name of the themes
      Themes: IDictionary<string, IconTheme>
      Hicolor: IconTheme array }

module Database =
    let private loadThemeForDirectory dir =
        let indexPath = Path.Combine(dir, "index.theme")

        if File.Exists indexPath then
            indexPath
            |> IconThemeParser.parseIndexTheme
            |> Some
        else
            None

    let buildIconLookupDb (themesDirectories: string array) : Database Task =
        task {
            let themes = ConcurrentDictionary<string, IconTheme>()

            do! themesDirectories
                |> Seq.collect (fun directory ->
                    directory
                    |> Directory.EnumerateDirectories
                    |> Seq.choose (fun dir ->
                        dir
                        |> loadThemeForDirectory
                        |> Option.map (Task.map (Option.iter (fun theme ->
                            themes.TryAdd(theme.Name, theme) |> ignore
                        )))
                    )
                )
                |> Task.WhenAll
                :> Task

            let hicolor =
                themesDirectories |> Array.choose (fun dir ->
                    let themeDir = Path.Combine(dir, "hicolor")
                    if Directory.Exists themeDir then
                        themeDir
                        |> IconThemeParser.parseFromDirectory
                        |> Option.ofResult
                    else
                        None
                )

            return { Themes = themes :> IDictionary<_, _>
                     Hicolor = hicolor }
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

let lookupIconInDatabase theme (iconName: string) size scale (db: Database) =
    let seenThemes = HashSet(10)

    let rec lookupTheme theme =
        if seenThemes.Contains theme then ValueNone else
        seenThemes.Add theme |> ignore

        match db.Themes.TryGetValue theme with
        | true, theme ->
            match lookupIconInTheme iconName size scale theme with
            | ValueSome i -> ValueSome i
            | ValueNone -> theme.ParentThemes |> Array.tryPickV lookupTheme
        | false, _ ->
            ValueNone

    orElse {
        return! lookupTheme theme
        return!
            db.Themes
            |> Seq.filter (fun kv -> seenThemes.Contains kv.Key |> not)
            |> Seq.tryPickV (_.Value >> lookupIconInTheme iconName size scale)
        return! db.Hicolor |> Array.tryPickV (lookupIconInTheme iconName size scale)
        return!
            extensions |> Array.tryPickV (fun ext ->
                Directory.EnumerateFiles("/usr/share/pixmaps", $"{iconName}.{ext}")
                |> Seq.tryHeadV
            )
    }
