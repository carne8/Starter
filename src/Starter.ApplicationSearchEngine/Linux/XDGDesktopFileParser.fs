module Starter.ApplicationSearchEngine.Linux.XDGDesktopFileParser

open System
open System.Collections.Concurrent
open System.IO
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Starter.SearchEngine

type private String with
    member inline this.TryIndexOf(s: string) =
        match this.IndexOf(s) with
        | -1 -> ValueNone
        | n -> ValueSome n

    member inline this.TryIndexOf(c: char) =
        match this.IndexOf(c) with
        | -1 -> ValueNone
        | n -> ValueSome n

[<Struct>]
type DesktopEntry =
    { Name: string
      Exec: string
      Icon: string
      Keywords: string array }

/// Find the desktop entries in a .desktop file
let private findDesktopEntries desktopFile =
    task {
        let! lines = desktopFile |> File.ReadAllLinesAsync
        let entries = ResizeArray<_ ResizeArray>()
        let mutable currentEntryIndex = ValueNone

        for line in lines do
            if line.StartsWith("[Desktop Entry]") then
                entries.Add(ResizeArray())
                currentEntryIndex <- ValueSome <| entries.Count - 1
            elif line.StartsWith("[") then
                currentEntryIndex <- ValueNone

            match currentEntryIndex with
            | ValueNone -> ()
            | ValueSome currentEntryIndex ->
                entries[currentEntryIndex].Add line

        return entries |> Seq.map Seq.toArray |> Seq.toArray
    }

let private ExecKeyParameters = [| "%f"; "%F"; "%u"; "%U"; "%d"; "%D"; "%n"; "%N"; "%i"; "%c"; "%k"; "%v"; "%m" |]
/// Get needed info from a desktop entry
let private parseDesktopEntry (desktopEntry: string array) =
    let mutable appName = ValueNone
    let mutable appExec = ValueNone
    let mutable appIcon = ValueNone
    let mutable appKeywords = ValueNone
    let mutable noDisplay = false

    // Find keys, key variants and values: key[variant]=value
    desktopEntry
    |> Array.choose (fun line ->
        option {
            let mutable key = ValueNone
            let variant =
                line.TryIndexOf '[' |> ValueOption.bind (fun start ->
                    key <- ValueSome line[..start-1]
                    line.TryIndexOf "]"
                    |> ValueOption.map (fun end' -> line[start+1..end'-1])
                )

            if variant.IsSome then do! None // TODO: I18n

            let! equalPos = line.TryIndexOf "="

            match key with
            | ValueNone -> key <- ValueSome line[..equalPos-1]
            | _ -> ()

            let! key = key

            return struct {| Key = key
                             Variant = variant
                             Value = line[equalPos+1..] |}
        }
    )
    |> Array.iter (fun line ->
        if appName.IsNone && line.Key = "Name" then appName <- ValueSome line.Value
        if appExec.IsNone && line.Key = "Exec" then appExec <- ValueSome line.Value
        if appIcon.IsNone && line.Key = "Icon" then appIcon <- ValueSome line.Value
        if appKeywords.IsNone && line.Key = "Keywords" then
            appKeywords <-
                line.Value.Split(
                    ';',
                    StringSplitOptions.TrimEntries
                    ||| StringSplitOptions.RemoveEmptyEntries
                ) |> ValueSome
        if line.Key = "NoDisplay" then noDisplay <- true
    )

    match noDisplay with
    | true -> None
    | false ->
        match appName, appExec, appIcon with
        | ValueSome name, ValueSome exec, ValueSome icon ->
            let exec =
                ExecKeyParameters |> Array.fold
                    (fun (exec: string) param -> exec.Replace(param, String.Empty))
                    exec

            { Name = name
              Exec = exec
              Icon = icon
              Keywords = appKeywords |> ValueOption.defaultValue Array.empty }
            |> Some
        | _ -> None

let loadDesktopEntries desktopFile =
    task {
        let! entries = desktopFile |> findDesktopEntries
        let appInfo = entries |> Array.Parallel.choose parseDesktopEntry
        let bag = ConcurrentBag<ISearchResult>()

        do! Parallel.ForEachAsync(
            appInfo,
            Func<DesktopEntry, _, _>(fun appInfo ct ->
                task {
                    let! icon =
                        appInfo.Icon
                        |> IconLoader.loadAppIcon
                        |> TaskOption.defaultValue null

                    { Id = $"application:{desktopFile}:{appInfo.Name}"
                      Name = appInfo.Name
                      Icon = icon
                      Description = "Applications" // TODO: I18n
                      Keywords = appInfo.Keywords
                      Exec = appInfo.Exec }
                    |> bag.Add
                }
                |> ValueTask
            )
        )

        return bag :> _ seq
    }
