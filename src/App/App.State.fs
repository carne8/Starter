namespace Starter.App

open Starter.App
open Elmish
open Avalonia.Controls

module Cmds =
    let resetTextBox model =
        Cmd.Avalonia.runUiAction (fun _ ->
            match model.TextBox with
            | None -> ()
            | Some textBox ->
                textBox.Focus() |> ignore
                textBox.SelectAll()
        )

    let hideWindow (window: Window) =
        Cmd.Avalonia.runUiAction (ignore >> window.Hide)

module State =
    let init (_window: Window) () =
        { Text = "Hello"
          TextBox = None },
        Cmd.none

    let update (window: Window) msg model =
        match msg with
        // Subscriptions
        | Msg.WindowEvent (WindowEvent.WindowOpened) -> model, Cmds.resetTextBox model
        | Msg.WindowEvent (WindowEvent.WindowHidden) -> model, Cmds.hideWindow window

        | Msg.SetTextBox textBox -> { model with TextBox = Some textBox }, Cmd.none
        | Msg.TextChanged text -> { model with Text = text }, Cmd.none