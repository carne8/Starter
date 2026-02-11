namespace Starter.Features.PlatformInterop

open System

type PlatformInteropFactory() =
    static let platformInterop: PlatformInterop =
        if OperatingSystem.IsWindows() then WindowsPlatformInterop()
        elif OperatingSystem.IsLinux() then LinuxPlatformInterop()
        else failwith "Not supported platform"

    static member GetPlatformInterop() = platformInterop
