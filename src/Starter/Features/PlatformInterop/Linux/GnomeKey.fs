module Starter.Features.PlatformInterop.Linux.GnomeKey

open System
open Starter.Features.Config
open Avalonia.Input
open FsToolkit.ErrorHandling

let fromAvaloniaKey (key: Key) =
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

let parseKeyboardShortcut (shortcut: KeyboardShortcut) =
    voption {
        let! modifiers = shortcut.Modifiers |> Array.traverseVOptionM fromAvaloniaKey
        let! key = shortcut.Key |> fromAvaloniaKey
        return String.Concat modifiers + key
    }
