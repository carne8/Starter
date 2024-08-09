module Starter.App.View

open Starter.App
open Avalonia.Controls
open Avalonia.FuncUI.DSL

let view (model: Model) dispatch =
    TextBox.create [
        TextBox.init (Msg.SetTextBox >> dispatch)

        TextBox.margin 5.
        TextBox.text model.Text
        TextBox.onTextChanged (Msg.TextChanged >> dispatch)
    ]