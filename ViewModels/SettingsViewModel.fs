namespace Starter.ViewModels

open System
open System.Reactive.Subjects
open Starter.Features.Config
open Starter.Features.PlatformInterop

type SettingsViewModel(baseConfig: Configuration) =
    inherit ViewModelBase()

    let mutable config = baseConfig
    let configObs = new BehaviorSubject<_>(baseConfig)

    let platform = PlatformInteropFactory.GetPlatformInterop()

    interface IDisposable with
        override _.Dispose() = configObs.Dispose()

    override this.OnPropertyChanged e =
        base.OnPropertyChanged(e)
        configObs.OnNext config

    member this.LaunchAtStartup
        with get () = config.LaunchAtStartup
        and set v =
            this.SetProperty(&config, { config with LaunchAtStartup = v }) |> ignore
            platform.ToggleLaunchAtStartup v
