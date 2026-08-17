module Starter.ApplicationSearchEngine.Linux.XDGDesktopFileParser

open System
open System.IO
open System.Text
open System.Collections.Generic
open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine.Logger
open Starter.ApplicationSearchEngine.Linux.Localization

/// Represents a raw desktop entry parsed from a .desktop file
[<Struct>]
type DesktopEntry =
    { Name: string
      Comment: string voption
      Exec: string
      WorkingDirectory: string voption
      IconName: string voption
      AdditionalSearchKeywords: string array | null
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

        return KeyValuePair(keyWithLocalization, value)
    }

/// Transform the lines of a desktop entry from .desktop file
/// into a DesktopEntry struct
let private parseDesktopEntryLines (localeLookupKeys: string array) filePath (lines: string seq) =
    let keyValuePairs =
        lines
        |> Seq.choosev parseKeyValuePair
        |> Dictionary

    let tryFindKey key =
        localeLookupKeys
        |> Array.tryPick (fun locale ->
            $"{key}[{locale}]"
            |> keyValuePairs.TryGetValue
            |> Option.ofPair
        )
        |> Option.orElseWith (fun () ->
            key
            |> keyValuePairs.TryGetValue
            |> Option.ofPair
        )
        |> Option.toValueOption

    let shouldBeShown =
        match tryFindKey "NoDisplay" with
        | ValueSome v when v.ToLowerInvariant() = "true" -> false
        | _ -> true

    let name = tryFindKey "Name"
    let comment = tryFindKey "Comment"
    let iconName = tryFindKey "Icon"
    let exec = tryFindKey "Exec"
    let path = tryFindKey "Path"
    let additionalSearchStrings = tryFindKey "Keywords"  |> ValueOption.map _.Split(';')

    // TODO: | "OnlyShowIn" | "NotShowIn"
    // TODO: | "TryExec"
    // TODO: | "DBusActivatable"
    // TODO: | "PrefersNonDefaultGPU"

    match shouldBeShown, name, exec with
    | true, ValueSome name, ValueSome exec ->
        { Name = name
          Comment = comment
          Exec = exec
          IconName = iconName
          WorkingDirectory = path
          AdditionalSearchKeywords =
            match additionalSearchStrings with
            | ValueNone
            | ValueSome [||] -> null
            | ValueSome arr -> arr
          DesktopFilePath = filePath }
        |> ValueSome
    | _ -> ValueNone

/// Make the Exec value found in desktop valid arguments for a executable line
let parseExec (entry: DesktopEntry) =
    let fragments = entry.Exec.Split ' '

    // Check presence of deprecated field code
    let deprecatedFieldCodes = [| "%d"; "%D"; "%n"; "%N"; "%v"; "%m" |]
    let containsDeprecatedFieldCode =
        fragments |> Array.exists (fun frag -> deprecatedFieldCodes |> Array.contains frag)

    match containsDeprecatedFieldCode with
    | true ->
        logger.Warning $"The Exec in {entry.DesktopFilePath} contains deprecated field code"
        ValueNone
    | false ->
        let exec = StringBuilder entry.Exec.Length

        for i = 0 to fragments.Length - 1 do
            match fragments[i] with
            | frag when i = 0 -> exec.Append frag |> ignore
            | "%i" ->
                entry.IconName
                |> ValueOption.map (sprintf " --icon %s")
                |> ValueOption.defaultValue String.Empty
                |> exec.Append
                |> ignore

            | "%c" ->
                exec.Append ' ' |> ignore
                exec.Append entry.Name |> ignore // TODO: Add translation

            | "%k" ->
                exec.Append ' ' |> ignore
                exec.Append entry.DesktopFilePath |> ignore

            | "%f"
            | "%F"
            | "%u"
            | "%U" -> ()
            | other ->
                exec.Append ' ' |> ignore
                exec.Append other |> ignore

        match exec.Length with
        | 0 -> ValueNone
        | _ -> ValueSome <| exec.ToString()

let loadDesktopEntries desktopFile =
    let localeLookupKeys = Localization.getLocaleLookupKeys ()

    desktopFile
    |> File.ReadAllLinesAsync
    |> Task.map (fun lines ->
        lines
        |> groupLinesByEntry
        |> Seq.choosev (parseDesktopEntryLines localeLookupKeys desktopFile)
    )
