namespace Starter.Features.PlatformInterop

type Linux() =
    inherit PlatformInterop()
    override _.ToggleLaunchAtStartup(_enable) = failwith "Not implemented"
    override _.IsLaunchAtStartupEnabled() = failwith "Not implemented"
