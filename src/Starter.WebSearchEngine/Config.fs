module rec Starter.WebSearchEngine.Config

open System.IO
open System.Text.Json
open Starter.WebSearchEngine.Logger

let private searchEngineMap =
    [ SearchEngineKind.Google
      SearchEngineKind.Qwant
      SearchEngineKind.DuckDuckGo
      SearchEngineKind.Bing
      SearchEngineKind.Ecosia ]
    |> List.map (fun se ->
        se |> SearchEngineKind.getName |> _.ToLowerInvariant(),
        se
    )
    |> dict

type ConfigDTO =
    { SearchEngine: string }

type Config =
    { SearchEngine: SearchEngineKind }
    static member defaultConfig = { SearchEngine = Google }
    static member ofConfigDto (config: ConfigDTO) = { SearchEngine = searchEngineMap[config.SearchEngine.ToLowerInvariant()] }
    static member toConfigDto (config: Config) : ConfigDTO = { SearchEngine = config.SearchEngine |> SearchEngineKind.getName }

[<Literal>]
let ConfigFilename = "web-search-config.json"

let loadConfig (filePath: string) =
    try
        use stream = File.OpenRead filePath
        let config = JsonSerializer.Deserialize<ConfigDTO> stream
        config |> Config.ofConfigDto

    with e ->
        logger.Warning(e, "Failed to load config")
        Config.defaultConfig |> saveConfig filePath
        Config.defaultConfig

let ensureFileExists (filePath: string) =
    let fileDir = filePath |> Path.GetDirectoryName
    if fileDir |> Directory.Exists |> not then
        fileDir |> Directory.CreateDirectory |> ignore

    if filePath |> File.Exists |> not then
        filePath |> File.Create |> ignore

let saveConfig (filePath: string) (config: Config) =
    try
        ensureFileExists filePath

        let json =
            config
            |> Config.toConfigDto
            |> JsonSerializer.SerializeToUtf8Bytes

        File.WriteAllBytes(filePath, json)

    with e ->
        logger.Warning(e, "Failed to save config")
        failwith "Failed to save config"
