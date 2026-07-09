module Starter.WorkspaceSearchEngine.Icons

open Starter.SearchEngine

open System.IO

open Avalonia
open Avalonia.Media
open Avalonia.Svg.Skia
open Avalonia.Threading


let private createSearchEngineIcon lightMode =
    let pen =
        Pen(
            (if lightMode then Brushes.Black else Brushes.White),
            1.5,
            lineCap = PenLineCap.Round,
            lineJoin = PenLineJoin.Round
        )

    let group = DrawingGroup()

    group.Children.Add(GeometryDrawing(Pen = pen, Geometry = RectangleGeometry(Rect(3, 3, 8, 18), 1, 1)))
    group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M7 3v18"))
    group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M20.4 18.9c.2.5-.1 1.1-.6 1.3l-1.9.7c-.5.2-1.1-.1-1.3-.6L11.1 5.1c-.2-.5.1-1.1.6-1.3l1.9-.7c.5-.2 1.1.1 1.3.6Z"))

    DrawingImage(group, Viewbox = Rect(0, 0, 24, 24))

let searchEngineIcon =
    Dispatcher.UIThread.Invoke(fun () ->
        StarterIconSource(createSearchEngineIcon true, createSearchEngineIcon false)
    )

module IconName =
    let vsCode = "vscode.svg"
    let vsCodeInsiders = "vscode-insiders.svg"
    let rider = "Rider.svg"
    let pyCharm = "PyCharm.svg"
    let intelliJ = "IntelliJ_IDEA.svg"
    let goLand = "GoLand.svg"
    let phpStorm = "PhpStorm.svg"
    let webStorm = "WebStorm.svg"
    let rubyMine = "RubyMine.svg"
    let rustRover = "RustRover.svg"
    let cLion = "CLion.svg"
    let androidStudio = "Android_Studio.svg"
    let windowsTerminal = "windows-terminal.svg"

let loadIcon pluginPath iconName =
    let iconFile = Path.Combine(pluginPath, "Images", iconName)
    let svg = Dispatcher.UIThread.Invoke(fun () -> SvgImage(Source = SvgSource.Load iconFile))
    StarterIconSource(svg, svg)
