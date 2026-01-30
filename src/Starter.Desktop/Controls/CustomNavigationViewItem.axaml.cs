using Avalonia;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Starter.SearchEngine;

namespace Starter.Desktop.Controls;

public partial class CustomNavigationViewItem : NavigationViewItem
{
    public static readonly StyledProperty<StarterIconSource?> StarterIconSourceProperty =
        CustomSettingsExpander.StarterIconSourceProperty.AddOwner<CustomNavigationViewItem>();

    public StarterIconSource? StarterIconSource
    {
        get => GetValue(StarterIconSourceProperty);
        set => SetValue(StarterIconSourceProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (StarterIconSource is null) return;
        if (change.Property == StarterIconSourceProperty || change.NewValue is ThemeVariant)
            IconSource = CustomSettingsExpander.BuildIconSource(StarterIconSource, ActualThemeVariant == ThemeVariant.Light);
    }

    public CustomNavigationViewItem() => InitializeComponent();
}
