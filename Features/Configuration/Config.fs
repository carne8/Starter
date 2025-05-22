module Starter.Features.Config

open System.IO
open FsToolkit.ErrorHandling
open Thoth.Json.Net

[<RequireQualifiedAccess>]
type Background =
    | Acrylic
    | Mica
    | None

    static member encoder background =
        match background with
        | Acrylic -> "acrylic" |> Encode.string
        | Mica -> "mica" |> Encode.string
        | None -> "none" |> Encode.string

    static member decoder: Decoder<Background> =
        Decode.string
        |> Decode.andThen (function
            | "acrylic" -> Acrylic |> Decode.succeed
            | "mica" -> Mica |> Decode.succeed
            | "none" -> None |> Decode.succeed
            | other -> Decode.fail $"{other} is not a valid background value."
        )

type Configuration =
    { Background: Background
      SearchEnginePrefixes: Map<string, string> }

    static member Default =
        { Background = Background.Mica
          SearchEnginePrefixes = Map.empty }

    static member encoder config =
        Encode.object [
            if config.Background <> Configuration.Default.Background then
                "background", Background.encoder config.Background

            "searchEnginePrefixes",
            config.SearchEnginePrefixes
            |> Map.map (fun _ v -> v |> Encode.string)
            |> Encode.dict
        ]

    static member decoder: Decoder<Configuration> =
        Decode.object (fun get ->
            { Background =
                get.Optional.Field "background" Background.decoder
                |> Option.defaultValue Background.Mica
              SearchEnginePrefixes =
                Decode.dict Decode.string
                |> get.Optional.Field "searchEnginePrefixes"
                |> Option.defaultValue Map.empty }
        )

// Read the config from the config file or return the default config
// May return Error only if it failed to decode the config file
let getConfig configPath =
    result {
        match File.Exists configPath with
        | false -> return Configuration.Default
        | true ->
            // Load config
            let json = configPath |> File.ReadAllText
            return! Decode.fromString Configuration.decoder json
    }

let saveConfig configPath (config: Configuration) =
    taskResult {
        // Encode config
        let json =
            config
            |> Configuration.encoder
            |> Encode.toString 2

        // Save config
        if not <| File.Exists configPath then
            File.Create configPath |> ignore

        do! File.WriteAllTextAsync(configPath, json)
    }
