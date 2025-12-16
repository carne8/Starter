namespace Starter.Features.InternalSearchEngines.Settings.ViewModels

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

type BackgroundComboBoxItemViewModel =
   { Name: string
     Value: Background
     IsEnabled: bool }

[<AutoOpen>]
module private Helpers =
    type Background with
        static member toString =
            function
            | Background.Acrylic -> "Acrylic"
            | Background.Mica -> "Mica"
            | Background.None -> "None"

        static member fromString =
            function
            | Background.Acrylic -> "Acrylic"
            | Background.Mica -> "Mica"
            | Background.None -> "None"

type SettingsViewModel(baseConfig: Configuration, searchEngines: Dictionary<string, SearchEngine> BehaviorSubject) =
    inherit ReactiveObject() // Equivalent to ViewModelBase

    let config = new BehaviorSubject<Configuration>(baseConfig)

    // Launch at startup
    let platform = PlatformInteropFactory.GetPlatformInterop()
    let mutable launchAtStartup = false
    let mutable launchAtStartupLoading = true

    // Background
    let backgrounds =
        [| { Name = "Acrylic"
             Value = Background.Acrylic
             IsEnabled = not <| OperatingSystem.IsLinux() } // TODO: I18n
           { Name = "Mica"
             Value = Background.Mica
             IsEnabled = not <| OperatingSystem.IsLinux() }
           { Name = "None"
             Value = Background.None
             IsEnabled = true } |]

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
        config.Skip(1).Debounce(TimeSpan.FromMilliseconds 100L)

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
    member this.Backgrounds = backgrounds
    member this.SelectedBackgroundIdx
        with get () = backgrounds |> Array.findIndex (_.Value >> (=) config.Value.Background)
        and set v =
            let { Value = value } = backgrounds |> Array.item v
            config.OnNext <| { config.Value with Background = value }
    member this.BackgroundDescription : string | null =
        if OperatingSystem.IsLinux() then
            "Acrylic and Mica background are not supported on Linux"
        else null

    // Search engine prefixes
    member this.SearchEngineActivators = seActivatorsVms

    // Zoom mode activated
    member this.ZoomedModeActivated
        with get () = config.Value.ZoomedMode
        and set v = config.OnNext <| { config.Value with ZoomedMode = v }
