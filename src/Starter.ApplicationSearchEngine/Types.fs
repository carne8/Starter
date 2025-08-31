namespace Starter.ApplicationSearchEngine

[<Struct>]
type FolderConfiguration =
    { Folders: string array
      ExcludedFolders: string array }

    static member isFileExcluded config (file: string) =
        config.ExcludedFolders |> Array.exists file.StartsWith

module Constants =
    open Avalonia

    let [<Literal>] IconSize = 70
    let iconPixelSize = PixelSize(IconSize, IconSize)
