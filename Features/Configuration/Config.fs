module Starter.Features.Config

open System
open System.IO
open System.Text.Json
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
type private FileNames =
    static member ConfigFile = "Starter-config.json"

// TODO: Add aot compatible serialization
type Configuration =
    { LaunchAtStartup: bool }
    static member Default = { LaunchAtStartup = false }

let jsonOptions =
    JsonSerializerOptions(
        JsonSerializerDefaults.Web,
        WriteIndented = true
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
            use stream = configPath |> File.OpenRead
            let config = JsonSerializer.Deserialize<Configuration>(stream, jsonOptions)

            match config with
            | null -> return Configuration.Default
            | config -> return config
    }

let saveConfig (config: Configuration) =
    taskResult {
        // Retrieve config path
        let! procPath =
            Environment.ProcessPath
            |> Result.requireNotNull ()
            |> Result.map Path.GetDirectoryName

        let configPath = Path.Combine(procPath, FileNames.ConfigFile)

        // Save config
        use file = File.Open(configPath, FileMode.OpenOrCreate, FileAccess.Write)
        do! JsonSerializer.SerializeAsync(file, config, jsonOptions)
    }
