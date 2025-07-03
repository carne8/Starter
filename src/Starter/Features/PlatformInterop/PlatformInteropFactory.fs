namespace Starter.Features.PlatformInterop

open System

type PlatformInteropFactory() =
    static let platformInterop: PlatformInterop =
        if OperatingSystem.IsWindows() then Windows()
        elif OperatingSystem.IsLinux() then Linux()
        else failwith "Not supported platform"

    static member GetPlatformInterop() = platformInterop
