[<RequireQualifiedAccess>]
module Starter.Features.Constants

open System
open System.IO

let ConfigDirectory =
    Environment.SpecialFolder.ApplicationData
    |> Environment.GetFolderPath
    |> fun appDataDir -> Path.Combine(appDataDir, "Starter")

let ConfigFile = Path.Combine(ConfigDirectory, "starter-config.json")
let ResultScoresFile = Path.Combine(ConfigDirectory, "result-scores.db")
let [<Literal>] ScoresMaxAging = 10_000

let ProcessDirectory =
    match Environment.ProcessPath with
    | null -> failwith "Failed to retrieve process path"
    | processPath ->
        processPath
        |> Path.GetDirectoryName
        |> function
            | null -> failwith "Failed to retrieve process path"
            | p -> p

let PluginsDirectory = Path.Combine(ProcessDirectory, "Plugins")
let PluginsSymlinkPath = Path.Combine(ConfigDirectory, "Plugins")

let LogFilePath = Path.Combine(ConfigDirectory, "Logs", "log.txt")
// let LogFilename () = $"logs-{DateTimeOffset.Now:``yyyy-MM-ddTHH-mm-ss``}.txt"

module Platform =
    module Windows =
        let StartupFile = "Starter.lnk"
