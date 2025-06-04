[<RequireQualifiedAccess>]
module Starter.Features.Constants

open System
open System.IO

let ProcessPath =
    match Environment.ProcessPath with
    | null -> failwith "Process path undefined"
    | path -> path

let ProcessDirectory =
    match ProcessPath |> Path.GetDirectoryName with
    | null -> failwith "Process directory path undefined"
    | path -> path

let ConfigFile = Path.Combine(ProcessDirectory, "starter-config.json")
let ResultScoresFile = Path.Combine(ProcessDirectory, "result-scores.db")
let [<Literal>] ScoresMaxAging = 10_000

module Platform =
    module Windows =
        let StartupFile = "Starter.lnk"
