module Starter.Features.PlatformInterop.Linux.Common

open System.Diagnostics
open FsToolkit.ErrorHandling

module Proc =
    let inline startProcess command args =
        ProcessStartInfo(
            FileName = command,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        )
        |> Process.Start
        |> Result.requireNotNull "Failed to start process"

    let inline readProcessOutput command args =
        taskResult {
            use! proc = startProcess command args
            do! proc.WaitForExitAsync()
            return! proc.StandardOutput.ReadToEndAsync()
        }

    let executeCommand command args =
        taskResult {
            use! proc = startProcess command args
            do! proc.WaitForExitAsync()

            match proc.ExitCode with
            | 0 -> return ()
            | exitCode ->
                let error = proc.StandardError.ReadToEndAsync()
                return! Error $"Command failed with exit code {exitCode}: {error}"
        }
