using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Starter.Controls;

public abstract class TextOptionsBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<TextRenderingMode> TextRenderingModeProperty =
        AvaloniaProperty.RegisterAttached<Visual, TextBlock, TextRenderingMode>(nameof(TextRenderingMode));

    public static TextRenderingMode? GetTextRenderingMode(Visual item) =>
        item.GetValue(TextRenderingModeProperty);

    public static void SetTextRenderingMode(Visual item, TextRenderingMode? value) =>
        item.SetValue(TextRenderingModeProperty, value);

    static TextOptionsBehavior()
    {
        TextRenderingModeProperty.Changed.AddClassHandler<Visual>(OnChangedVisual);
    }

    private static void OnChangedVisual(Visual item, AvaloniaPropertyChangedEventArgs args) =>
        TextOptions.SetTextRenderingMode(
            item,
            args.NewValue is TextRenderingMode textRenderingMode
                ? textRenderingMode
                : TextRenderingMode.Unspecified
        );
}
