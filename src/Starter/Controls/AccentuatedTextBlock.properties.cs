using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Starter.Controls;

public partial class AccentuatedTextBlock
{
     public static readonly AttachedProperty<FontFamily> FontFamilyProperty = TextElement.FontFamilyProperty.AddOwner<AccentuatedTextBlock>();
     public static readonly AttachedProperty<double> FontSizeProperty = TextElement.FontSizeProperty.AddOwner<AccentuatedTextBlock>();
     public static readonly AttachedProperty<FontWeight> FontWeightProperty = TextElement.FontWeightProperty.AddOwner<AccentuatedTextBlock>();
     public static readonly AttachedProperty<IBrush?> ForegroundProperty = TextElement.ForegroundProperty.AddOwner<AccentuatedTextBlock>();
     public static readonly StyledProperty<IBrush?> AccentuatedForegroundProperty = AvaloniaProperty.Register<AccentuatedTextBlock, IBrush?>(nameof(AccentuatedForeground));
     public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<AccentuatedTextBlock>();
     public static readonly StyledProperty<bool[]?> AccentuationMapProperty = AvaloniaProperty.Register<AccentuatedTextBlock, bool[]?>(nameof(AccentuationMap));

     public FontFamily FontFamily
     {
          get => GetValue(FontFamilyProperty);
          set => SetValue(FontFamilyProperty, value);
     }
     public double FontSize
     {
          get => GetValue(FontSizeProperty);
          set => SetValue(FontSizeProperty, value);
     }
     public FontWeight FontWeight
     {
          get => GetValue(FontWeightProperty);
          set => SetValue(FontWeightProperty, value);
     }
     public IBrush? Foreground
     {
          get => GetValue(ForegroundProperty);
          set => SetValue(ForegroundProperty, value);
     }
     public IBrush? AccentuatedForeground
     {
          get => GetValue(AccentuatedForegroundProperty);
          set => SetValue(AccentuatedForegroundProperty, value);
     }
     public string? Text
     {
          get => GetValue(TextProperty);
          set => SetValue(TextProperty, value);
     }
     public bool[]? AccentuationMap
     {
          get => GetValue(AccentuationMapProperty);
          set => SetValue(AccentuationMapProperty, value);
     }
}
