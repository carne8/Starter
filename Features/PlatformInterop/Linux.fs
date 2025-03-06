namespace Starter.Features.PlatformInterop

type Linux() =
    inherit PlatformInterop()
    override _.EnableLaunchAtStartup() = failwith "Not implemented"
