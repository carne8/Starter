namespace Starter.PlatformInterop

type Linux() =
    inherit PlatformInterop()
    override _.EnableLaunchAtStartup() = failwith "Not implemented"
