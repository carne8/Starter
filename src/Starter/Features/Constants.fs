[<RequireQualifiedAccess>]
module Starter.Features.Constants

open System
open System.IO

// --- Config ---
let ConfigDirectory =
    Environment.SpecialFolder.ApplicationData
    |> Environment.GetFolderPath
    |> fun appDataDir -> Path.Combine(appDataDir, "Starter")

let ConfigFile = Path.Combine(ConfigDirectory, "starter-config.json")
let ResultScoresFile = Path.Combine(ConfigDirectory, "result-scores.db")
let PluginConfigDirectory pluginName = Path.Combine(ConfigDirectory, pluginName)
let [<Literal>] ScoresMaxAging = 10_000

// --- Exe ---
let ProcessExecutableFile =
    match Environment.ProcessPath with
    | null -> failwith "Failed to retrieve process path"
    | processPath -> processPath

// --- Plugins ---
let ProcessDirectory =
    ProcessExecutableFile
    |> Path.GetDirectoryName
    |> function
        | null -> failwith "Failed to retrieve process path"
        | p -> p

let PluginsDirectory = Path.Combine(ProcessDirectory, "Plugins")
let PluginsSymlinkPath = Path.Combine(ConfigDirectory, "Plugins")

// --- Logs ---
let LogFilePath = Path.Combine(ConfigDirectory, "Logs", "log.txt")

module Platform =
    module Windows =
        let StartupFile = "Starter.lnk"
    module Linux =
        let StartupFile = "Starter.desktop"
