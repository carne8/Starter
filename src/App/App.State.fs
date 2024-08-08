namespace Starter.App

open Starter.App
open Elmish

module State =
    let init () = { Text = "" }, Cmd.none

    let update msg model =
        match msg with
        | TextChanged text -> { model with Text = text }, Cmd.none