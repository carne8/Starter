module Starter.ApplicationSearchEngine.Linux.XDGDesktopFileParser

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.IO
open System.Text
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine.Logger
open Starter.SearchEngine

type private String with
    member inline this.TryIndexOf(s: string) =
        match this.IndexOf s with
        | -1 -> ValueNone
        | n -> ValueSome n

    member inline this.TryIndexOf(c: char) =
        match this.IndexOf c with
        | -1 -> ValueNone
        | n -> ValueSome n

module Seq =
    let inline choosev f =
        Seq.choose (f >> Option.ofValueOption)

/// Represents a raw desktop entry parsed from a .desktop file
[<Struct>]
type DesktopEntry =
    { Name: string
      Exec: string
      WorkingDirectory: string ValueOption
      IconName: string ValueOption
      AdditionalSearchKeywords: string array
      DesktopFilePath: string }

let private groupLinesByEntry (desktopFileLines: string array) =
    // Group lines by entry
    let entries = List<List<string>>()
    let mutable currentEntryIndex = ValueNone

    for line in desktopFileLines do
        if line.StartsWith "[Desktop Entry]" then
            entries.Add(List())
            currentEntryIndex <- ValueSome <| entries.Count - 1
        elif line.StartsWith "[" then
            currentEntryIndex <- ValueNone
        else
            match currentEntryIndex with
            | ValueNone -> ()
            | ValueSome currentEntryIndex ->
                entries[currentEntryIndex].Add line

    entries

let private parseKeyValuePair (line: string) =
    voption {
        let! equalIndex = line.TryIndexOf '='
        let keyWithLocalization = line[..equalIndex-1]
        let value = line[equalIndex+1..]

        // Separate key and localization
        let key, localization =
            match keyWithLocalization.TryIndexOf '[' with
            | ValueNone -> keyWithLocalization, ValueNone
            | ValueSome i -> keyWithLocalization[..i-1], ValueSome keyWithLocalization[i+1..]

        return struct {| Key = key
                         Localization = localization
                         Value = value |}
    }

/// Transform the lines of a desktop entry from .desktop file
/// into a DesktopEntry struct
let private parseDesktopEntryLines filePath (lines: string seq) =
    let keyValuePairs = lines |> Seq.choosev parseKeyValuePair

    let mutable shouldBeShown = true
    let mutable name = ValueNone
    let mutable iconName = ValueNone
    let mutable exec = ValueNone
    let mutable path = ValueNone
    let mutable additionalSearchStrings = List.empty

    let mutable enumerator = keyValuePairs.GetEnumerator()
    while shouldBeShown && enumerator.MoveNext() do
        let kv = enumerator.Current
        match kv.Key with
        | "Hidden"
        | "NoDisplay" when kv.Value.ToLowerInvariant() = "true" -> shouldBeShown <- false
        | "Name" -> name <- ValueSome kv.Value // TODO: Add name localization
        | "Icon" -> iconName <- ValueSome kv.Value
        | "Exec" -> exec <- ValueSome kv.Value
        | "Path" -> path <- ValueSome kv.Value
        | "GenericName"
        | "Keywords" ->
            additionalSearchStrings <-
                (kv.Value.Split ';' |> Array.toList) @ additionalSearchStrings
        | _ -> ()
        // TODO: | "OnlyShowIn" | "NotShowIn"
        // TODO: | "TryExec"
        // TODO: | "DBusActivatable"
        // TODO: | "PrefersNonDefaultGPU"

    match shouldBeShown, name, exec with
    | true, ValueSome name, ValueSome exec ->
        { Name = name
          Exec = exec
          IconName = iconName
          WorkingDirectory = path
          AdditionalSearchKeywords =
              match additionalSearchStrings with
              | [] -> null
              | l -> l |> List.toArray
          DesktopFilePath = filePath }
        |> ValueSome
    | _ -> ValueNone

/// Make the Exec value found in desktop entry an executable line
/// to run when the app is selected.
let private parseExec (entry: DesktopEntry) =
    // Check presence of deprecated field code
    let deprecatedFieldCodes = [ "%d"; "%D"; "%n"; "%N"; "%v"; "%m" ]
    let containsDeprecatedFieldCode =
        entry.Exec.Split ' '
        |> Array.exists (fun frag -> deprecatedFieldCodes |> List.contains frag)

    match containsDeprecatedFieldCode with
    | true ->
        logger.Warning $"The Exec in {entry.DesktopFilePath} contains deprecated field code"
        ValueNone
    | false ->
        let exec = StringBuilder(entry.Exec)

        match entry.Exec.TryIndexOf "%i" with
        | ValueNone -> ()
        | ValueSome i ->
            exec.Remove(i, 2) |> ignore
            entry.IconName
            |> ValueOption.map (sprintf "--icon %s")
            |> ValueOption.defaultValue String.Empty
            |> fun s -> exec.Insert(i, s)
            |> ignore

        match entry.Exec.TryIndexOf "%c" with
        | ValueNone -> ()
        | ValueSome i ->
            exec.Remove(i, 2) |> ignore
            exec.Insert(i, entry.Name) |> ignore

        match entry.Exec.TryIndexOf "%k" with
        | ValueNone -> ()
        | ValueSome i ->
            exec.Remove(i, 2) |> ignore
            exec.Insert(i, entry.DesktopFilePath) |> ignore

        exec.Replace("%f", "") |> ignore
        exec.Replace("%F", "") |> ignore
        exec.Replace("%u", "") |> ignore
        exec.Replace("%U", "") |> ignore

        exec.ToString() |> ValueSome

let loadDesktopEntries desktopFile =
    task {
        let! lines = desktopFile |> File.ReadAllLinesAsync
        let entries =
            lines
            |> groupLinesByEntry
            |> Seq.choosev (parseDesktopEntryLines desktopFile)
            |> Seq.toArray

        let bag = ConcurrentBag<ISearchResult>()

        do! Parallel.ForEachAsync(
            entries,
            Func<DesktopEntry, _, _>(fun entry ct ->
                task {
                    let! icon =
                        match entry.IconName with
                        | ValueNone -> null
                        | ValueSome iconName ->
                            iconName
                            |> IconLoader.loadAppIcon
                            |> TaskOption.defaultValue null

                    { Id = $"application:{desktopFile}:{entry.Name}"
                      Name = entry.Name
                      Icon = icon
                      Description = "Applications" // TODO: I18n or use Generic Name
                      Keywords = entry.AdditionalSearchKeywords
                      Exec = entry |> parseExec
                      WorkingDirectory = entry.WorkingDirectory }
                    |> bag.Add
                }
                |> ValueTask
            )
        )

        return bag :> _ seq
    }
