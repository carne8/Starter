namespace Starter.Features.PlatformInterop

open System

// TODO: Switch to DI
type PlatformInterop() =
    static let platformInterop: IPlatformInterop =
        if OperatingSystem.IsWindows() then WindowsPlatformInterop()
        elif OperatingSystem.IsLinux() then LinuxPlatformInterop()
        else failwith "Not supported platform"

    static member GetPlatformInterop() = platformInterop
