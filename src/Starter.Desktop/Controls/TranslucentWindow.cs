using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Starter.Features.Config;

namespace Starter.Desktop.Controls;

public class TranslucentWindow : Window
{
    public static readonly StyledProperty<Background?> BackgroundKindProperty = AvaloniaProperty.Register<TranslucentWindow, Background?>(nameof(BackgroundKind));
    public static readonly StyledProperty<IBrush?> AcrylicBackgroundColorProperty = AvaloniaProperty.Register<TranslucentWindow, IBrush?>(nameof(AcrylicBackgroundColor));
    public static readonly StyledProperty<IBrush?> BackgroundColorProperty = AvaloniaProperty.Register<TranslucentWindow, IBrush?>(nameof(BackgroundColor));

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
                TransparencyLevelHint = [WindowTransparencyLevel.None];
                Background = BackgroundColor;
            }
            else if (BackgroundKind.IsAcrylic)
            {
                TransparencyLevelHint = [WindowTransparencyLevel.AcrylicBlur, WindowTransparencyLevel.Blur];
                Background = AcrylicBackgroundColor;
            }
            else if (BackgroundKind.IsMica)
            {
                TransparencyLevelHint = [WindowTransparencyLevel.Mica, WindowTransparencyLevel.Transparent];
                Background = null;
            }
        }

        base.OnPropertyChanged(change);
    }
}
