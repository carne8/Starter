module Starter.ApplicationSearchEngine.Linux.Theme.IconThemeParser

open System
open System.IO
open System.Threading.Tasks
open System.Text.RegularExpressions

open Starter.ApplicationSearchEngine.Linux.Theme
open Starter.ApplicationSearchEngine
open FsToolkit.ErrorHandling

/// Parse the index.theme file and return directory information
let parseIndexTheme (indexPath: string) : Task<IconTheme option> =
    task {
        if not (File.Exists indexPath) then return None else

        use lines = indexPath |> File.ReadLinesAsync |> _.GetAsyncEnumerator()
        let mutable currentSection = ""

        // Icon theme properties
        let path = indexPath |> Path.GetDirectoryName
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

        return Some { Name = path |> Path.GetFileName
                      ThemePath = path
                      Directories = directories.ToArray()
                      ParentThemes = parentThemes }
    }

/// Parse a directory containing icons (ex: .../128x128@2/)
let private parseIconDirectory (dir: string) : IconThemeDirectory array option =
    let directoryRegex = Regex(@"(\d+)x\d+@?(\d)?", RegexOptions.Compiled)
    match dir with
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

let parseFromDirectory (directory: string) =
    { Name = directory |> Path.GetFileName
      ThemePath = directory
      Directories =
        directory
        |> Directory.GetDirectories
        |> Array.choose parseIconDirectory
        |> Array.concat
      ParentThemes = Array.empty }

