using Avalonia;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Starter.SearchEngine;

namespace Starter.Desktop.Controls;

public partial class CustomSettingsExpander : SettingsExpander
{
    public static readonly StyledProperty<StarterIconSource> StarterIconSourceProperty =
        AvaloniaProperty.Register<CustomSettingsExpander, StarterIconSource>(nameof(StarterIconSource));

    public StarterIconSource StarterIconSource
    {
        get => GetValue(StarterIconSourceProperty);
        set => SetValue(StarterIconSourceProperty, value);
    }

    public IconSource BuildIconSource(StarterIconSource icon, bool lightMode) =>
        icon.Geometry is null
            ? new ImageIconSource { Source = icon.GetImage(lightMode) }
            : new PathIconSource { Data = icon.Geometry };

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StarterIconSourceProperty || change.NewValue is ThemeVariant)
            IconSource = BuildIconSource(StarterIconSource, ActualThemeVariant == ThemeVariant.Light);
    }

    public CustomSettingsExpander() => InitializeComponent();
}
