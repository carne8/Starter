module Starter.Config

open System
open System.IO
open FsToolkit.ErrorHandling
open FSharp.Configuration

[<RequireQualifiedAccess>]
type private FileNames =
    static member ConfigFile = "Starter-config.yml"

[<Literal>]
let private configSchema = __SOURCE_DIRECTORY__ + "/config.yml"
type Configuration = YamlConfig<configSchema>

[<RequireQualifiedAccess>]
type LoadConfigError =
    | CannotRetrieveProcessPath
    | ConfigFileNotFound

let getConfig () =
    result {
        // Retrieve config path
        let! procPath =
            Environment.ProcessPath
            |> Result.requireNotNull LoadConfigError.CannotRetrieveProcessPath
            |> Result.map Path.GetDirectoryName
        let configPath = Path.Combine(procPath, FileNames.ConfigFile)

        do! File.Exists configPath |> Result.requireTrue LoadConfigError.ConfigFileNotFound

        // Load config
        let rawConfig = configPath |> File.ReadAllText
        let config = Configuration()
        config.LoadText rawConfig

        return config
    }

[<RequireQualifiedAccess>]
type SaveConfigError =
    | CannotRetrieveProcessPath

let saveConfig (config: Configuration) =
    result {
        // Retrieve config path
        let! procPath =
            Environment.ProcessPath
            |> Result.requireNotNull SaveConfigError.CannotRetrieveProcessPath
            |> Result.map Path.GetDirectoryName
        let configPath = Path.Combine(procPath, FileNames.ConfigFile)

        // Save config
        config.Save configPath
    }
