namespace Starter.Controls

#nowarn 0044 // IGlyphTypeface is [Unstable]

open System
open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Documents
open Avalonia.Media
open Avalonia.Media.TextFormatting

/// Displays text with variable weight
type AccentuatedTextBlock() =
    inherit Control()

    static let FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner<AccentuatedTextBlock>()
    static let FontSizeProperty = TextElement.FontSizeProperty.AddOwner<AccentuatedTextBlock>()
    static let FontStyleProperty = TextElement.FontStyleProperty.AddOwner<AccentuatedTextBlock>()
    static let FontStretchProperty = TextElement.FontStretchProperty.AddOwner<AccentuatedTextBlock>()
    static let ForegroundProperty = TextElement.ForegroundProperty.AddOwner<AccentuatedTextBlock>()
    static let TextProperty = TextBlock.TextProperty.AddOwner<AccentuatedTextBlock>()
    static let AccentuationMapProperty = AvaloniaProperty.Register<AccentuatedTextBlock, bool array>("AccentuationMap", Array.empty)

    let mutable normalGlyphTypeface = Typeface(FontFamily.Default, weight = FontWeight.Regular).GlyphTypeface
    let mutable accentuatedTypeface = Typeface(FontFamily.Default, weight = FontWeight.ExtraBold)
    let mutable accentuatedGlyphTypeface = accentuatedTypeface.GlyphTypeface
    let mutable normalScale = 12. / (float normalGlyphTypeface.Metrics.DesignEmHeight)
    let mutable accentuatedScale = 12. / (float accentuatedGlyphTypeface.Metrics.DesignEmHeight)

    do
        AccentuatedTextBlock.AffectsRender(ForegroundProperty, FontSizeProperty)
        AccentuatedTextBlock.AffectsMeasure(FontSizeProperty)

    member this.FontFamily
        with get () = this.GetValue(FontFamilyProperty)
        and set v = this.SetValue(FontFamilyProperty, v) |> ignore
    member this.FontSize
        with get () = this.GetValue(FontSizeProperty)
        and set v = this.SetValue(FontSizeProperty, v) |> ignore
    member this.FontStyle
        with get () = this.GetValue(FontStyleProperty)
        and set v = this.SetValue(FontStyleProperty, v) |> ignore
    member this.FontStretch
        with get () = this.GetValue(FontStretchProperty)
        and set v = this.SetValue(FontStretchProperty, v) |> ignore
    member this.Foreground
        with get () = this.GetValue(ForegroundProperty)
        and set v = this.SetValue(ForegroundProperty, v) |> ignore
    member this.Text
        with get() = this.GetValue(TextProperty)
        and set v = this.SetValue(TextProperty, v) |> ignore
    member this.AccentuationMap
        with get() = this.GetValue(AccentuationMapProperty)
        and set v = this.SetValue(AccentuationMapProperty, v) |> ignore

    override this.OnPropertyChanged change =
        if change.Property = FontFamilyProperty then
            match change.NewValue with
            | :? FontFamily as newFont ->
                normalGlyphTypeface <- Typeface(newFont, weight = FontWeight.Regular).GlyphTypeface
                accentuatedTypeface <- Typeface(newFont, weight = FontWeight.ExtraBold)
                accentuatedGlyphTypeface <- accentuatedTypeface.GlyphTypeface
                normalScale <- this.FontSize / (float normalGlyphTypeface.Metrics.DesignEmHeight)
                accentuatedScale <- this.FontSize / (float accentuatedGlyphTypeface.Metrics.DesignEmHeight)
            | _ -> ()

        if change.Property = FontSizeProperty then
            match change.NewValue with
            | :? float as newFontSize ->
                normalScale <- newFontSize / (float normalGlyphTypeface.Metrics.DesignEmHeight)
                accentuatedScale <- newFontSize / (float accentuatedGlyphTypeface.Metrics.DesignEmHeight)
            | _ -> ()

    override this.MeasureOverride(_availableSize) =
        let text =
            match this.Text with
            | null -> String.Empty
            | text -> text

        let shapedBuffer = TextShaper.Current.ShapeText(text, TextShaperOptions(accentuatedGlyphTypeface, this.FontSize))
        use shapedTextRun = new ShapedTextRun(shapedBuffer, GenericTextRunProperties(accentuatedTypeface, this.FontSize))
        shapedTextRun.Size

    override this.ArrangeOverride(finalSize) = finalSize

    override this.Render(context) =
        base.Render(context)
        match this.Text with
        | null -> ()
        | text ->
            let textMemory = text.AsMemory()
            let accentuationMap = this.AccentuationMap

            let normalTypeface = Typeface(this.FontFamily, weight = FontWeight.Regular).GlyphTypeface
            let accentuatedTypeface = Typeface(this.FontFamily, weight = FontWeight.ExtraBold).GlyphTypeface

            let mutable glyphIndices = Array.zeroCreate text.Length |> ArraySegment

            match accentuationMap.Length with
            | 0 ->
                for i = 0 to glyphIndices.Count-1 do
                    glyphIndices[i] <-
                        text[i]
                        |> uint
                        |> normalTypeface.GetGlyph

                use glyphRun =
                    new GlyphRun(
                        normalTypeface,
                        this.FontSize,
                        textMemory,
                        glyphIndices
                    )

                context.DrawGlyphRun(this.Foreground, glyphRun)

            | _ ->
                let mutable prevCharAccentuation = false
                let mutable rangeStart = 0
                let mutable advance = 0.

                for charIdx = 0 to text.Length do
                    if charIdx = text.Length || prevCharAccentuation <> accentuationMap[charIdx] then
                        let rangeEnd = charIdx-1
                        let rangeLength = rangeEnd - rangeStart + 1
                        let typeface = if accentuationMap[rangeStart] then accentuatedTypeface else normalTypeface

                        if rangeLength <> 0 then
                            for i = 0 to rangeLength-1 do // to charIdx - rangeStart
                                glyphIndices[i] <-
                                    text[rangeStart + i] // corresponds to the char index
                                    |> uint
                                    |> typeface.GetGlyph

                            Matrix.CreateTranslation(advance, 0) |> context.PushTransform |> ignore

                            use glyphRun =
                                new GlyphRun(
                                    typeface,
                                    this.FontSize,
                                    textMemory.Slice(rangeStart, rangeLength),
                                    glyphIndices.Slice(0, rangeLength)
                                )

                            context.DrawGlyphRun(this.Foreground, glyphRun)

                            // Update advance for next range
                            let scale = if accentuationMap[rangeStart] then accentuatedScale else normalScale
                            advance <- 0
                            for i = 0 to rangeLength-1 do
                                advance <-
                                    glyphIndices[i]
                                    |> typeface.GetGlyphAdvance
                                    |> float
                                    |> (*) scale
                                    |> (+) advance

                            rangeStart <- charIdx

                    if charIdx <> text.Length then
                        prevCharAccentuation <- accentuationMap[charIdx]
