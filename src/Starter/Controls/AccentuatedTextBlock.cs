using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Starter.Controls;

public partial class AccentuatedTextBlock : Control
{
    private Typeface typeface = new(FontFamily.Default, weight: FontWeight.Regular);
    private double scale;

    static AccentuatedTextBlock() =>
        AffectsRender<AccentuatedTextBlock>(
            ForegroundProperty,
            AccentuatedForegroundProperty,
            FontWeightProperty
        );

    public AccentuatedTextBlock() => UpdateScales();

    private void LoadFont()
    {
        typeface = new Typeface(FontFamily, weight: FontWeight);
        UpdateScales();
    }

    private void UpdateScales() => scale = FontSize / typeface.GlyphTypeface.Metrics.DesignEmHeight;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FontFamilyProperty ||
            change.Property == FontWeightProperty) LoadFont();
        else if (change.Property == FontSizeProperty) UpdateScales();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = Text ?? string.Empty;
        var shapedBuffer =
            TextShaper.Current.ShapeText(text, new TextShaperOptions(typeface.GlyphTypeface, FontSize));
        using var shapedTextRun =
            new ShapedTextRun(shapedBuffer, new GenericTextRunProperties(typeface, FontSize));

        return shapedTextRun.Size;
    }

    protected override Size ArrangeOverride(Size finalSize) => finalSize;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Text is null) return;

        var textMemory = Text.AsMemory();

        if (AccentuationMap is null || AccentuationMap.Length == 0)
            RenderUniform(context, Text, textMemory);
        else RenderAccentuated(context, Text, textMemory, AccentuationMap);
    }

    public void RenderUniform(DrawingContext context, string text, ReadOnlyMemory<char> textMemory)
    {
        var glyphIndices = new ushort[textMemory.Length];

        for (var i = 0; i < glyphIndices.Length; i++)
            glyphIndices[i] = typeface.GlyphTypeface.CharacterToGlyphMap.GetGlyph(text[i]);

        DrawGlyphRun(context, textMemory, glyphIndices, false);
    }

    public void RenderAccentuated(DrawingContext context, string text, ReadOnlyMemory<char> textMemory, bool[] accentuationMap)
    {
        var glyphIndices = new ushort[textMemory.Length];

        var prevCharAccentuation = false;
        var rangeStart = 0;
        var advance = 0.0;

        for (var charIdx = 0; charIdx <= text.Length; charIdx++)
        {
            if (charIdx == text.Length || prevCharAccentuation != accentuationMap[charIdx])
            {
                var rangeEnd = charIdx - 1;
                var rangeLength = rangeEnd - rangeStart + 1;

                if (rangeLength != 0)
                {
                    for (var i = 0; i < rangeLength; i++)
                        glyphIndices[i] = typeface.GlyphTypeface.CharacterToGlyphMap.GetGlyph(text[rangeStart + i]);
                    // to charIdx - rangeStart
                    // corresponds to the char index

                    context.PushTransform(Matrix.CreateTranslation(advance, 0));

                    DrawGlyphRun(
                        context,
                        textMemory.Slice(rangeStart, rangeLength),
                        new ArraySegment<ushort>(glyphIndices, 0, rangeLength),
                        accentuationMap[rangeStart]
                    );

                    // Update advance for next range
                    advance = 0;
                    for (var i = 0; i < rangeLength; i++)
                    {
                        if (!typeface.GlyphTypeface.TryGetHorizontalGlyphAdvance(glyphIndices[i], out var glyphAdvance)) continue;
                        advance += scale * glyphAdvance;
                    }

                    rangeStart = charIdx;
                }
            }

            if (charIdx != text.Length) prevCharAccentuation = accentuationMap[charIdx];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void DrawGlyphRun(
        DrawingContext context,
        ReadOnlyMemory<char> textMemory,
        IReadOnlyList<ushort> glyphIndices,
        bool accentuated
    )
    {
        using var glyphRun =
            new GlyphRun(
                typeface.GlyphTypeface,
                FontSize,
                textMemory,
                glyphIndices
            );

        context.DrawGlyphRun(accentuated ? AccentuatedForeground : Foreground, glyphRun);
    }
}
