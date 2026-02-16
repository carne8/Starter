module Starter.ApplicationSearchEngine.Windows.Config

open System
open System.IO
open System.Text.Json
open System.Text.Json.Nodes
open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Logger

[<Literal>]
let ConfigFilename = "applications-config.json"

module FolderConfiguration =
    let Empty =
        { Folders = Array.empty
          ExcludedFolders = Array.empty
          AllowDuplicates = false }

    let Default =
        { Folders =
            [| Environment.GetFolderPath(Environment.SpecialFolder.Programs)
               Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms) |]
          ExcludedFolders =
            [| Environment.GetFolderPath(Environment.SpecialFolder.Startup)
               Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) |]
          AllowDuplicates = false }

    let normalize folderConfig =
        { Folders =
            Array.append folderConfig.Folders Default.Folders
            |> Array.filter Directory.Exists
          ExcludedFolders =
            Array.append folderConfig.ExcludedFolders Default.ExcludedFolders
            |> Array.filter Directory.Exists
          AllowDuplicates = folderConfig.AllowDuplicates }

module private ValueOption =
    let inline ofPair (r: bool, v: 'a | null) =
        match r with
        | false -> ValueNone
        | true ->
            match v with
            | null -> ValueNone
            | x -> ValueSome x

module private JsonNode =
    let parseArray (node: JsonNode | null) =
        match node with
        | :? JsonArray as arr -> arr |> ValueSome
        | _ -> ValueNone

    let parseString (node: JsonNode | null) =
        match node with
        | :? JsonValue as value -> value.TryGetValue<string>() |> ValueOption.ofPair
        | _ -> ValueNone

    let parseBool (node: JsonNode | null) =
        match node with
        | :? JsonValue as value ->
            match value.GetValueKind() with
            | JsonValueKind.False -> ValueSome false
            | JsonValueKind.True -> ValueSome true
            | _ -> ValueNone
        | _ -> ValueNone

let private ensureFileExists (file: string) =
    match file |> Path.GetDirectoryName with
    | null -> logger.Error $"Failed to create config directory, invalid path: {file}"
    | fileDir when fileDir |> Directory.Exists |> not ->
        fileDir |> Directory.CreateDirectory |> ignore
    | _ -> ()

    if not <| File.Exists file then
        logger.Information "Config file does not exist. Creating it."
        let stream = File.CreateText file
        stream.Write "{}"
        stream.Flush()
        stream.Dispose()

let load (file: string) =
    voption {
        ensureFileExists file

        let json = File.ReadAllText file
        let! node =
            try JsonNode.Parse json |> ValueSome
            with e ->
                logger.Error(e, "Failed to open file for read")
                ValueNone

        let! allowDuplicates = node["allowDuplicates"] |> JsonNode.parseBool

        let! foldersJson = node["folders"] |> JsonNode.parseArray
        let folders =
            foldersJson
            |> Seq.choose (JsonNode.parseString >> Option.ofValueOption)
            |> Seq.toArray

        let excludedFoldersJson = node["excludedFolders"] |> JsonNode.parseArray
        let excludedFolders =
            match excludedFoldersJson with
            | ValueSome json ->
                json
                |> Seq.choose (JsonNode.parseString >> Option.ofValueOption)
                |> Seq.toArray
            | ValueNone -> Array.empty

        return { Folders = folders
                 ExcludedFolders = excludedFolders
                 AllowDuplicates = allowDuplicates }
    }
    |> ValueOption.defaultValue FolderConfiguration.Empty

let save (file: string) config =
    result {
        let json = JsonObject()

        json["allowDuplicates"] <- config.AllowDuplicates |> JsonValue.Create

        json["folders"] <-
            config.Folders
            |> Array.map (fun f -> f |> JsonValue.Create :> JsonNode | null)
            |> JsonArray

        json["excludedFolders"] <-
            config.ExcludedFolders
            |> Array.map (fun f -> f |> JsonValue.Create :> JsonNode | null)
            |> JsonArray

        use! file =
            try File.Open(file, FileMode.Create) |> Ok
            with e -> Error ("Failed to open file for write", e)

        use jsonWriter = new Utf8JsonWriter(file)
        json.WriteTo jsonWriter
    }
    |> Result.defaultWith (fun (err, exn) -> logger.Error(exn, $"Failed to save config: {err}"))
