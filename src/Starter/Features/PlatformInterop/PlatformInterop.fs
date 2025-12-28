namespace Starter.Features.PlatformInterop

[<AbstractClass>]
type PlatformInterop() =
    abstract member ToggleLaunchAtStartup: bool -> unit
    abstract member IsLaunchAtStartupEnabled: unit -> bool

    abstract member RegisterHotkey:
        modifiers: Avalonia.Input.Key array ->
        key: Avalonia.Input.Key ->
        window: Avalonia.Controls.Window -> unit

    abstract member SetupHotkeyCallback: window: Avalonia.Controls.Window -> unit
