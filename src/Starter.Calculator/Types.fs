namespace Starter.Calculator.Types

open Avalonia
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

type LaTeXSearchResult(LaTeX: string, dataTemplate) =
    inherit ControlSearchResult()

    member _.LaTeX = LaTeX
    override this.Id = null
    override this.Name = ""
    override this.Description = ""
    override this.Keywords = Array.empty
    override this.Icon = Icon.icon
    override this.Priority = ResultPriority.Unique
    override this.ShowIfNoActivator = true
    override this.ActivatorFilter = Array.empty
    override this.ShowIcon = true
    override this.ControlDataContext = this
    override this.ControlDataTemplate = dataTemplate
    override this.ContextMenuLoader = null

type NumberSearchResult(result) =
    inherit SearchResult()

    member this.Result = result
    override this.Id = null
    override this.Name = result
    override this.Description = null
    override this.Keywords = Array.empty
    override this.Icon = Icon.icon
    override this.Priority = ResultPriority.Unique
    override this.ShowIfNoActivator = true
    override this.ActivatorFilter = Array.empty
    override this.ContextMenuLoader = null
