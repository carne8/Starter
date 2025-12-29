namespace Starter.Features.PlatformInterop

open Starter.Features

[<AbstractClass>]
type PlatformInterop() =
    abstract member ToggleLaunchAtStartup: bool -> unit
    abstract member IsLaunchAtStartupEnabled: unit -> bool

    abstract member RegisterHotkey:
        shortcut: Config.KeyboardShortcut
        -> window: Avalonia.Controls.Window
        -> unit

    abstract member SetupHotkeyCallback: window: Avalonia.Controls.Window -> unit
