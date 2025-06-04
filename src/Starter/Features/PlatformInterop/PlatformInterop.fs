namespace Starter.Features.PlatformInterop

[<AbstractClass>]
type PlatformInterop() =
    abstract member ToggleLaunchAtStartup: bool -> unit
    abstract member IsLaunchAtStartupEnabled: unit -> bool
