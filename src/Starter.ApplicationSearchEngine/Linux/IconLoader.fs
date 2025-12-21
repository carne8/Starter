module Starter.ApplicationSearchEngine.Linux.IconLoader

open Avalonia.Media.Imaging
open Avalonia.Threading
open Avalonia.Svg.Skia

open FsToolkit.ErrorHandling
open Starter.SearchEngine
open Starter.ApplicationSearchEngine

open System
open System.IO
open System.Threading.Tasks
open System.Diagnostics
open System.Text.RegularExpressions


type OrElseBuilder() = // TODO: Benchmark for the fun
    member _.Return x = Some x
    member _.ReturnFrom x = x
    member _.Combine(a,b) =
        match a with
        | Some _ -> a
        | None -> b()
    member _.Delay f = f
    member _.Run f = f()

let orElse = OrElseBuilder()

[<Struct>]
type IconType =
    | Fixed
    | Scalable
    | Threshold

    static member ofString =
        function
        | "Fixed" -> Fixed
        | "Scalable" -> Scalable
        | _ -> Threshold

[<Struct>]
type IconThemeDirectory =
    { Path: string
      Size: int
      Scale: int
      MinSize: int
      MaxSize: int
      Threshold: int
      Type: IconType }

    /// Check if directory matches the requested size exactly
    static member matchesSize (iconSize: int) (iconScale: int) (dirInfo: IconThemeDirectory) : bool =
        if dirInfo.Scale <> iconScale then
            false
        else
            match dirInfo.Type with
            | Fixed -> dirInfo.Size = iconSize
            | Scalable -> dirInfo.MinSize <= iconSize && iconSize <= dirInfo.MaxSize
            | Threshold ->
                dirInfo.Size - dirInfo.Threshold <= iconSize &&
                iconSize <= dirInfo.Size + dirInfo.Threshold

    /// Calculate size distance for closest match
    static member getSizeDistance (iconSize: int) (iconScale: int) (dirInfo: IconThemeDirectory) : int =
        let size = dirInfo.Size
        let scale = dirInfo.Scale

        match dirInfo.Type with
        | Fixed ->
            abs (size * scale - iconSize * iconScale)
        | Scalable ->
            let minSize = dirInfo.MinSize
            let maxSize = dirInfo.MaxSize
            if iconSize * iconScale < minSize * scale then
                minSize * scale - iconSize * iconScale
            elif iconSize * iconScale > maxSize * scale then
                iconSize * iconScale - maxSize * scale
            else
                0
        | Threshold ->
            let threshold = dirInfo.Threshold
            if iconSize * iconScale < (size - threshold) * scale then
                (size - threshold) * scale - iconSize * iconScale
            elif iconSize * iconScale > (size + threshold) * scale then
                iconSize * iconScale - (size + threshold) * scale
            else
                0

[<Struct>]
type IconTheme =
    { Name: string
      Path: string
      Directories: IconThemeDirectory array
      ParentThemes: string array }

    /// Parse the index.theme file and return directory information
    static member parseIndexTheme (indexPath: string) : Task<IconTheme option> =
        task {
            if not (File.Exists indexPath) then return None else

            let lines = indexPath |> File.ReadLinesAsync |> _.GetAsyncEnumerator()
            let mutable currentSection = ""

            // Icon theme properties
            let path = indexPath |> Path.GetDirectoryName
            let mutable themeName = String.Empty
            let mutable dirNames = Array.empty
            let mutable scaledDirNames = Array.empty
            let mutable parentThemes = Array.empty
            let directories = ResizeArray()

            // Directory properties
            let mutable size = ValueNone
            let mutable scale = ValueNone
            let mutable minSize = ValueNone
            let mutable maxSize = ValueNone
            let mutable threshold = ValueNone
            let mutable iconType = Threshold

            while! lines.MoveNextAsync() do
                let line = lines.Current.Trim()
                let shouldSkipLine = line.StartsWith "#" || String.IsNullOrWhiteSpace line // Skip comments and empty lines

                if shouldSkipLine then () else

                // Section header
                if line.StartsWith "[" then
                    if line <> "[Icon Theme]" then // If directory section
                        match size with
                        | ValueNone -> ()
                        | ValueSome size ->
                            { Path = Path.Combine(path, currentSection.Substring(1, currentSection.Length-2))
                              Size = size
                              Scale = scale |> ValueOption.defaultValue 1
                              MinSize = minSize |> ValueOption.defaultValue size
                              MaxSize = maxSize |> ValueOption.defaultValue size
                              Threshold = threshold |> ValueOption.defaultValue 2
                              Type = iconType }
                            |> directories.Add

                        size <- ValueNone
                        scale <- ValueNone
                        minSize <- ValueNone
                        maxSize <- ValueNone
                        threshold <- ValueNone
                        iconType <- Threshold

                    currentSection <- line

                // Icon theme section
                elif currentSection = "[Icon Theme]" && line.Contains "=" then
                    match line.Split('=', 2) with
                    | [| key; value |] ->
                        let key = key.Trim()
                        let value = value.Trim()

                        match key with
                        | "Name" -> themeName <- value
                        | "Directories" -> dirNames <- value.Split ',' |> Array.map _.Trim()
                        | "ScaledDirectories" -> scaledDirNames <- value.Split ',' |> Array.map _.Trim()
                        | "Inherits" -> parentThemes <- value.Split ',' |> Array.map _.Trim()
                        | _ -> ()
                    | _ -> Logger.logger.Warning $"Failed to parse line of theme manifest {indexPath}: \"{line}\""

                // Directory section
                elif line.Contains "=" then
                    match line.Split('=', 2) with
                    | [| key; value |] ->
                        let key = key.Trim()
                        let value = value.Trim()

                        match key with
                        | "Size" -> size <- value |> Int32.TryParse |> ValueOption.ofPair
                        | "Scale" -> scale <- value |> Int32.TryParse |> ValueOption.ofPair
                        | "MinSize" -> minSize <- value |> Int32.TryParse |> ValueOption.ofPair
                        | "MaxSize" -> maxSize <- value |> Int32.TryParse |> ValueOption.ofPair
                        | "Threshold" -> threshold <- value |> Int32.TryParse |> ValueOption.ofPair
                        | "Type" -> iconType <- value |> IconType.ofString
                        | _ -> ()
                    | _ -> Logger.logger.Warning $"Failed to parse line of theme manifest {indexPath}: \"{line}\""

            if themeName |> String.IsNullOrEmpty then return None else
            return Some { Name = themeName.ToLowerInvariant()
                          Path = path
                          Directories = directories.ToArray()
                          ParentThemes = parentThemes }
        }

    static member parseFromDirectory (directory: string) =
        let directoryRegex = Regex(@"(\d+)x\d+@?(\d)?", RegexOptions.Compiled)
        let inline parseDirectory (directory: string) =
            match directory with
            | dir when dir.EndsWith "scalable" ->
                dir
                |> Directory.GetDirectories
                |> Array.map (fun dir ->
                    { Path = dir
                      Scale = 1
                      Size = 16
                      MinSize = 8
                      MaxSize = 512
                      Threshold = 1
                      Type = Scalable }
                )
                |> Some
            | dir ->
                option {
                    let match' = directoryRegex.Match dir
                    match match'.Success, match'.Groups.Count with
                    | true, 2
                    | true, 3 ->
                        let! size = match'.Groups[1].Value |> ValueOption.tryParse
                        let scale =
                            match match'.Groups.Count = 3 with
                            | false -> 1
                            | true ->
                                match'.Groups[2].Value
                                |> ValueOption.tryParse
                                |> ValueOption.defaultValue 1

                        let themeDir =
                            { Path = dir
                              Size = size
                              Scale = scale
                              MinSize = size
                              MaxSize = size
                              Threshold = 2
                              Type = Scalable }

                        return
                            dir
                            |> Directory.GetDirectories
                            |> Array.map (fun dir -> { themeDir with Path = dir })

                    | _ -> return! None
                }

        { Name = directory |> Path.GetFileName
          Path = directory
          Directories =
            directory
            |> Directory.GetDirectories
            |> Array.choose parseDirectory
            |> Array.concat
          ParentThemes = Array.empty }


module private IconLookup =
    type LookupThemeTree =
        | RootTheme of IconTheme
        | ChildTheme of IconTheme * LookupThemeTree array

    let extensions = [| "svg"; "png"; "xpm" |]

    /// Directories to search for icons
    let private iconDirectories =
        let xdgDataDirs =
            Environment.GetEnvironmentVariable "XDG_DATA_DIRS"
            |> Option.ofObj
            |> Option.defaultValue "/usr/local/share:/usr/share"

        [| // $HOME/.icons
           Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.UserProfile, ".icons")

           // $HOME/.local/share/icons
           Path.Combine(Environment.GetFolderPath Environment.SpecialFolder.UserProfile, ".local", "share", "icons")

           // $XDG_DATA_DIRS/icons
           for dataDir in xdgDataDirs.Split ':' do Path.Combine(dataDir, "icons") |]
        |> Array.filter Directory.Exists

    let private fallbackThemes =
        iconDirectories
        |> Array.map (fun dir -> Path.Combine(dir, "hicolor"))
        |> Array.choose (fun themePath ->
            match Directory.Exists themePath with
            | false -> None
            | true ->
                themePath
                |> IconTheme.parseFromDirectory
                |> Some
        )

    let rec loadIconTheme iconThemeName : LookupThemeTree array Task =
        iconDirectories
        |> Array.choose (fun iconDir ->
            let indexPath = Path.Combine(iconDir, iconThemeName, "index.theme")
            match File.Exists indexPath with
            | true -> Some indexPath
            | false -> None
        )
        |> Array.map IconTheme.parseIndexTheme
        |> Task.WhenAll
        |> Task.bind (
            Array.choose (function
                | None -> None
                | Some ({ ParentThemes = [||] } as theme) ->
                    theme
                    |> RootTheme
                    |> Task.singleton
                    |> Some
                | Some theme ->
                    task {
                        let parents = ResizeArray theme.ParentThemes.Length
                        for parentTheme in theme.ParentThemes do
                            let! theme = loadIconTheme parentTheme
                            parents.AddRange theme

                        return ChildTheme(theme, parents.ToArray())
                    }
                    |> Some
            )
            >> Task.WhenAll
        )

    /// Look up icon in a specific theme
    let private lookupIconInTheme iconName size scale theme =
        let inline fileExists directory iconName extension =
            let iconPath = Path.Combine(directory, $"{iconName}.{extension}")
            match File.Exists iconPath with
            | true -> Some iconPath
            | false -> None

        // First pass: exact size match
        let exactMatch = theme.Directories |> Array.tryPick (fun dir ->
            match dir |> IconThemeDirectory.matchesSize size scale with
            | false -> None
            | true -> extensions |> Array.tryPick (fileExists dir.Path iconName)
        )

        match exactMatch with
        | Some path -> Some path
        | None ->
            // Second pass: closest size match
            theme.Directories
            |> Array.fold
                (fun struct (minDistance, closestPath) dir ->
                    let distance = dir |> IconThemeDirectory.getSizeDistance size scale

                    match distance < minDistance with
                    | false -> struct (minDistance, closestPath)
                    | true ->
                        extensions
                        |> Array.tryPick (fileExists dir.Path iconName)
                        |> function
                            | Some path -> struct (distance, ValueSome path)
                            | None -> struct (minDistance, closestPath)
                )
                struct (Int32.MaxValue, ValueNone)
            |> fun struct (_, closestPath) -> closestPath |> ValueOption.toOption

    let rec searchIconInTree iconName size scale themeTree =
        match themeTree with
        | RootTheme theme -> lookupIconInTheme iconName size scale theme
        | ChildTheme (theme, parents) ->
            orElse {
                return! lookupIconInTheme iconName size scale theme
                return! parents |> Array.tryPick (searchIconInTree iconName size scale)
                return! fallbackThemes |> Array.tryPick (lookupIconInTheme iconName size scale)
                return!
                    extensions
                    |> Array.tryPick (fun ext ->
                        Directory.EnumerateFiles("/usr/share/pixmaps", $"{iconName}.{ext}")
                        |> Seq.tryHead
                    )
            }

module private IconThemeDetection =
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


module private StarterIconSource =
    let fromSvgFile svgPath =
        Dispatcher.UIThread.InvokeAsync(fun () ->
            SvgImage(Source = SvgSource.Load svgPath)
        )
        |> _.GetTask()
        |> Task.map (fun svg -> StarterIconSource(svg, svg))

    let fromBitmapFile bmp = StarterIconSource(bmp, bmp)

let private themeTrees =
    IconThemeDetection.getCurrentIconTheme()
    |> Task.bind IconLookup.loadIconTheme

let loadAppIcon (iconName: string) =
    taskOption {
        let! themeTrees = themeTrees
        let! iconFile =
            match iconName |> Path.IsPathFullyQualified with
            | true -> Some iconName
            | false -> themeTrees |> Array.tryPick (IconLookup.searchIconInTree iconName 128 1)

        match iconFile |> Path.GetExtension with
        | ".svg" -> return! iconFile |> StarterIconSource.fromSvgFile
        | _ -> return new Bitmap(iconFile) |> StarterIconSource.fromBitmapFile
    }
    |> Task.catch
    |> Task.map (function
        | Choice1Of2 opt -> opt
        | Choice2Of2 exn ->
            Logger.logger.Warning $"Failed to load icon {iconName}: {exn}"
            None
    )
