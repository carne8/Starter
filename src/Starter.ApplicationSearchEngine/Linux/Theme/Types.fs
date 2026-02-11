namespace Starter.ApplicationSearchEngine.Linux.Theme

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

[<Struct>]
type IconTheme =
    { Name: string
      ThemePath: string
      Directories: IconThemeDirectory array
      ParentThemes: string array }

module IconTheme =
    open System.IO

    let getParentManifests themeManifest =
        themeManifest.ParentThemes
        |> Array.choose (fun parent ->
            let parentThemeManifestPath = Path.Combine(themeManifest.ThemePath, "..", parent, "index.theme")

            match File.Exists parentThemeManifestPath with
            | true -> Some parentThemeManifestPath
            | false -> None
        )

module IconThemeDirectory =
    /// Check if directory matches the requested size exactly
    let matchesSize (iconSize: int) (iconScale: int) (dirInfo: IconThemeDirectory) : bool =
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
    let getSizeDistance (iconSize: int) (iconScale: int) (dirInfo: IconThemeDirectory) : int =
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