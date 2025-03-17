namespace Starter.ViewModels

open System
open System.Reactive.Subjects
open Starter.Features.Config
open Starter.Features.PlatformInterop

type SettingsViewModel(baseConfig: Configuration) =
    inherit ViewModelBase()

    let mutable config = baseConfig
    let configObs = new Subject<Configuration>()
    let platform = PlatformInteropFactory.GetPlatformInterop()
    let transparencyHints =
        [| "Acrylic", Background.Acrylic
           "Mica", Background.Mica
           "None", Background.None |]
        |> Array.unzip

    interface IDisposable with
        override _.Dispose() = configObs.Dispose()

    member _.Configuration = configObs
    member _.Save() = configObs.OnNext config

    override this.OnPropertyChanged e =
        base.OnPropertyChanged(e)

    // --- Settings bindings ---
    member this.LaunchAtStartup
        with get () = config.LaunchAtStartup
        and set v =
            this.SetProperty(&config, { config with LaunchAtStartup = v }) |> ignore
            platform.ToggleLaunchAtStartup v

    member this.Backgrounds = transparencyHints |> fst
    member this.SelectedBackgroundIdx
        with get () = transparencyHints |> snd |> Array.findIndex ((=) config.Background)
        and set v =
            let v' = transparencyHints |> snd |> Array.item v
            this.SetProperty(&config, { config with Background = v' }) |> ignore
