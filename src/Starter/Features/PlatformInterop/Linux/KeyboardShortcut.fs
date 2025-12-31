module Starter.Features.PlatformInterop.Linux.KeyboardShortcut

open Starter.Features.Config

open System
open System.Text

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

open System.Diagnostics
open System.Text.RegularExpressions
open FsToolkit.ErrorHandling

[<Literal>]
let private StarterKeybindingName = "'Starter'"
[<Literal>]
let private StarterKeybindingCommand = "dbus-send --print-reply --dest=com.carne8.Starter /com/carne8/Starter com.carne8.Starter.Launch"

let inline startProcess command args =
    ProcessStartInfo(
        FileName = command,
        Arguments = args,
        RedirectStandardOutput = true,
        RedirectStandardError = true
    )
    |> Process.Start
    |> Result.requireNotNull "Failed to start process"

let inline readProcessOutput command args =
    result {
        use! proc = startProcess command args
        proc.WaitForExit()
        return proc.StandardOutput.ReadToEnd()
    }

let private executeCommand command args =
    try
        result {
            use! proc = startProcess command args
            proc.WaitForExit()

            match proc.ExitCode with
            | 0 -> return ()
            | exitCode ->
                let error = proc.StandardError.ReadToEnd()
                printfn "%A %A" command args
                return! Error $"Command failed with exit code {exitCode}: {error}"
        }
    with
    | ex -> Error ex.Message

let setKeyboardShortcut de (keyboardShortcut: KeyboardShortcut) =
    match de with
    | Gnome | Budgie | Cinnamon ->
        // Gnome and Budgie and Cinnamon both use GNOME's gsettings backend
        let regex = Regex "'((?:(\d)|.)+?)'"

        result {
            let! keybinding =
                keyboardShortcut
                |> GnomeKey.parseKeyboardShortcut
                |> Result.requireValueSome $"Failed to convert keyboard shortcut in gsettings keybinding: %A{keyboardShortcut}"

            let! output = readProcessOutput "gsettings" "get org.gnome.settings-daemon.plugins.media-keys custom-keybindings"
            let otherKeybindings =
                output
                |> regex.Matches
                |> Seq.choose (fun m ->
                    option {
                        let! customKeybinding = m.Groups |> Seq.tryItem 1
                        let customKeybindingPath = customKeybinding.Value

                        let! customKeybindingIdx = m.Groups |> Seq.tryItem 2
                        let! customKeybindingIdx = Int32.TryParse customKeybindingIdx.ValueSpan |> ValueOption.ofPair

                        return struct (customKeybindingPath, customKeybindingIdx)
                    }
                )
                |> Seq.toArray

            let keybindingExists =
                otherKeybindings |> Array.tryFind (fun struct (keybindingPath, _) ->
                    readProcessOutput "gsettings" $"get org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{keybindingPath} name"
                    |> Result.map (fun output -> output.Trim() = StarterKeybindingName)
                    |> Result.defaultValue false
                )

            // Update keybinding list
            let! keybindingName =
                match keybindingExists with
                | Some struct (keybindingName, _) -> Ok keybindingName
                | None ->
                    let newKeybindingIdx =
                        match otherKeybindings with
                        | [| |] -> 0
                        | _ ->
                            otherKeybindings
                            |> Array.maxBy valueSnd
                            |> valueSnd
                            |> (+) 1
                    let newKeybindingName = $"/org/gnome/settings-daemon/plugins/media-keys/custom-keybindings/custom{newKeybindingIdx}/"

                    let cmd = StringBuilder()
                    cmd.Append "set org.gnome.settings-daemon.plugins.media-keys custom-keybindings [" |> ignore
                    for struct (keybindingName, _) in otherKeybindings do
                        cmd.Append ''' |> ignore
                        cmd.Append keybindingName |> ignore
                        cmd.Append "'," |> ignore

                    cmd.Append ''' |> ignore
                    cmd.Append newKeybindingName |> ignore
                    cmd.Append "']" |> ignore

                    cmd.ToString()
                    |> executeCommand "gsettings"
                    |> function
                        | Ok () -> Ok newKeybindingName
                        | Error err -> Error $"Failed to set keybindings: {err}"

            // Update keybinding
            do! executeCommand "gsettings" $"set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{keybindingName} name {StarterKeybindingName}"
                |> Result.mapError (sprintf "Failed to set keybinding name: %s")
            do! executeCommand "gsettings" $"set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{keybindingName} command \"{StarterKeybindingCommand}\""
                |> Result.mapError (sprintf "Failed to set keybinding command: %s")
            do! executeCommand "gsettings" $"set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{keybindingName} binding '{keybinding}'" // TODO
                |> Result.mapError (sprintf "Failed to set keybinding binding: %s")
        }
    | _ -> Error "Not supported"


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
