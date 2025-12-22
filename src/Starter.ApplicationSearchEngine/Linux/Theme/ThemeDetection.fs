module Starter.ApplicationSearchEngine.Linux.Theme.ThemeDetection

open System
open System.IO
open System.Threading.Tasks
open System.Diagnostics

open FsToolkit.ErrorHandling

[<Struct>]
type DesktopEnvironment =
    | Gnome
    | KDE
    | Xfce
    | Cinnamon
    | Mate
    | Budgie
    | Deepin
    | LXDE
    | LXQt
    | Enlightenment
    | Unknown

/// Run a command and capture its output
let private runCommand (command: string) args : Task<string option> =
    task {
        try
            use proc =
                ProcessStartInfo(
                    FileName = command,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                )
                |> Process.Start

            let! output = proc.StandardOutput.ReadToEndAsync()
            do! proc.WaitForExitAsync()

            if proc.ExitCode = 0 && not (String.IsNullOrWhiteSpace output) then
                return Some <| output.Trim().Trim('\'', '"') // Remove quotes and trim
            else
                return None
        with
        | _ -> return None
    }

/// Parse INI-style config file
let private parseIniFile (path: string) (section: string) (key: string) : string option =
    try
        if not (File.Exists path) then None else

        let lines = File.ReadAllLines path
        let mutable inSection = false
        let mutable result = None

        for line in lines do
            let line = line.Trim()

            // Check for section header
            if line.StartsWith "[" && line.EndsWith "]" then
                let sectionName = line.Substring(1, line.Length - 2)
                inSection <- sectionName = section

            // Check for key-value pair in correct section
            elif inSection && line.Contains "=" then
                let parts = line.Split('=', 2)
                if parts.Length = 2 && parts[0].Trim() = key then
                    result <- Some (parts[1].Trim().Trim('\'', '"'))

        result
    with _ -> None

/// Detect desktop environment from environment variables
let private detectDesktopEnvironment () =
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

    // Check for specific desktop environments
    if xdgCurrent.Contains "gnome" || xdgSession.Contains "gnome" then Gnome
    elif xdgCurrent.Contains "kde" || xdgSession.Contains "plasma" then KDE
    elif xdgCurrent.Contains "xfce" || xdgSession.Contains "xfce" then Xfce
    elif xdgCurrent.Contains "cinnamon" || xdgSession.Contains "cinnamon" then Cinnamon
    elif xdgCurrent.Contains "mate" || xdgSession.Contains "mate" then Mate
    elif xdgCurrent.Contains "budgie" || xdgSession.Contains "budgie" then Budgie
    elif xdgCurrent.Contains "deepin" || xdgSession.Contains "deepin" then Deepin
    elif xdgCurrent.Contains "lxde" || xdgSession.Contains "lxde" then LXDE
    elif xdgCurrent.Contains "lxqt" || xdgSession.Contains "lxqt" then LXQt
    elif xdgCurrent.Contains "enlightenment" || xdgSession.Contains "enlightenment" then Enlightenment
    else Unknown

let private getIconTheme de =
    match de with
    | Gnome | Budgie -> runCommand "gsettings" "get org.gnome.desktop.interface icon-theme"
    | Xfce -> runCommand "xfconf-query" "-c xsettings -p /Net/IconThemeName"
    | Cinnamon -> runCommand "gsettings" "get org.cinnamon.desktop.interface icon-theme"
    | Mate -> runCommand "gsettings" "get org.mate.desktop.interface icon-theme"
    | Deepin -> runCommand "gsettings" "get com.deepin.dde.appearance icon-theme"
    | KDE ->
        task {
            // Try kreadconfig6 first (Plasma 6)
            let! result = runCommand "kreadconfig6" "--group Icons --key Theme"
            match result with
            | Some theme -> return Some theme
            | None ->
                // Fall back to kreadconfig5 (Plasma 5)
                return! runCommand "kreadconfig5" "--group Icons --key Theme"
        }

    | LXDE | LXQt ->
        // Try LXQt config
        let lxqtConf =
            Path.Combine(
                Environment.GetFolderPath Environment.SpecialFolder.UserProfile,
                ".config", "lxqt", "lxqt.conf"
            )

        if File.Exists lxqtConf then
            try
                lxqtConf
                |> File.ReadAllLines
                |> Array.tryPick (fun line ->
                    if line.ToLowerInvariant().Contains "icon_theme" && line.Contains "=" then
                        let parts = line.Split('=', 2)
                        if parts.Length = 2 then
                            parts[1].Trim().Trim('\'', '"') |> Some
                        else None
                    else None
                )
            with
            | _ -> None
            |> Task.singleton
        else None |> Task.singleton

    | Enlightenment
    | Unknown -> Task.singleton None

/// Get icon theme from GTK config files
let private getGtkIconTheme () =
    let homeDir = Environment.GetFolderPath Environment.SpecialFolder.UserProfile

    orElse {
        // Try GTK-4
        let gtk4Settings = Path.Combine(homeDir, ".config", "gtk-4.0", "settings.ini")
        return! parseIniFile gtk4Settings "Settings" "gtk-icon-theme-name"

        // Try GTK-3
        let gtk3Settings = Path.Combine(homeDir, ".config", "gtk-3.0", "settings.ini")
        return! parseIniFile gtk3Settings "Settings" "gtk-icon-theme-name"

        // Try GTK-2
        let gtk2Settings = Path.Combine(homeDir, ".gtkrc-2.0")
        if File.Exists gtk2Settings then
            return!
                try gtk2Settings
                    |> File.ReadAllLines
                    |> Array.tryPick (fun line ->
                        if line.Contains "gtk-icon-theme-name" && line.Contains "=" then
                            let parts = line.Split('=', 2)
                            if parts.Length = 2 then
                                Some <| parts[1].Trim().Trim('\'', '"')
                            else
                                None
                        else
                            None
                    )
                with _ -> None
        else
            return! None
    }

/// Detect the current icon theme
let getCurrentIconTheme () : Task<string> =
    task {
        // Try desktop-specific method first
        let! desktopTheme = detectDesktopEnvironment() |> getIconTheme

        return orElse {
            return! desktopTheme

            // Try GTK config as fallback
            return! getGtkIconTheme()

            // Try environment variable
            return! Environment.GetEnvironmentVariable "ICON_THEME" |> Option.ofObj
        }
        |> Option.defaultValue "hicolor"
    }