[<AutoOpen>]
module Starter.Spotify.Helpers

module Process =
    open System.Diagnostics

    let run proc args =
        ProcessStartInfo(
            FileName = proc,
            Arguments = args,
            UseShellExecute = true
        )
        |> Process.Start
        |> function null -> () | d -> d.Dispose()
