namespace Starter.Features.PlatformInterop

open Starter.Features
open System.Threading.Tasks

[<AbstractClass>]
type PlatformInterop() =
    abstract member ToggleLaunchAtStartup: bool -> unit
    abstract member IsLaunchAtStartupEnabled: unit -> bool

    abstract member HotkeyRegistrable: bool
    abstract member RegisterHotkey:
        shortcut: Config.KeyboardShortcut
        -> window: Avalonia.Controls.Window
        -> ValueTask<bool>

    abstract member SetupHotkeyCallback: window: Avalonia.Controls.Window -> unit
