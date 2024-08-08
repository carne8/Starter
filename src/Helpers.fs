[<AutoOpen>]
module Helpers


module Avalonia =
    module Threading =
        open Avalonia.Threading
        let runInUIThread (f: unit -> unit) = Dispatcher.UIThread.Post(f)
        let runInUIThread' priority (f: unit -> unit) = Dispatcher.UIThread.Post(f, priority)