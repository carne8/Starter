[<AutoOpen>]
module Helpers


module Avalonia =
    module Threading =
        open Avalonia.Threading
        let runInUIThread (f: unit -> unit) = Dispatcher.UIThread.Post(f)
        let runInUIThread' priority (f: unit -> unit) = Dispatcher.UIThread.Post(f, priority)

module Elmish =
    open Elmish

    [<RequireQualifiedAccess>]
    module Cmd =
        module Avalonia =
            open Avalonia

            let runUiAction (f: Dispatch<_> -> unit) =
                Cmd.ofEffect (fun dispatch ->
                    (fun _ -> f dispatch)
                    |> Threading.runInUIThread
                )