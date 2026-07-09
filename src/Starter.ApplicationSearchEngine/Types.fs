namespace Starter.ApplicationSearchEngine

[<Struct>]
type FolderConfiguration =
    { /// Folders to search for apps in decreasing order of priority
      Folders: string array
      ExcludedFolders: string array
      AllowDuplicates: bool }

    static member isFileExcluded config (file: string) =
        config.ExcludedFolders |> Array.exists file.StartsWith

module Constants =
    open Avalonia
    open Avalonia.Media
    open Avalonia.Threading
    open Starter.SearchEngine

    let [<Literal>] IconSize = 70
    let iconPixelSize = PixelSize(IconSize, IconSize)

    let private createIcon lightMode =
        let pen =
            Pen(
                (if lightMode then Brushes.Black else Brushes.White),
                1.5,
                lineCap = PenLineCap.Round,
                lineJoin = PenLineJoin.Round
            )

        let group = DrawingGroup()

        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = RectangleGeometry(Rect(3, 3, 7, 7), 1, 1)))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = RectangleGeometry(Rect(14, 3, 7, 7), 1, 1)))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = RectangleGeometry(Rect(14, 14, 7, 7), 1, 1)))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = RectangleGeometry(Rect(3, 14, 7, 7), 1, 1)))

        DrawingImage(group, Viewbox = Rect(0, 0, 24, 24))

    let getIcon () =
        Dispatcher.UIThread.Invoke(fun () ->
            StarterIconSource(createIcon true, createIcon false)
        )
