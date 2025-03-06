namespace Starter.Features.PlatformInterop

[<AbstractClass>]
type PlatformInterop() =
    abstract member EnableLaunchAtStartup: unit -> unit
