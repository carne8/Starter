namespace Starter.Features.Config.UI.StarterSettings

open Starter.Features.Config
open Starter.Features.PlatformInterop
open Starter.SearchEngine

open System
open System.Collections.Generic
open System.Threading.Tasks

open ReactiveUI
open R3

type SearchEnginePrefixViewModel(se: SearchEngine, prefix: string, onPrefixChanged) =
    let mutable prefix = prefix

    member this.Icon = se.Icon
    member this.Name = se.Name
    member this.Prefix
        with get () = prefix
        and set v = prefix <- v; v |> onPrefixChanged

type ViewModel(baseConfig: Configuration, searchEngines: Dictionary<string, SearchEngine> BehaviorSubject) =
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

    // Search engine prefixes
    let onPrefixChanged seId newPrefix =
        let newMap =
            config.Value.SearchEnginePrefixes |> Map.change seId (
                match newPrefix with
                | "" -> fun _ -> None
                | s -> fun _ -> Some s
            )

        config.OnNext <| { config.Value with SearchEnginePrefixes = newMap }

    let sePrefixVms =
        searchEngines.Select(
            Seq.map (fun (kv: KeyValuePair<_, _>) ->
                let prefix =
                    config.Value.SearchEnginePrefixes
                    |> Map.tryFind kv.Key
                    |> Option.defaultValue String.Empty

                SearchEnginePrefixViewModel(
                    kv.Value,
                    prefix,
                    onPrefixChanged kv.Key
                )
            )
            >> Seq.toArray
        )

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
    member this.SearchEnginePrefixes = sePrefixVms

    // Zoom mode activated
    member this.ZoomedModeActivated
        with get () = config.Value.ZoomedMode
        and set v = config.OnNext <| { config.Value with ZoomedMode = v }
