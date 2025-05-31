namespace Starter.ViewModels

open System
open System.Collections.Generic
open ReactiveUI
open R3

open Starter.Features.Config
open Starter.Features.PlatformInterop
open Starter.SearchEngine

type SearchEnginePrefixViewModel(se: ISearchEngine, prefix: string, onPrefixChanged) =
    let icon = se.Icon |> StarterIconSource.build

    member this.Icon = icon
    member this.Name = se.Name
    member this.Prefix
        with get () = prefix
        and set v = v |> onPrefixChanged


type SettingsViewModel(baseConfig: Configuration, searchEngines: IDictionary<string, ISearchEngine> BehaviorSubject) =
    inherit ReactiveObject() // Equivalent to ViewModelBase

    let mutable config = baseConfig
    let configSaves = new Subject<Configuration>()

    // Launch at startup
    let platform = PlatformInteropFactory.GetPlatformInterop()
    let mutable launchAtStartup = false

    // Background
    let transparencyHints =
        [| "Acrylic", Background.Acrylic
           "Mica", Background.Mica
           "None", Background.None |]
        |> Array.unzip

    // Search engine prefixes
    let onPrefixChanged seId newPrefix =
        let newMap =
            config.SearchEnginePrefixes |> Map.change seId (
                match newPrefix with
                | "" -> fun _ -> None
                | s -> fun _ -> Some s
            )

        config <- { config with SearchEnginePrefixes = newMap }

    let sePrefixVms =
        searchEngines.Select(Seq.map (fun (kv: KeyValuePair<_, _>) ->
            let prefix =
                config.SearchEnginePrefixes
                |> Map.tryFind kv.Key
                |> Option.defaultValue String.Empty

            SearchEnginePrefixViewModel(
                kv.Value,
                prefix,
                onPrefixChanged kv.Key
            )
        ))

    interface IDisposable with
        override _.Dispose() =
            configSaves.Dispose()

    member _.Configuration = configSaves
    member _.Save() = configSaves.OnNext config

    // --- Settings bindings ---

    // Launch at startup
    member this.LaunchAtStartup
        with get () = launchAtStartup
        and set v =
            this.RaiseAndSetIfChanged(&launchAtStartup, v) |> ignore
            platform.ToggleLaunchAtStartup v

    // Background
    member this.Backgrounds = transparencyHints |> fst
    member this.SelectedBackgroundIdx
        with get () = transparencyHints |> snd |> Array.findIndex ((=) config.Background)
        and set v =
            let v' = transparencyHints |> snd |> Array.item v
            this.RaiseAndSetIfChanged(&config, { config with Background = v' }) |> ignore

    // Search engine prefixes
    member this.SearchEnginePrefixes = sePrefixVms
