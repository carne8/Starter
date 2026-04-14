using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Starter.Controls;

public partial class AccentuatedTextBlock : Control
{
    private Typeface normalTypeface;
    private double normalScale;
    private Typeface accentuatedTypeface;
    private double accentuatedScale;

    static AccentuatedTextBlock() => AffectsRender<AccentuatedTextBlock>(ForegroundProperty);

    public AccentuatedTextBlock()
    {
        normalTypeface = new Typeface(FontFamily.Default, weight: FontWeight.Regular);
        accentuatedTypeface = new Typeface(FontFamily.Default, weight: FontWeight.ExtraBold);
        UpdateScales();
    }

    private void LoadFont(FontFamily font)
    {
        normalTypeface = new Typeface(font, weight: FontWeight.Regular);
        accentuatedTypeface = new Typeface(font, weight: FontWeight.ExtraBold);
        UpdateScales();
    }

    private void UpdateScales()
    {
        normalScale = FontSize / normalTypeface.GlyphTypeface.Metrics.DesignEmHeight;
        accentuatedScale = FontSize / accentuatedTypeface.GlyphTypeface.Metrics.DesignEmHeight;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FontFamilyProperty) LoadFont(FontFamily);
        else if (change.Property == FontSizeProperty) UpdateScales();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = Text ?? string.Empty;
        var shapedBuffer =
            TextShaper.Current.ShapeText(text, new TextShaperOptions(accentuatedTypeface.GlyphTypeface, FontSize));
        using var shapedTextRun =
            new ShapedTextRun(shapedBuffer, new GenericTextRunProperties(accentuatedTypeface, FontSize));

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
            glyphIndices[i] = normalTypeface.GlyphTypeface.CharacterToGlyphMap.GetGlyph(text[i]);

        DrawGlyphRun(context, normalTypeface.GlyphTypeface, textMemory, glyphIndices);
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
                var typeface =
                    accentuationMap[rangeStart]
                        ? accentuatedTypeface.GlyphTypeface
                        : normalTypeface.GlyphTypeface;

                if (rangeLength != 0)
                {
                    for (var i = 0; i < rangeLength; i++)
                        glyphIndices[i] = typeface.CharacterToGlyphMap.GetGlyph(text[rangeStart + i]);
                    // to charIdx - rangeStart
                    // corresponds to the char index

                    context.PushTransform(Matrix.CreateTranslation(advance, 0));

                    DrawGlyphRun(
                        context,
                        typeface,
                        textMemory.Slice(rangeStart, rangeLength),
                        new ArraySegment<ushort>(glyphIndices, 0, rangeLength)
                    );

                    // Update advance for next range
                    var scale = accentuationMap[rangeStart] ? accentuatedScale : normalScale;
                    advance = 0;
                    for (var i = 0; i < rangeLength; i++)
                    {
                        if (!typeface.TryGetHorizontalGlyphAdvance(glyphIndices[i], out var glyphAdvance)) continue;
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
        GlyphTypeface typeface,
        ReadOnlyMemory<char> textMemory,
        IReadOnlyList<ushort> glyphIndices
    )
    {
        using var glyphRun =
            new GlyphRun(
                typeface,
                FontSize,
                textMemory,
                glyphIndices
            );

        context.DrawGlyphRun(Foreground, glyphRun);
    }
}
