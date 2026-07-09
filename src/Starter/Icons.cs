using Avalonia;
using Avalonia.Media;
using Starter.SearchEngine;

namespace Starter;

public static class Icons
{
    public static readonly StarterIconSource Settings = new(SettingsIcon(true), SettingsIcon(false));
    public static readonly StarterIconSource Logs = new(LogsIcon(true), LogsIcon(false));
    public static readonly StarterIconSource Exit = new(ExitIcon(true), ExitIcon(false));

    private static Pen GetPen(bool lightMode) =>
        new(lightMode ? Brushes.Black : Brushes.White, 1.5, lineCap: PenLineCap.Round, lineJoin: PenLineJoin.Round);

    private static DrawingImage SettingsIcon(bool lightMode)
    {
        var pen = GetPen(lightMode);
        return new DrawingImage(new DrawingGroup
        {
            Children =
            [
                new GeometryDrawing
                {
                    Geometry = StreamGeometry.Parse(
                        "F1 M9.671 4.136a2.34 2.34 0 0 1 4.659 0 2.34 2.34 0 0 0 3.319 1.915 2.34 2.34 0 0 1 2.33 4.033 2.34 2.34 0 0 0 0 3.831 2.34 2.34 0 0 1-2.33 4.033 2.34 2.34 0 0 0-3.319 1.915 2.34 2.34 0 0 1-4.659 0 2.34 2.34 0 0 0-3.32-1.915 2.34 2.34 0 0 1-2.33-4.033 2.34 2.34 0 0 0 0-3.831A2.34 2.34 0 0 1 6.35 6.051a2.34 2.34 0 0 0 3.319-1.915"
                    ),
                    Pen = pen
                },

                new GeometryDrawing
                {
                    Geometry = new EllipseGeometry { RadiusX = 3, RadiusY = 3, Center = new Point(12, 12) },
                    Pen = pen
                }
            ]
        }) { Viewbox = new Rect(0, 0, 24, 24) };
    }

    private static DrawingImage LogsIcon(bool lightMode)
    {
        var pen = GetPen(lightMode);
        return new DrawingImage(new DrawingGroup
        {
            Children =
            [
                new GeometryDrawing { Pen = pen, Geometry = StreamGeometry.Parse("F1 M12 7v14") },
                new GeometryDrawing { Pen = pen, Geometry = StreamGeometry.Parse("F1 M16 12h2") },
                new GeometryDrawing { Pen = pen, Geometry = StreamGeometry.Parse("F1 M16 8h2") },
                new GeometryDrawing { Pen = pen, Geometry = StreamGeometry.Parse("F1 M3 18a1 1 0 0 1-1-1V4a1 1 0 0 1 1-1h5a4 4 0 0 1 4 4 4 4 0 0 1 4-4h5a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1h-6a3 3 0 0 0-3 3 3 3 0 0 0-3-3z") },
                new GeometryDrawing { Pen = pen, Geometry = StreamGeometry.Parse("F1 M6 12h2") },
                new GeometryDrawing { Pen = pen, Geometry = StreamGeometry.Parse("F1 M6 8h2") }
            ]
        }) { Viewbox = new Rect(0, 0, 24, 24) };
    }

    private static DrawingImage ExitIcon(bool lightMode)
    {
        var pen = GetPen(lightMode);
        return new DrawingImage(new DrawingGroup
        {
            Children =
            [
                new GeometryDrawing { Pen = pen, Geometry = StreamGeometry.Parse("F1 m15 9-6 6") },
                new GeometryDrawing { Pen = pen, Geometry = StreamGeometry.Parse("F1 M2.586 16.726A2 2 0 0 1 2 15.312V8.688a2 2 0 0 1 .586-1.414l4.688-4.688A2 2 0 0 1 8.688 2h6.624a2 2 0 0 1 1.414.586l4.688 4.688A2 2 0 0 1 22 8.688v6.624a2 2 0 0 1-.586 1.414l-4.688 4.688a2 2 0 0 1-1.414.586H8.688a2 2 0 0 1-1.414-.586z") },
                new GeometryDrawing { Pen = pen, Geometry = StreamGeometry.Parse("F1 m9 9 6 6") }
            ]
        }) { Viewbox = new Rect(0, 0, 24, 24) };
    }
}
