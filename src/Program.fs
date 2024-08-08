module Program

open System
open System.Threading
open SharpHook
open SharpHook.Native

open Avalonia
open Avalonia.Controls

[<CompiledName "BuildAvaloniaApp">]
let buildAvaloniaApp () =
    AppBuilder
        .Configure<App.App>()
        .UsePlatformDetect()
        .UseSkia()
        .LogToTrace()

type KeyboardShortcutListener() =
    let hook = new TaskPoolGlobalHook()
    let event = new Event<unit>()

    // State
    let shortcutKeys = [KeyCode.VcRightControl; KeyCode.VcSpace] |> Set.ofList
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

let appMainDelegate =
    AppBuilder.AppMainDelegate(fun app _args ->
        let cts = new CancellationTokenSource()

        let mutable openedWindow: Window option = None
        let showWindow () = Threading.Dispatcher.UIThread.Post((fun () ->
            match openedWindow with
            | Some _ -> printfn "Window already opened"
            | None ->
                let window = new Window()
                openedWindow <- Some window

                window.SystemDecorations <- SystemDecorations.None
                window.Width <- 300
                window.Height <- 30
                window.Show()
                window.Closing.Add(fun _ -> openedWindow <- None)
        ))

        // Hooks
        let hook = new KeyboardShortcutListener()
        hook.ShortcutPressed.Add(fun _ -> showWindow())

        hook.Run(cts.Token) // Run the hook
        app.Run(cts.Token) // Run the app
    )

[<EntryPoint; STAThread>]
let main argv =
    let app = buildAvaloniaApp()
    app.Start(appMainDelegate, argv)

    0