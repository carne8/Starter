namespace Starter.Features.Config

open System
open System.IO

open Starter.Features
open Starter.Features.Logging

open Avalonia.Input
open FsToolkit.ErrorHandling
open Thoth.Json.Net

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

    static member decoder: Decoder<_> =
        Decode.string |> Decode.andThen (function
            | "acrylic" -> Acrylic |> Decode.succeed
            | "mica" -> Mica |> Decode.succeed
            | "none" -> None |> Decode.succeed
            | other -> Decode.fail $"{other} is not a valid background value."
        )

[<RequireQualifiedAccess>]
type Antialiasing =
    | Alias
    | Grayscale
    | Subpixel
    | PlatformDefault

    static member encoder background =
        match background with
        | Alias -> "alias" |> Encode.string
        | Grayscale -> "grayscale" |> Encode.string
        | Subpixel -> "subpixel" |> Encode.string
        | PlatformDefault -> "platform-default" |> Encode.string

    static member decoder: Decoder<_> =
        Decode.string |> Decode.andThen (function
            | "alias" -> Alias |> Decode.succeed
            | "grayscale" -> Grayscale |> Decode.succeed
            | "subpixel" -> Subpixel |> Decode.succeed
            | "platform-default" -> PlatformDefault |> Decode.succeed
            | other -> Decode.fail $"{other} is not a valid antialiasing value."
        )

module Key =
    let encode = Key.GetName >> Encode.string
    let decode =
        Decode.string |> Decode.andThen (fun keyName ->
            match Key.TryParse keyName with
            | true, key -> key |> Decode.succeed
            | false, _ -> Decode.fail $"{keyName} is not a valid key name."
        )

[<Struct>]
type KeyboardShortcut =
    { Modifiers: Key array
      Key: Key }

    static member encode shortcut =
        Encode.object [
            "modifiers",
            shortcut.Modifiers
            |> Array.map Key.encode
            |> Encode.array

            "key", shortcut.Key |> Key.encode
        ]

    static member decode =
        Decode.object (fun get ->
            { Modifiers = get.Required.Field "modifiers" (Decode.array Key.decode)
              Key = get.Required.Field "key" Key.decode }
        )

type Configuration =
    { KeyboardShortcut: KeyboardShortcut
      Background: Background
      ZoomedMode: bool
      ActivatorPrefixes: Map<string, string>
      Antialiasing: Antialiasing }

    member this.WithBackground newValue = { this with Background = newValue }
    member this.WithZoomedMode newValue = { this with ZoomedMode = newValue }
    member this.WithAntialiasing newValue = { this with Antialiasing = newValue }
    member this.WithKeyboardShortcut newValue = { this with KeyboardShortcut = newValue }
    member this.WithActivatorPrefixes newValue = { this with ActivatorPrefixes = newValue }

    static member Default =
        { KeyboardShortcut = { Modifiers = [| Key.LeftAlt |]; Key = Key.Space }
          Background = Background.Mica
          ZoomedMode = false
          ActivatorPrefixes = Map.empty
          Antialiasing = Antialiasing.Grayscale }

    static member encoder config =
        Encode.object [
            "keyboardShortcut",
            KeyboardShortcut.encode config.KeyboardShortcut

            if config.Background <> Configuration.Default.Background then
                "background", Background.encoder config.Background

            "searchEnginePrefixes",
            config.ActivatorPrefixes
            |> Map.map (fun _ -> Encode.string)
            |> Encode.dict

            "zoomedMode", config.ZoomedMode |> Encode.bool

            "antialiasing", config.Antialiasing |> Antialiasing.encoder
        ]

    static member decoder: Decoder<Configuration> =
        Decode.object (fun get ->
            { KeyboardShortcut =
                get.Optional.Field "keyboardShortcut" KeyboardShortcut.decode
                |> Option.defaultValue Configuration.Default.KeyboardShortcut
              Background =
                get.Optional.Field "background" Background.decoder
                |> Option.defaultValue Configuration.Default.Background
              ZoomedMode =
                get.Optional.Field "zoomedMode" Decode.bool
                |> Option.defaultValue Configuration.Default.ZoomedMode
              ActivatorPrefixes =
                Decode.dict Decode.string
                |> get.Optional.Field "searchEnginePrefixes"
                |> Option.defaultValue Configuration.Default.ActivatorPrefixes
              Antialiasing =
                Antialiasing.decoder
                |> get.Optional.Field "antialiasing"
                |> Option.defaultValue Configuration.Default.Antialiasing }
        )

    /// Read the config from the config file or return the default config.
    /// May return Error only if it failed to decode the config file.
    static member loadFromFile filePath =
        match File.Exists filePath with
        | false -> Ok Configuration.Default
        | true ->
            // Load config
            match filePath |> File.ReadAllText with
            | "" -> Ok Configuration.Default
            | json ->
                json
                |> Decode.fromString Configuration.decoder

    static member save (filePath: string) (config: Configuration) =
        taskResult {
            // Encode config
            let json =
                config
                |> Configuration.encoder
                |> Encode.toString 2

            // Create directory if it doesn't exist
            do! match filePath |> Path.GetDirectoryName with
                | null -> Error "Invalid file path"
                | fileDir ->
                    fileDir
                    |> Directory.CreateDirectory
                    |> ignore
                    Ok()

            // Save config
            do! try
                    if not <| File.Exists filePath then
                        filePath |> File.Create |> _.Dispose()
                    Ok()
                with err -> Error $"Failed to create config file: {err.Message}"

            do! File.WriteAllTextAsync(filePath, json)
                |> Task.ofUnit
                |> Task.catch
                |> Task.map (function
                    | Choice1Of2 () -> Ok ()
                    | Choice2Of2 e -> Error $"Failed to write to config file: {e.Message}"
                )
        }

    static member ensureDirectoriesExists () =
        // Ensure plugins directory exists
        if Constants.PluginsDirectory |> Directory.Exists |> not then
            logger.Debug "Plugins directory doesn't exist, creating it"
            Constants.PluginsDirectory
            |> Directory.CreateDirectory
            |> ignore

        // Ensure starter config directory exists
        if Constants.ConfigDirectory |> Directory.Exists |> not then
            logger.Debug "Config directory doesn't exist, creating it"
            Constants.ConfigDirectory
            |> Directory.CreateDirectory
            |> ignore
