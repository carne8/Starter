module Starter.Features.PlatformInterop.Linux.Gnome

open System
open System.Text
open System.Text.RegularExpressions
open System.Threading.Tasks

open Starter.Features.Config

open Avalonia.Input
open FsToolkit.ErrorHandling

let private fromAvaloniaKey (key: Key) =
    match key with
    | Key.None -> ValueNone
    | Key.LeftShift
    | Key.RightShift -> ValueSome "<Shift>"
    | Key.LeftCtrl
    | Key.RightCtrl -> ValueSome "<Control>"
    | Key.LeftAlt
    | Key.RightAlt -> ValueSome "<Alt>"
    | Key.LWin
    | Key.RWin -> ValueSome "<Super>"
    | Key.A -> ValueSome "a"
    | Key.B -> ValueSome "b"
    | Key.C -> ValueSome "c"
    | Key.D -> ValueSome "d"
    | Key.E -> ValueSome "e"
    | Key.F -> ValueSome "f"
    | Key.G -> ValueSome "g"
    | Key.H -> ValueSome "h"
    | Key.I -> ValueSome "i"
    | Key.J -> ValueSome "j"
    | Key.K -> ValueSome "k"
    | Key.L -> ValueSome "l"
    | Key.M -> ValueSome "m"
    | Key.N -> ValueSome "n"
    | Key.O -> ValueSome "o"
    | Key.P -> ValueSome "p"
    | Key.Q -> ValueSome "q"
    | Key.R -> ValueSome "r"
    | Key.S -> ValueSome "s"
    | Key.T -> ValueSome "t"
    | Key.U -> ValueSome "u"
    | Key.V -> ValueSome "v"
    | Key.W -> ValueSome "w"
    | Key.X -> ValueSome "x"
    | Key.Y -> ValueSome "y"
    | Key.Z -> ValueSome "z"
    | Key.F1 -> ValueSome "F1"
    | Key.F2 -> ValueSome "F2"
    | Key.F3 -> ValueSome "F3"
    | Key.F4 -> ValueSome "F4"
    | Key.F5 -> ValueSome "F5"
    | Key.F6 -> ValueSome "F6"
    | Key.F7 -> ValueSome "F7"
    | Key.F8 -> ValueSome "F8"
    | Key.F9 -> ValueSome "F9"
    | Key.F10 -> ValueSome "F10"
    | Key.F11 -> ValueSome "F11"
    | Key.F12 -> ValueSome "F12"
    | Key.F13 -> ValueSome "F13"
    | Key.F14 -> ValueSome "F14"
    | Key.F15 -> ValueSome "F15"
    | Key.F16 -> ValueSome "F16"
    | Key.F17 -> ValueSome "F17"
    | Key.F18 -> ValueSome "F18"
    | Key.F19 -> ValueSome "F19"
    | Key.F20 -> ValueSome "F20"
    | Key.F21 -> ValueSome "F21"
    | Key.F22 -> ValueSome "F22"
    | Key.F23 -> ValueSome "F23"
    | Key.F24 -> ValueSome "F24"
    | Key.Enter -> ValueSome "Return"
    | Key.Escape -> ValueSome "Escape"
    | Key.Tab -> ValueSome "Tab"
    | Key.Back -> ValueSome "BackSpace"
    | Key.Delete -> ValueSome "Delete"
    | Key.Insert -> ValueSome "Insert"
    | Key.Home -> ValueSome "Home"
    | Key.End -> ValueSome "End"
    | Key.PageUp -> ValueSome "Page_Up"
    | Key.PageDown -> ValueSome "Page_Down"
    | Key.Up -> ValueSome "Up"
    | Key.Down -> ValueSome "Down"
    | Key.Left -> ValueSome "Left"
    | Key.Right -> ValueSome "Right"
    | Key.Space -> ValueSome "space"
    | Key.VolumeMute -> ValueSome "AudioMute"
    | Key.VolumeDown -> ValueSome "AudioLowerVolume"
    | Key.VolumeUp -> ValueSome "AudioRaiseVolume"
    | Key.Play -> ValueSome "AudioPlay"
    | Key.Pause -> ValueSome "AudioPause"
    | Key.MediaStop -> ValueSome "AudioStop"
    | Key.MediaNextTrack -> ValueSome "AudioNext"
    | Key.MediaPreviousTrack -> ValueSome "AudioPrev"
    | _ -> ValueNone

let private parseKeyboardShortcut (shortcut: KeyboardShortcut) =
    voption {
        let! modifiers = shortcut.Modifiers |> Array.traverseVOptionM fromAvaloniaKey
        let! key = shortcut.Key |> fromAvaloniaKey
        return String.Concat modifiers + key
    }

let private findCustomKeybindings () =
    let regex = Regex "'((?:(\d)|.)+?)'"

    Proc.readProcessOutput "gsettings" "get org.gnome.settings-daemon.plugins.media-keys custom-keybindings"
    |> TaskResult.map (fun output ->
        output
        |> regex.Matches
        |> Seq.choose (fun m ->
            option {
                let! path = m.Groups |> Seq.tryItem 1 |> Option.map _.Value
                let! idxGroup = m.Groups |> Seq.tryItem 2
                let! idx = Int32.TryParse idxGroup.ValueSpan |> ValueOption.ofPair

                return struct {| Path = path; Idx = idx |}
            }
        )
        |> Seq.toArray
    )

let private findKeybindingByName name (keybindings: struct {| Path: string; Idx: int |} array) =
    taskResult {
        let mutable foundKeybinding = ValueNone
        let mutable i = 0

        while foundKeybinding.IsNone && i < keybindings.Length do
            let keybinding = keybindings[i]
            let! output =
                Proc.readProcessOutput
                    "gsettings"
                    $"get org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{keybinding.Path} name"

            if output.Trim() = name then
                foundKeybinding <- ValueSome keybinding

            i <- i+1

        return foundKeybinding
    }

let private setKeybindingList (keybindings: struct {| Path: string; Idx: int |} seq) =
    let cmd = StringBuilder()
    cmd.Append "set org.gnome.settings-daemon.plugins.media-keys custom-keybindings [" |> ignore

    let keybindings = keybindings.GetEnumerator()
    let mutable finished = keybindings.MoveNext() |> not

    while not finished do
        let left = keybindings.Current

        cmd.Append ''' |> ignore
        cmd.Append left.Path |> ignore

        if keybindings.MoveNext() then
            cmd.Append "'," |> ignore
        else
            cmd.Append ''' |> ignore
            finished <- true

    cmd.Append "]" |> ignore

    Proc.executeCommand "gsettings" (cmd.ToString())
    |> TaskResult.mapError (sprintf "Failed to update the keybionding list: %s")

let private updateKeybinding (path: string) (name: string) (command: string) (binding: string) =
    taskResult {
        do! Proc.executeCommand "gsettings" $"set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{path} name {name}"
            |> TaskResult.mapError (sprintf "Failed to set keybinding name: %s")
        do! Proc.executeCommand "gsettings" $"set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{path} command \"{command}\""
            |> TaskResult.mapError (sprintf "Failed to set keybinding command: %s")
        do! Proc.executeCommand "gsettings" $"set org.gnome.settings-daemon.plugins.media-keys.custom-keybinding:{path} binding '{binding}'"
            |> TaskResult.mapError (sprintf "Failed to set keybinding binding: %s")
    }

let setKeyboardShortcut name command keyboardShortcut =
    taskResult {
        let! keybinding =
            keyboardShortcut
            |> parseKeyboardShortcut
            |> Result.requireValueSome $"Failed to convert keyboard shortcut in gsettings keybinding: %A{keyboardShortcut}"

        let! keybindings = findCustomKeybindings ()
        let! starterKeybinding = findKeybindingByName name keybindings

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
                |> setKeybindingList
                |> TaskResult.map (fun () -> newKeybindingPath)
                |> ValueTask<Result<_, _>>

        return! updateKeybinding keybindingPath name command keybinding
    }
