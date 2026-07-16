namespace Starter.Calculator.Types

open Avalonia
open Avalonia.Controls.Templates
open Avalonia.Media
open Avalonia.Threading
open MathNet.Numerics
open Starter.SearchEngine

[<Struct>]
type Constant = E | I | Pi

type Function =
    | Sqrt
    | Abs
    | Ln
    | Lg
    | Exp
    | Sin | Cos | Tan
    | Sec | Csc | Cot
    | Sh | Ch | Th
    | Sech | Csch | Coth
    | Asin | Acos | Atan
    | Asec | Acsc | Acot
    | Ash | Ach | Ath
    | Asech | Acsch | Acoth
    | Factorial

type Expression =
    | Constant of Constant
    | Number of BigRational
    | Sum of Expression * Expression
    | Product of Expression * Expression
    | Power of base': Expression * exponent: Expression
    | Function of Function * Expression
    | Undefined

module Icon =
    let private createIcon lightMode =
        let pen =
            Pen(
                (if lightMode then Brushes.Black else Brushes.White),
                1.5,
                lineCap = PenLineCap.Round,
                lineJoin = PenLineJoin.Round
            )

        let group = DrawingGroup()
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = RectangleGeometry(Rect(4, 2, 16, 20), 2, 2)))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = LineGeometry(Point(8, 6), Point(16, 6))))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = LineGeometry(Point(16, 14), Point(16, 18))))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M16 10h.01"))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M12 10h.01"))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M8 10h.01"))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M12 14h.01"))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M8 14h.01"))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M12 18h.01"))
        group.Children.Add(GeometryDrawing(Pen = pen, Geometry = StreamGeometry.Parse "F1 M8 18h.01"))

        DrawingImage(group, Viewbox = Rect(0, 0, 24, 24))

    let icon =
        Dispatcher.UIThread.Invoke(fun () ->
            StarterIconSource(createIcon true, createIcon false)
        )

type LaTeXSearchResult =
    { LaTeX: string
      DataTemplate: IDataTemplate }

    interface IControlSearchResult with
        member this.Id = null
        member this.Name = ""
        member this.Description = ""
        member this.Keywords = Array.empty
        member this.Icon = Icon.icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
        member this.ShowIcon = true
        member this.ControlDataContext = this
        member this.ControlDataTemplate = this.DataTemplate
        member this.HasContextMenu = false
        member this.GetContextMenu() = null

type NumberSearchResult =
    { Result: string }

    interface ISearchResult with
        member this.Id = null
        member this.Name = this.Result
        member this.Description = null
        member this.Keywords = Array.empty
        member this.Icon = Icon.icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
        member this.HasContextMenu = false
        member this.GetContextMenu() = null
