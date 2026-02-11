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

let ensureConfigFileExists (filePath: string) =
    let fileDir = filePath |> Path.GetDirectoryName
    if fileDir |> Directory.Exists |> not then
        fileDir |> Directory.CreateDirectory |> ignore

    if filePath |> File.Exists |> not then
        logger.Information "Config file does not exist. Creating it."
        use file = File.Create filePath

        Config.defaultConfig
        |> Config.toConfigDto
        |> JsonSerializer.SerializeToUtf8Bytes
        |> file.Write

let loadConfig (filePath: string) =
    try
        ensureConfigFileExists filePath

        use stream = File.OpenRead filePath
        let config = JsonSerializer.Deserialize<ConfigDTO> stream
        config |> Config.ofConfigDto

    with e ->
        logger.Error(e, "Failed to load config")
        Config.defaultConfig |> saveConfig filePath
        Config.defaultConfig

let saveConfig (filePath: string) (config: Config) =
    try
        ensureConfigFileExists filePath

        let json =
            config
            |> Config.toConfigDto
            |> JsonSerializer.SerializeToUtf8Bytes

        File.WriteAllBytes(filePath, json)

    with e ->
        logger.Error(e, "Failed to save config")
        failwith "Failed to save config"
