module Starter.Features.PlatformInterop.Linux.KeyboardShortcut

open Starter.Features.Config

open System
open System.Threading.Tasks
open FsToolkit.ErrorHandling

type DesktopEnvironment =
    | Gnome
    | KDE
    | XFCE
    | Cinnamon
    | MATE
    | Budgie
    | Deepin
    | LXDE
    | LXQt
    | Enlightenment
    | Unknown

    static member detectDesktopEnvironment () =
        let xdgCurrent =
            Environment.GetEnvironmentVariable "XDG_CURRENT_DESKTOP"
            |> Option.ofObj
            |> Option.map _.ToLowerInvariant()
            |> Option.defaultValue ""

        let xdgSession =
            Environment.GetEnvironmentVariable "XDG_SESSION_DESKTOP"
            |> Option.ofObj
            |> Option.map _.ToLowerInvariant()
            |> Option.defaultValue ""

        // Check for specific desktop environments
        if xdgCurrent.Contains "gnome" || xdgSession.Contains "gnome" then Gnome
        elif xdgCurrent.Contains "kde" || xdgSession.Contains "plasma" then KDE
        elif xdgCurrent.Contains "xfce" || xdgSession.Contains "xfce" then XFCE
        elif xdgCurrent.Contains "cinnamon" || xdgSession.Contains "cinnamon" then Cinnamon
        elif xdgCurrent.Contains "mate" || xdgSession.Contains "mate" then MATE
        elif xdgCurrent.Contains "budgie" || xdgSession.Contains "budgie" then Budgie
        elif xdgCurrent.Contains "deepin" || xdgSession.Contains "deepin" then Deepin
        elif xdgCurrent.Contains "lxde" || xdgSession.Contains "lxde" then LXDE
        elif xdgCurrent.Contains "lxqt" || xdgSession.Contains "lxqt" then LXQt
        elif xdgCurrent.Contains "enlightenment" || xdgSession.Contains "enlightenment" then Enlightenment
        else Unknown

[<Literal>]
let private StarterKeybindingName = "'Starter'"
[<Literal>]
let private StarterKeybindingCommand = "dbus-send --print-reply --dest=com.carne8.Starter /com/carne8/Starter com.carne8.Starter.Launch"

let setKeyboardShortcut de (keyboardShortcut: KeyboardShortcut) =
    match de with
    | Gnome | Budgie | Cinnamon ->
        // Gnome and Budgie and Cinnamon both use GNOME's gsettings backend
        taskResult {
            let! keybinding =
                keyboardShortcut
                |> Gnome.parseKeyboardShortcut
                |> Result.requireValueSome $"Failed to convert keyboard shortcut in gsettings keybinding: %A{keyboardShortcut}"

            let! keybindings = Gnome.findCustomKeybindings ()
            let! starterKeybinding = Gnome.findKeybindingByName StarterKeybindingName keybindings

            // Update keybinding list
            let! keybindingPath =
                match starterKeybinding with
                | ValueSome keybinding ->
                    keybinding.Path
                    |> Ok
                    |> ValueTask.FromResult
                | ValueNone ->
                    let newKeybindingIdx =
                        match keybindings with
                        | [| |] -> 0
                        | _ -> (keybindings |> Array.maxBy _.Idx).Idx + 1

                    let newKeybindingPath = $"/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/custom{newKeybindingIdx}/"

                    seq { yield! keybindings; struct {| Path = newKeybindingPath; Idx = newKeybindingIdx |}}
                    |> Gnome.setKeybindingList
                    |> TaskResult.map (fun () -> newKeybindingPath)
                    |> ValueTask<Result<_, _>>

            return! Gnome.updateKeybinding keybindingPath StarterKeybindingName StarterKeybindingCommand keybinding
        }

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
