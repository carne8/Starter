module Starter.ApplicationSearchEngine.Linux.XDGDesktopFileParser

open System
open System.IO
open System.Text
open System.Collections.Generic
open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine.Logger

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

    use enumerator = keyValuePairs.GetEnumerator()
    while shouldBeShown && enumerator.MoveNext() do
        let kv = enumerator.Current
        match kv.Key with
        | "Hidden"
        | "NoDisplay" when kv.Value.ToLowerInvariant() = "true" -> shouldBeShown <- false
        | "Name" when kv.Localization.IsNone -> name <- ValueSome kv.Value // TODO: Add name localization
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

/// Make the Exec value found in desktop valid arguments for a executable line
let parseArguments (entry: DesktopEntry) =
    let fragments = entry.Exec.Split ' '
    match fragments with
    | [| |] | [| _ |] -> ValueNone
    | fragments ->
        // Check presence of deprecated field code
        let deprecatedFieldCodes = [| "%d"; "%D"; "%n"; "%N"; "%v"; "%m" |]
        let containsDeprecatedFieldCode =
            fragments |> Array.exists (fun frag -> deprecatedFieldCodes |> Array.contains frag)

        match containsDeprecatedFieldCode with
        | true ->
            logger.Warning $"The Exec in {entry.DesktopFilePath} contains deprecated field code"
            ValueNone
        | false ->
            let arguments = StringBuilder entry.Exec.Length

            for i = 1 to fragments.Length - 1 do
                match fragments[i] with
                | "%i" ->
                    entry.IconName
                    |> ValueOption.map (sprintf " --icon %s")
                    |> ValueOption.defaultValue String.Empty
                    |> arguments.Append
                    |> ignore

                | "%c" ->
                    arguments.Append ' ' |> ignore
                    arguments.Append entry.Name |> ignore // TODO: Add translation

                | "%k" ->
                    arguments.Append ' ' |> ignore
                    arguments.Append entry.DesktopFilePath |> ignore

                | "%f"
                | "%F"
                | "%u"
                | "%U" -> ()
                | other ->
                    if i <> 1 then arguments.Append ' ' |> ignore
                    arguments.Append other |> ignore

            match arguments.ToString() with
            | "" -> ValueNone
            | s -> ValueSome s

let loadDesktopEntries desktopFile =
    desktopFile
    |> File.ReadAllLinesAsync
    |> Task.map (fun lines ->
        lines
        |> groupLinesByEntry
        |> Seq.choosev (parseDesktopEntryLines desktopFile)
    )