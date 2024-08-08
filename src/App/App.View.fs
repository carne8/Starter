module Starter.App.View

open Starter.App
open Avalonia.Controls
open Avalonia.FuncUI.DSL

let view (model: Model) dispatch =
    TextBox.create [
        TextBox.text model.Text
        TextBox.onTextChanged (TextChanged >> dispatch)
    ]
