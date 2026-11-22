[<AutoOpen>]
module Starter.Spotify.Helpers

open System.Net.Http.Json

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

type System.Net.Http.HttpContent with
    member inline this.TryReadFromJsonAsync<'a>() =
        task {
            try
                match! this.ReadFromJsonAsync<'a>() with
                | null ->
                    let error = this.ReadAsStringAsync()
                    return Error $"Failed to parse response: {error}"
                | json -> return Ok json

            with exn -> return Error exn.Message
        }
