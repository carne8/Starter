namespace Starter.Features.PlatformInterop

open Starter.Features
open System.Threading.Tasks

type IPlatformInterop =
    abstract member ToggleLaunchAtStartup: enable: bool -> unit
    abstract member IsLaunchAtStartupEnabled: unit -> bool

    abstract member HotkeyRegistrable: bool
    abstract member RegisterHotkey:
        shortcut: Config.KeyboardShortcut
        -> window: Avalonia.Controls.Window
        -> Task<bool>

    abstract member SetupHotkeyCallback: window: Avalonia.Controls.Window -> unit
    abstract member SupportBackground: background: Config.Background -> bool
    abstract member EnsureConfigCompatibility: config: Config.Configuration -> Config.Configuration
