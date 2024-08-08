module Starter.App.Host

open Starter.App
open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish

open System.Threading
open SharpHook
open SharpHook.Native

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


let startElmish (host: IViewHost) =
    Program.mkProgram State.init State.update View.view
    |> Program.withHost host
    |> Program.run

let initAppWindow
    (window: HostWindow)
    =
    window.Title <- "Starter"
    // window.SystemDecorations <- SystemDecorations.None
    window.Width <- 300
    window.Height <- 30
    // window.Show()

    #if DEBUG
    window.AttachDevTools()
    #endif

    startElmish window

type App() =
    inherit Application()

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.ShutdownMode <- ShutdownMode.OnExplicitShutdown

            // Create window
            let window = HostWindow()
            window |> initAppWindow

            // Hook
            let hook = new KeyboardShortcutListener()
            hook.ShortcutPressed.Add(fun _ ->
                Threading.runInUIThread'
                    Threading.DispatcherPriority.MaxValue
                    (match window.IsActive with
                     | true -> ignore
                     | false -> window.Show)
            )
            hook.Run(CancellationToken.None) |> ignore

            window.LostFocus.Add(fun _ -> // TODO: This doesn't work
                printfn "Lost focus"
                window.Hide()
            )
        | _ -> ()

        base.OnFrameworkInitializationCompleted()

