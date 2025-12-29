namespace Starter.Features.InternalSearchEngines.Settings.ViewModels

open System
open System.Collections.Generic
open Avalonia.Input
open ReactiveUI
open Starter.Features.Logging
open Starter.Features.Config

type KeyboardShortcutInputViewModel(initialKeyboardShortcut: KeyboardShortcut, onKeyboardChanged: KeyboardShortcut -> unit) as this =
    inherit ReactiveObject() // TODO: Switch to CommunityToolkit

    let mutable listenKeys = false
    let stoppedListening = Event<unit>()
    let stoppedListeningEvent = stoppedListening.Publish

    let mutable keyboardShortcut = initialKeyboardShortcut
    let mutable pressedModifiers = HashSet<Key>()
    let mutable pressedKey = Key.None
    let mutable text = ""

    do this.RefreshText
        keyboardShortcut.Modifiers
        keyboardShortcut.Key

    // Fields
    member this.Text
        with get () = text
        and set v = this.RaiseAndSetIfChanged(&text, v) |> ignore

    member this.RefreshText modifiers key =
        this.Text <-
            modifiers
            |> Seq.map string
            |> fun s -> String.Join(" + ", s)
            |> fun s -> if key <> Key.None then $"{s} + {key}" else s

    member this.StoppedListeningKeys = stoppedListeningEvent

    // Methods
    member this.StartListeningKeys() = listenKeys <- true

    member this.Cancel() =
        listenKeys <- false
        stoppedListening.Trigger()

        pressedKey <- Key.None
        pressedModifiers.Clear()
        this.RefreshText keyboardShortcut.Modifiers keyboardShortcut.Key

    member this.Validate() =
        match pressedModifiers |> Seq.isEmpty, pressedKey with
        | true, _
        | _, Key.None -> this.Cancel()
        | _ ->
            keyboardShortcut <-
                { Modifiers = pressedModifiers |> Seq.toArray
                  Key = pressedKey }

            logger.Information $"Keyboard shortcut changed: %A{keyboardShortcut}"
            this.RefreshText keyboardShortcut.Modifiers keyboardShortcut.Key
            onKeyboardChanged keyboardShortcut

            listenKeys <- false
            stoppedListening.Trigger()

    member this.KeyboardShortcutKeyDown(key: Key) =
        if not listenKeys then () else
        match key with
        | Key.Escape -> this.Cancel()
        | Key.Enter
        | Key.Return -> this.Validate()

        // Modifiers
        | Key.LWin
        | Key.RWin
        | Key.LeftAlt
        | Key.RightAlt
        | Key.LeftCtrl
        | Key.RightCtrl
        | Key.LeftShift
        | Key.RightShift ->
            key |> pressedModifiers.Add |> ignore
            this.RefreshText (Seq.toArray pressedModifiers) pressedKey

        // Other keys
        | _ ->
            pressedKey <- key
            this.RefreshText (Seq.toArray pressedModifiers) pressedKey
