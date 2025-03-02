namespace Starter.PlatformInterop

open System

type PlatformInteropFactory() =
    static member GetPlatformInterop() : PlatformInterop =
        if OperatingSystem.IsWindows() then Windows()
        elif OperatingSystem.IsLinux() then Linux()
        else failwith "Not supported platform"
