module Starter.Features.PlatformInterop.Linux.KeyboardShortcut

open Starter.Features.Config
open FsToolkit.ErrorHandling

[<Literal>]
let private StarterKeybindingName = "'Starter'"
[<Literal>]
let private StarterKeybindingCommand = "dbus-send --print-reply --dest=com.carne8.Starter /com/carne8/Starter com.carne8.Starter.Launch"

let setKeyboardShortcut de (keyboardShortcut: KeyboardShortcut) =
    match de with
    | Gnome -> Gnome.setKeyboardShortcut StarterKeybindingName StarterKeybindingCommand keyboardShortcut
    | _ ->
        "Not supported"
        |> Error
        |> Task.singleton


    // | KDE ->
    //     do! executeCommand "kwriteconfig5"
    //         $"--file kglobalshortcutsrc --group \"{keyboardShortcut.Name}\" --key \"_launch\" \"{keyboardShortcut.Binding},none,{keyboardShortcut.Name}\""
    //
    // | XFCE | LXDE ->
    //     // XFCE and LXDE both can use xfconf-query
    //     do! executeCommand "xfconf-query"
    //         $"-c xfce4-keyboard-shortcuts -p \"/commands/custom/{keyboardShortcut.Binding}\" -n -t string -s \"{keyboardShortcut.Command}\""
    //
    //
    // | MATE ->
    //     do! executeCommand "gsettings"
    //         $"set org.mate.Marco.global-keybindings run-command-1 '{keyboardShortcut.Binding}'"
    //     do! executeCommand "gsettings"
    //         $"set org.mate.Marco.keybinding-commands command-1 '{keyboardShortcut.Command}'"
    //
    // | Deepin ->
    //     do! executeCommand "gsettings"
    //         $"set com.deepin.dde.keybinding.system custom '{keyboardShortcut.Binding}'"
    //     do! executeCommand "dbus-send"
    //         $"--type=method_call --dest=com.deepin.daemon.Keybinding /com/deepin/daemon/Keybinding com.deepin.daemon.Keybinding.Add string:'{keyboardShortcut.Name}' string:'{keyboardShortcut.Command}' string:'{keyboardShortcut.Binding}'"
    //
    // | LXQt ->
    //     do! executeCommand "qdbus"
    //         $"org.lxqt.global_key_shortcuts /GlobalKeyShortcuts org.lxqt.global_key_shortcuts.addAction \"{keyboardShortcut.Binding}\" \"{keyboardShortcut.Command}\""
    //
    // | Enlightenment ->
    //     do! executeCommand "enlightenment_remote"
    //         $"-binding-key-add \"{keyboardShortcut.Binding}\" \"exec\" \"{keyboardShortcut.Command}\""
    //
    // | Unknown ->
    //     return! Error "Unknown or unsupported desktop environment"
