namespace Starter.WorkspaceSearchEngine

open System.Collections.Generic
open Starter.WorkspaceSearchEngine.Logger

type Settings = { ShowIfNoActivator: Dictionary<string, bool> }

module Settings =
    open System.IO
    open System.Text.Json

    let defaultSettings = { ShowIfNoActivator = Dictionary() }

    [<Literal>]
    let private SettingsFilename = "settings.json"

    let getFilePath settingsDirectory = Path.Combine(settingsDirectory, SettingsFilename)

    let ensureConfigFileExists (filePath: string) =
        let fileDir = filePath |> Path.GetDirectoryName
        if fileDir |> Directory.Exists |> not then
            fileDir |> Directory.CreateDirectory |> ignore

        if filePath |> File.Exists |> not then
            logger.Information "Config file does not exist. Creating it."
            use file = File.Create filePath

            defaultSettings
            |> JsonSerializer.SerializeToUtf8Bytes
            |> file.Write

    let saveSettings (filePath: string) (settings: Settings) =
        try
            ensureConfigFileExists filePath
            let json = settings |> JsonSerializer.SerializeToUtf8Bytes
            File.WriteAllBytes(filePath, json)
        with e ->
            logger.Error(e, "Failed to save config")

    let loadSettings (filePath: string) =
        try
            ensureConfigFileExists filePath
            use stream = File.OpenRead filePath
            JsonSerializer.Deserialize<Settings> stream
        with
        | e ->
            logger.Error(e, "Failed to load config")
            defaultSettings |> saveSettings filePath
            defaultSettings
