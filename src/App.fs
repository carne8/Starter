module App

open Elmish
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.ApplicationLifetimes
open Avalonia.FuncUI.DSL
open Avalonia.FuncUI.Hosts
open Avalonia.FuncUI.Elmish

type Model = { Text: string }
type Msg =
    | TextChanged of string

let init () = { Text = "" }, Cmd.none

let update msg model =
    match msg with
    | TextChanged text -> { model with Text = text }, Cmd.none

let view (model: Model) dispatch =
    TextBox.create [
        TextBox.text model.Text
        TextBox.onTextChanged (TextChanged >> dispatch)
    ]

let startElmish (host: IViewHost) =
    Program.mkProgram init update view
    |> Program.withHost host
    |> Program.run


type AppWindow() =
    inherit HostWindow()

    override this.OnInitialized() =
        this.Title <- "Starter"
        this.SystemDecorations <- SystemDecorations.None
        this.Width <- 300
        this.Height <- 30
        this.Show()

        #if DEBUG
        this.AttachDevTools()
        #endif

        startElmish this

type AppControl() =
    inherit HostControl()
    override this.OnInitialized() = startElmish this

type App() =
    inherit Application()

    override this.OnFrameworkInitializationCompleted() =
        match this.ApplicationLifetime with
        | :? IClassicDesktopStyleApplicationLifetime as desktop -> desktop.MainWindow <- new AppWindow()
        | :? ISingleViewApplicationLifetime as singleView -> singleView.MainView <- new AppControl()
        | _ -> ()

        base.OnFrameworkInitializationCompleted()

