using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Starter.Features.Config;

namespace Starter.Controls;

public class TranslucentWindow : Window
{
    public static readonly StyledProperty<Background?> BackgroundKindProperty = AvaloniaProperty.Register<TranslucentWindow, Background?>(nameof(BackgroundKind));
    public static readonly StyledProperty<IBrush?> AcrylicBackgroundColorProperty = AvaloniaProperty.Register<TranslucentWindow, IBrush?>(nameof(AcrylicBackgroundColor));
    public static readonly StyledProperty<IBrush?> BackgroundColorProperty = AvaloniaProperty.Register<TranslucentWindow, IBrush?>(nameof(BackgroundColor));

    private static readonly IReadOnlyList<WindowTransparencyLevel> NoneHint = [WindowTransparencyLevel.None];
    private static readonly IReadOnlyList<WindowTransparencyLevel> AcrylicHint = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent];
    private static readonly IReadOnlyList<WindowTransparencyLevel> MicaHint = [WindowTransparencyLevel.Mica, WindowTransparencyLevel.Blur, WindowTransparencyLevel.Transparent];

    public Background? BackgroundKind
    {
        get => GetValue(BackgroundKindProperty);
        set => SetValue(BackgroundKindProperty, value);
    }

    public IBrush? AcrylicBackgroundColor
    {
        get => GetValue(AcrylicBackgroundColorProperty);
        set => SetValue(AcrylicBackgroundColorProperty, value);
    }

    public IBrush? BackgroundColor
    {
        get => GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == BackgroundKindProperty
            || change.Property == AcrylicBackgroundColorProperty
            || change.Property == BackgroundColorProperty)
        {
            if (BackgroundKind is null) {}
            else if (BackgroundKind.IsNone)
            {
                TransparencyLevelHint = NoneHint;
                Background = BackgroundColor;
            }
            else if (BackgroundKind.IsAcrylic)
            {
                TransparencyLevelHint = AcrylicHint;
                Background = AcrylicBackgroundColor;
            }
            else if (BackgroundKind.IsMica)
            {
                TransparencyLevelHint = MicaHint;
                Background = null;
            }
        }

        base.OnPropertyChanged(change);
    }
}
