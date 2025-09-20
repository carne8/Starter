namespace Starter.Features.InternalSearchEngines.Settings.ViewModels

open Starter.Features.Config
open Starter.Features.PlatformInterop
open Starter.SearchEngine

open System
open System.Collections.Generic
open System.Threading.Tasks

open ReactiveUI
open R3

type SettingsViewModel(baseConfig: Configuration, searchEngines: Dictionary<string, SearchEngine> BehaviorSubject) =
    inherit ReactiveObject() // Equivalent to ViewModelBase

    let config = new BehaviorSubject<Configuration>(baseConfig)

    // Launch at startup
    let platform = PlatformInteropFactory.GetPlatformInterop()
    let mutable launchAtStartup = false
    let mutable launchAtStartupLoading = true

    // Background
    let transparencyHints =
        [| "Acrylic", Background.Acrylic
           "Mica", Background.Mica
           "None", Background.None |]
        |> Array.unzip

    // Activator prefixes
    let onActivatorPrefixChanged activatorId newPrefix =
        let newMap =
            config.Value.ActivatorPrefixes |> Map.change activatorId (
                match newPrefix with
                | "" -> fun _ -> None
                | s -> fun _ -> Some s
            )

        config.OnNext <| { config.Value with ActivatorPrefixes = newMap }

    let seActivatorsVms =
        searchEngines |> Observable.map (Seq.map (fun kv ->
            let searchEngine = kv.Value
            let activators =
                searchEngine.Activators |> Observable.map (Seq.map (fun activator ->
                    let prefix =
                        config.Value.ActivatorPrefixes
                        |> Map.tryFind activator.Id
                        |> Option.defaultValue String.Empty
                    struct (activator, prefix)
                ))

            SearchEngineActivatorsViewModel(searchEngine, activators, onActivatorPrefixChanged)
        ))

    interface IDisposable with
        override _.Dispose() = config.Dispose()

    member _.Configuration =
        config.Skip(1).Debounce(TimeSpan.FromMilliseconds 100)

    // --- Settings bindings ---

    member this.OnOpened() =
        Task.Run<unit>(fun () -> // Checks if launch at startup is enabled
            task {
                this.LaunchAtStartup <- platform.IsLaunchAtStartupEnabled()
                this.LaunchAtStartupLoading <- false
            }
        ) |> ignore

    // Launch at startup
    member this.LaunchAtStartupLoading
        with get () = launchAtStartupLoading
        and set v = this.RaiseAndSetIfChanged(&launchAtStartupLoading, v) |> ignore
    member this.LaunchAtStartup
        with get () = launchAtStartup
        and set v =
            this.RaiseAndSetIfChanged(&launchAtStartup, v) |> ignore
            Task.Run<unit>(fun () -> platform.ToggleLaunchAtStartup v) |> ignore

    // Background
    member this.Backgrounds = transparencyHints |> fst
    member this.SelectedBackgroundIdx
        with get () = transparencyHints |> snd |> Array.findIndex ((=) config.Value.Background)
        and set v =
            let v' = transparencyHints |> snd |> Array.item v
            config.OnNext <| { config.Value with Background = v' }

    // Search engine prefixes
    member this.SearchEngineActivators = seActivatorsVms

    // Zoom mode activated
    member this.ZoomedModeActivated
        with get () = config.Value.ZoomedMode
        and set v = config.OnNext <| { config.Value with ZoomedMode = v }
