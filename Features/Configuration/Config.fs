module Starter.Features.Config

open System
open System.IO
open FsToolkit.ErrorHandling
open Thoth.Json.Net

[<RequireQualifiedAccess>]
type private FileNames =
    static member ConfigFile = "Starter-config.json"

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
    { LaunchAtStartup: bool
      Background: Background }

    static member Default =
        { LaunchAtStartup = false
          Background = Background.Mica }

    static member encoder config =
        Encode.object [
            if config.LaunchAtStartup <> Configuration.Default.LaunchAtStartup then
                "launchAtStartup", Encode.bool config.LaunchAtStartup

            if config.Background <> Configuration.Default.Background then
                "background", Background.encoder config.Background
        ]

    static member decoder: Decoder<Configuration> =
        Decode.object (fun get ->
            { LaunchAtStartup =
                get.Optional.Field "launchAtStartup" Decode.bool
                |> Option.defaultValue false
              Background =
                get.Optional.Field "background" Background.decoder
                |> Option.defaultValue Background.Mica }
        )

let getConfig () =
    result {
        // Retrieve config path
        let! procPath =
            Environment.ProcessPath
            |> Result.requireNotNull ()
            |> Result.map Path.GetDirectoryName

        let configPath = Path.Combine(procPath, FileNames.ConfigFile)

        match File.Exists configPath with
        | false -> return Configuration.Default
        | true ->
            // Load config
            let json = configPath |> File.ReadAllText
            let config = Decode.fromString Configuration.decoder json

            match config with
            | Ok config -> return config
            | Error err ->
                printfn "Failed to decode configuration: %A" err
                return Configuration.Default
    }

let saveConfig (config: Configuration) =
    taskResult {
        // Retrieve config path
        let! procPath =
            Environment.ProcessPath
            |> Result.requireNotNull ()
            |> Result.map Path.GetDirectoryName

        let configPath = Path.Combine(procPath, FileNames.ConfigFile)

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
