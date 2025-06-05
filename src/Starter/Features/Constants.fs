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

module Platform =
    module Windows =
        let StartupFile = "Starter.lnk"
