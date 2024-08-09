namespace Starter.App

[<RequireQualifiedAccess>]
type WindowEvent =
    | WindowActivated
    | WindowDeactivated

type Model =
    { Text: string
      TextBox: Avalonia.Controls.TextBox option }

[<RequireQualifiedAccess>]
type Msg =
    | SetTextBox of Avalonia.Controls.TextBox
    | WindowEvent of WindowEvent
    | TextChanged of string
