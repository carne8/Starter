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

type AppWindow() =
    inherit HostWindow()

    override this.OnInitialized() =
        this.Title <- "Starter"
        this.SizeToContent <- SizeToContent.WidthAndHeight
        this.ShowInTaskbar <- false
        this.SystemDecorations <- SystemDecorations.None
        this.Topmost <- true

        this.Deactivated.Add(fun _ -> this.Hide())

        #if DEBUG
        this.AttachDevTools()
        #endif

        startElmish this

type App() =
    inherit Application()

    let mutable window: AppWindow option = None

    let showWindow() =
        Threading.runInUIThread'
            Threading.DispatcherPriority.MaxValue
            (fun _ ->
                match window with
                | Some w when w.IsVisible -> () // Don't show the window if it's already shown
                | _ ->
                    window <- new AppWindow() |> Some
                    window.Value.Show()
            )

    let initializeHook (desktop: IClassicDesktopStyleApplicationLifetime) =
        let hook = new ShortcutListener([KeyCode.VcRightControl; KeyCode.VcSpace])

        hook.ShortcutPressed.Add showWindow
        desktop.ShutdownRequested.Add (fun _ -> hook.Dispose())

        hook.Run(CancellationToken.None) |> ignore

    override this.Initialize() =
        this.Styles.Add (FluentAvaloniaTheme())
        this.RequestedThemeVariant <- Styling.ThemeVariant.Light

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop -> initializeHook desktop
        | _ -> ()

        base.OnFrameworkInitializationCompleted()

