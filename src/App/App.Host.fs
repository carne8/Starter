module Starter.App.Host

open Starter.App
open Starter.Features.Shortcuts

open System.Threading
open Elmish
open SharpHook.Native
open FluentAvalonia.Styling

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish
open Avalonia.Media

let startElmish (hostWindow: HostWindow) =
    let subscribeToWindowEvents = fun dispatch ->
        let dispatch = Msg.WindowEvent >> dispatch

        let opened = hostWindow.Opened.Subscribe(fun _ -> WindowEvent.WindowOpened |> dispatch)
        let hidden = hostWindow.Deactivated.Subscribe(fun _ -> WindowEvent.WindowHidden |> dispatch)

        { new System.IDisposable with
            member __.Dispose() =
                opened.Dispose()
                hidden.Dispose()
        }

    let subscribe _model = [ ["window-events"], subscribeToWindowEvents ]

    Program.mkProgram
        (State.init hostWindow)
        (State.update hostWindow)
        View.view
    |> Program.withSubscription subscribe
    |> Program.withHost hostWindow
    |> Program.run

let initAppWindow (window: HostWindow) =
    window.Title <- "Starter"
    window.SystemDecorations <- SystemDecorations.None
    window.Width <- 300
    window.Height <- 35
    window.Background <- Brushes.Transparent
    window.Topmost <- true

    #if DEBUG
    window.AttachDevTools()
    #endif

    startElmish window

type App() =
    inherit Application()

    let displayWindowAndHideIt (window: Window) =
        task {
            let taskCompletionSource = new Tasks.TaskCompletionSource()

            let sub = window.Opened.Subscribe(fun _ ->
                window.Hide()
                taskCompletionSource.TrySetResult() |> ignore
            )

            window.Show()
            do! taskCompletionSource.Task
            sub.Dispose()
        }

    override this.Initialize() =
        this.Styles.Add (FluentAvaloniaTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Light

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop ->
            desktop.ShutdownMode <- ShutdownMode.OnExplicitShutdown

            let hook = new ShortcutListener([KeyCode.VcRightControl; KeyCode.VcSpace])

            // Create window
            let window = HostWindow()
            window |> initAppWindow
            window.Closed.Add(fun _ -> desktop.Shutdown())

            // This fix a bug where the window doesn't have focus the first time it's shown
            // But doesn't fix the bug when published with AOT
            window
            |> displayWindowAndHideIt
            |> ignore

            hook.ShortcutPressed.Add(fun _ ->
                Threading.runInUIThread'
                    Threading.DispatcherPriority.MaxValue
                    (match window.IsActive with
                     | true -> ignore
                     | false -> window.Show)
            )

            hook.Run(CancellationToken.None) |> ignore
            desktop.ShutdownRequested.Add(fun _ -> hook.Dispose())
        | _ -> ()

        base.OnFrameworkInitializationCompleted()

