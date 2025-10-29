module Starter.ApplicationSearchEngine.Linux.IconLoader

open System
open System.Collections.Generic
open System.Threading.Tasks
open Avalonia.Media.Imaging
open Avalonia.Threading
open Avalonia.Svg.Skia
open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Logger
open Starter.SearchEngine
open System.IO
open System.Text.RegularExpressions
open Svg.Model.Drawables.Elements

let private directoryIconSizeRegex = Regex(@"\/(\d+)x\d+(?:@\d)?\/", RegexOptions.Compiled)

module private StarterIconSource =
    let fromSvgFile svgPath =
        Dispatcher.UIThread.InvokeAsync(fun () ->
            SvgImage(Source = SvgSource.Load svgPath)
        )
        |> _.GetTask()
        |> Task.map (fun svg -> StarterIconSource(svg, svg))

    let fromBitmapFile bmp = StarterIconSource(bmp, bmp)

module IconThemeIndex =
    // #r "nuget: FsToolkit.ErrorHandling, 5.1.0"
    // open FsToolkit.ErrorHandling
    // open System.IO
    // open System.Collections.Generic
    
    [<Struct>]
    type IconType =
        | Fixed
        | Scalable
        | Threshold
    
    [<Struct>]
    type IconFolderInfo =
        { Size: int
          Scale: int
          MinSize: int
          MaxSize: int
          Threshold: int
          Type: IconType }
    
    let getIconDirectories (theme: string) =
        taskOption {
            let! indexContent = theme |> File.ReadAllLinesAsync
            let! directoriesLineIdx = indexContent |> Array.tryFindIndex _.StartsWith("Directories")
            let directories =
                indexContent[directoriesLineIdx]
                    .Remove(0, "Directories=".Length)
                    .Split(',', StringSplitOptions.TrimEntries ||| StringSplitOptions.RemoveEmptyEntries)
                |> Array.map (fun d ->
                    KeyValuePair(
                        d,
                        struct {| Size = ValueNone
                                  Scale = ValueNone
                                  MinSize = ValueNone
                                  MaxSize = ValueNone
                                  Threshold = ValueNone
                                  Type = ValueNone |}))
                |> Dictionary
                
            let mutable currentDirInfo = ValueNone
            
            for lineIdx in directoriesLineIdx+1..indexContent.Length-1 do
                let line = indexContent[lineIdx]
                if line.StartsWith '[' && line.EndsWith ']' then // Starts of a section
                    let name = line.Substring(1, line.Length-2)                    
                    if directories.ContainsKey name then
                        currentDirInfo <- ValueSome name
                else
                    match currentDirInfo with // Content of a section
                    | ValueNone -> ()
                    | ValueSome dirName ->
                        if line.StartsWith "Context" then
                            let context = line.Substring "Context=".Length
                            if context <> "Applications" then
                                directories.Remove(dirName) |> ignore
                                currentDirInfo <- ValueNone
                        elif line.StartsWith "Size" then
                            let size = line.Substring "Size=".Length
                            directories[dirName] <- {| directories[dirName] with Size = ValueSome <| int size |}
                        elif line.StartsWith "Scale" then
                            let scale = line.Substring "Scale=".Length
                            directories[dirName] <- {| directories[dirName] with Scale = ValueSome <| int scale |} 
                        elif line.StartsWith "MinSize" then
                            let minSize = line.Substring "MinSize=".Length
                            directories[dirName] <- {| directories[dirName] with MinSize = ValueSome <| int minSize |} 
                        elif line.StartsWith "MaxSize" then
                            let maxSize = line.Substring "MaxSize=".Length
                            directories[dirName] <- {| directories[dirName] with MaxSize = ValueSome <| int maxSize |}
                        elif line.StartsWith "Type" then
                            let type' = 
                                match line.Substring "Type=".Length with
                                | "Fixed" -> Fixed
                                | "Scalable" -> Scalable
                                | "Threshold" | _ -> Threshold
                            
                            directories[dirName] <- {| directories[dirName] with Type = ValueSome type' |}
                    
            return directories |> Seq.choose (fun kv ->
                match kv.Value.Size with
                | ValueNone -> None
                | ValueSome size ->
                    let directoryInfo =
                        { Size = size
                          Scale = kv.Value.Scale |> ValueOption.defaultValue 1
                          MinSize = kv.Value.MinSize |> ValueOption.defaultValue size
                          MaxSize = kv.Value.MaxSize |> ValueOption.defaultValue size
                          Threshold = kv.Value.Threshold |> ValueOption.defaultValue 2
                          Type = kv.Value.Type |> ValueOption.defaultValue Threshold }
                        
                    let directoryPath =
                        Path.Combine(theme |> Path.GetDirectoryName, kv.Key)
                        
                    KeyValuePair(directoryPath, directoryInfo) |> Some
            )
        }
        
    let getParentThemes (theme: string) =
        task {
            let indexContent = theme |> File.ReadLinesAsync
            let enumerator = indexContent.GetAsyncEnumerator()
            
            let mutable parentTheme = ValueNone
            while! match parentTheme with
                   | ValueNone -> enumerator.MoveNextAsync()
                   | ValueSome _ -> ValueTask.FromResult false
                do
                let line = enumerator.Current
                if line.StartsWith "Inherits" then
                    parentTheme <- ValueSome <| line.Remove(0, "Inherits=".Length)
            
            return // Returns the parent index.theme files 
                parentTheme
                |> ValueOption.map (fun themeNames ->
                    let themesDirectory = theme |> Path.GetDirectoryName |> Path.GetDirectoryName 
                    themeNames.Split ','
                    |> Seq.collect (fun themeName ->
                        Directory.EnumerateFiles(
                            Path.Combine(themesDirectory, themeName),
                            "index.theme",
                            SearchOption.AllDirectories
                        )
                    )
                    |> Seq.toArray
                )
                |> ValueOption.defaultWith (fun () -> Array.empty)
        }
        
    let directoryMatchesSize size scale directory =
        let d = directory
        if d.Scale = scale then false
        else
            match d.Type with
            | Fixed -> d.Size = size
            | Scalable -> d.MinSize <= size && size <= d.MaxSize
            | Threshold -> d.Size - d.Threshold <= size && size <= d.Size + d.Threshold
            
    let directorySizeDistance isize iscale dir =
        let { Scale = scale
              Size = size
              MinSize = minSize
              MaxSize = maxSize
              Threshold = threshold } = dir
        
        match dir.Type with
        | Fixed -> abs (size * scale - isize * iscale)
        | Scalable ->
            if isize * iscale < minSize * scale then
                minSize * scale - isize * iscale
            elif isize * iscale > maxSize * scale then
                isize * iscale - maxSize * scale
            else 0
        | Threshold ->
            if isize * iscale < (size - threshold) * scale then
                minSize * scale - isize * iscale
            elif isize * iscale > (size + threshold) * scale then
                isize * iscale - maxSize * scale
            else 0

    let findIcon size scale icon theme =
        printfn "%A" theme
        taskOption {
            let! directories = theme |> getIconDirectories
            let inline exactSizeMatch () =
                directories
                |> Seq.filter (_.Value >> directoryMatchesSize size scale)
                |> Seq.collect (fun d ->
                    [ $"{d.Key}/{icon}.svg"
                      $"{d.Key}/{icon}.png"
                      $"{d.Key}/{icon}.xpm" ]
                    |> List.filter Path.Exists
                )
                |> Seq.tryHead
            
            let inline closestSizeMatch () =
                directories |> Seq.fold
                    (fun struct (minSize, closestFile) kv ->
                        let sizeDistance = kv.Value |> directorySizeDistance size scale
                        let inline findIconFile () =
                            [ $"{kv.Key}/{icon}.svg"
                              $"{kv.Key}/{icon}.png"
                              $"{kv.Key}/{icon}.xpm" ]
                            |> List.filter Path.Exists
                            |> List.tryHead
                            
                        if sizeDistance < minSize then
                            match findIconFile () with
                            | None -> struct (minSize, closestFile)
                            | Some file -> struct (sizeDistance, ValueSome file)
                        else struct (minSize, closestFile)
                    )
                    struct (Int32.MaxValue, ValueNone)
                |> fun struct (_, file) -> file
                |> ValueOption.toOption
            
            match exactSizeMatch() with
            | None -> return! closestSizeMatch()
            | Some file -> return file
        }

let rec private findIconInTheme size scale iconName (theme: string) =
    task {
        let! icon = theme |> IconThemeIndex.findIcon size scale iconName
        match icon with
        | Some icon -> return Some icon
        | None ->
            let! parentThemes = theme |> IconThemeIndex.getParentThemes
            let mutable foundIcon = None
            let mutable i = 0
            while foundIcon.IsNone && i < parentThemes.Length do
                let! iconOpt = parentThemes[i] |> findIconInTheme size scale iconName
                foundIcon <- iconOpt
                i <- i + 1

            return foundIcon
    }

let defaultTheme = "default"

let private iconLookup (folders: FolderConfiguration) size scale icon =
    task {
        let themes =
            folders.Folders
            |> Array.map (fun folder ->
                let folder = Path.Combine(folder, "icons")
                Path.Combine(folder, defaultTheme, "index.theme")
            )
            |> fun arr ->
                Array.concat [|
                    [| Environment.SpecialFolder.UserProfile
                       |> Environment.GetFolderPath
                       |> fun d -> Path.Combine(d, ".icons", defaultTheme, "index.theme") |]
                    arr
                    [| $"/usr/share/pixmaps/{defaultTheme}/index.theme" |]
                |]
            |> Array.filter (fun theme ->
                theme |> FolderConfiguration.isFileExcluded folders |> not
                && theme |> Path.Exists
            )
        
        let mutable i = 0
        let mutable foundIcon = None
        while i < themes.Length do
            let! icon = themes[i] |> findIconInTheme size scale icon
            foundIcon <- icon
            i <- i + 1
            
        return foundIcon
    }
    
// TODO: Rebuild using -> https://specifications.freedesktop.org/icon-theme-spec/latest/
// $HOME/.icons
// $XDG_DATA_DIRS/icons
// /usr/share/pixmaps

// let private findIconFile (folderConfig: FolderConfiguration) iconName = // TODO: Take care of using the folderConfig
//     match iconName |> File.Exists with
//     | true -> Some iconName
//     | false ->
//         let files =
//             seq {
//                 if Directory.Exists "/usr/share/icons" then Directory.EnumerateFiles("/usr/share/icons", $"{iconName}.*", SearchOption.AllDirectories)
//                 if Directory.Exists "/usr/share/pixmaps" then Directory.EnumerateFiles("/usr/share/pixmaps", $"{iconName}.*", SearchOption.AllDirectories)
//                 if Directory.Exists "/var/lib/flatpak/exports/share/icons/" then Directory.EnumerateFiles("/var/lib/flatpak/exports/share/icons/", $"{iconName}.*", SearchOption.AllDirectories)
//             }
//             |> Seq.concat
//
//         let svgFile =
//             files |> Seq.tryFind (fun path ->
//                 path |> Path.GetExtension = ".svg"
//                 && path |> Path.GetFileNameWithoutExtension = iconName
//             )
//
//         match svgFile with
//         | Some file -> Some file
//         | None ->
//             files
//             |> Seq.filter (Path.GetExtension >> (<>) ".svg")
//             |> Seq.sortByDescending (fun path ->
//                 let match' = directoryIconSizeRegex.Match(path)
//                 if not match'.Success then 0 else int match'.Groups[1].Value
//             )
//             |> Seq.tryHead

let loadAppIcon folderConfig (iconName: string) =
    taskOption {
        let! iconFile = iconLookup folderConfig 128 1 iconName
        
        match iconFile |> Path.GetExtension with
        | ".svg" -> return! iconFile |> StarterIconSource.fromSvgFile
        | _ -> return new Bitmap(iconFile) |> StarterIconSource.fromBitmapFile
    }
    |> Task.catch
    |> Task.map (function
        | Choice1Of2 opt -> opt
        | Choice2Of2 exn ->
            logger.Warning $"Failed to load icon {iconName}: {exn}"
            None
    )
