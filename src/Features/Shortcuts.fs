module Starter.Features.Shortcuts

open System.Threading
open SharpHook
open SharpHook.Native

type ShortcutListener(keys: KeyCode seq) =
    let hook = new TaskPoolGlobalHook()
    let event = new Event<unit>()

    // State
    let shortcutKeys = keys |> Set.ofSeq
    let mutable pressedKeys = Set.empty<KeyCode>

    // Event handlers
    let keyUp keyCode = pressedKeys <- pressedKeys |> Set.remove keyCode
    let keyDown keyCode =
        match pressedKeys |> Set.contains keyCode with // Ignore already pressed keys
        | true -> ()
        | false ->
            // Add the key to the set
            pressedKeys <- pressedKeys |> Set.add keyCode

            // Check if the shortcut is pressed
            let isShortcut =
                shortcutKeys.Count = pressedKeys.Count
                && shortcutKeys |> Set.forall (fun key -> pressedKeys |> Set.contains key)

            match isShortcut with
            | true -> event.Trigger()
            | false -> ()

    do
        hook.HookEnabled.Add(fun _ -> printfn "Hook enabled")
        hook.KeyPressed.Add(_.Data.KeyCode >> keyDown)
        hook.KeyReleased.Add(_.Data.KeyCode >> keyUp)

    member _.Run(ct: CancellationToken) =
        ct.Register(fun _ -> hook.Dispose()) |> ignore
        hook.RunAsync() |> ignore

    member _.Dispose() = hook.Dispose()

    [<CLIEvent>]
    member _.ShortcutPressed = event.Publish
