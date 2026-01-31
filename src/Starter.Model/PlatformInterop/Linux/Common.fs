namespace Starter.Features.PlatformInterop.Linux

open System
open System.Diagnostics
open FsToolkit.ErrorHandling
open Helpers

type DesktopEnvironment =
    | Gnome
    | KDE
    | Unknown
    // | XFCE
    // | Cinnamon
    // | MATE
    // | Budgie
    // | Deepin
    // | LXDE
    // | LXQt
    // | Enlightenment

    static member detectDesktopEnvironment () =
        let xdgCurrent =
            Environment.GetEnvironmentVariable "XDG_CURRENT_DESKTOP"
            |> Option.ofObj
            |> Option.map _.ToLowerInvariant()
            |> Option.defaultValue ""

        let xdgSession =
            Environment.GetEnvironmentVariable "XDG_SESSION_DESKTOP"
            |> Option.ofObj
            |> Option.map _.ToLowerInvariant()
            |> Option.defaultValue ""

        if xdgCurrent.Contains "gnome" || xdgSession.Contains "gnome" then Gnome
        elif xdgCurrent.Contains "kde" || xdgSession.Contains "plasma" then KDE
        else Unknown
        // elif xdgCurrent.Contains "xfce" || xdgSession.Contains "xfce" then XFCE
        // elif xdgCurrent.Contains "cinnamon" || xdgSession.Contains "cinnamon" then Cinnamon
        // elif xdgCurrent.Contains "mate" || xdgSession.Contains "mate" then MATE
        // elif xdgCurrent.Contains "budgie" || xdgSession.Contains "budgie" then Budgie
        // elif xdgCurrent.Contains "deepin" || xdgSession.Contains "deepin" then Deepin
        // elif xdgCurrent.Contains "lxde" || xdgSession.Contains "lxde" then LXDE
        // elif xdgCurrent.Contains "lxqt" || xdgSession.Contains "lxqt" then LXQt
        // elif xdgCurrent.Contains "enlightenment" || xdgSession.Contains "enlightenment" then Enlightenment

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
