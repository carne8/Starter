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
        let activated =
            hostWindow.Activated.Subscribe(fun _ ->
                WindowEvent.WindowActivated |> Msg.WindowEvent |> dispatch
            )
        let deactivated =
            hostWindow.Deactivated.Subscribe(fun _ ->
                WindowEvent.WindowDeactivated |> Msg.WindowEvent |> dispatch
            )

        { new System.IDisposable with
            member __.Dispose() =
                activated.Dispose()
                deactivated.Dispose()
        }

    let subscribe _model =
        [ ["window-events"], subscribeToWindowEvents ]

    Program.mkProgram State.init (State.update hostWindow) View.view
    |> Program.withSubscription subscribe
    |> Program.withHost hostWindow
    |> Program.runWith hostWindow

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

