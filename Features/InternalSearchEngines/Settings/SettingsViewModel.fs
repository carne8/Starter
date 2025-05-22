namespace Starter.ViewModels

open System
open System.Reactive.Subjects
open System.Collections.Generic
open ReactiveUI

open Starter.Features.Config
open Starter.Features.PlatformInterop
open Starter.SearchEngine

type SearchEnginePrefixViewModel(seName: string, prefix: string) =
    inherit ReactiveObject()

    let mutable prefix = prefix

    member this.Name = seName
    member this.Prefix
        with get () = prefix
        and set v = this.RaiseAndSetIfChanged(&prefix, v) |> ignore


type SettingsViewModel(baseConfig: Configuration, searchEngines: IDictionary<string, ISearchEngine>) =
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
    let disposables = List(searchEngines.Count + 1)
    let sePrefixVms =
        lazy
            searchEngines
            |> Seq.map (fun kv ->
                let prefix =
                    config.SearchEnginePrefixes
                    |> Map.tryFind kv.Key
                    |> Option.defaultValue String.Empty
                let vm = SearchEnginePrefixViewModel(kv.Value.ShortName, prefix)

                vm.Changed.Subscribe(fun _ ->
                    let newMap =
                        config.SearchEnginePrefixes |> Map.change kv.Key (
                            match vm.Prefix with
                            | "" -> fun _ -> None
                            | s -> fun _ -> Some s
                        )

                    config <- { config with SearchEnginePrefixes = newMap }
                )
                |> disposables.Add

                vm
            )
            |> Seq.toArray


    interface IDisposable with
        override _.Dispose() =
            disposables |> Seq.iter _.Dispose()
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
    member this.SearchEnginePrefixes = sePrefixVms.Value
