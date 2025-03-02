namespace Starter.PlatformInterop

[<AbstractClass>]
type PlatformInterop() =
    abstract member EnableLaunchAtStartup: unit -> unit
