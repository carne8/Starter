using Avalonia;
using Avalonia.Styling;
using Avalonia.Svg.Skia;
using FluentAvalonia.UI.Controls;
using Starter.SearchEngine;

namespace Starter.Controls;

file static class StarterIconSourceExtension
{
    extension(StarterIconSource icon)
    {
        public FAIconSource Build(bool lightMode) =>
            icon.Geometry is not null
                ? new FAPathIconSource { Data = icon.Geometry }
                : new FAImageIconSource
                {
                    Source = icon.GetSvg(lightMode) is { } svg
                        ? new SvgImage { Source = svg }
                        : icon.GetImage(lightMode)
                };
    }
}

public abstract class NavigationViewItemBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<StarterIconSource?> StarterIconSourceProperty =
        AvaloniaProperty.RegisterAttached<NavigationViewItemBehavior, FANavigationViewItem, StarterIconSource?>(nameof(StarterIconSource));

    public static StarterIconSource? GetStarterIconSource(FANavigationViewItem item) =>
        item.GetValue(StarterIconSourceProperty);

    public static void SetStarterIconSource(FANavigationViewItem item, StarterIconSource? value) =>
        item.SetValue(StarterIconSourceProperty, value);

    static NavigationViewItemBehavior()
    {
        StarterIconSourceProperty.Changed.AddClassHandler<FANavigationViewItem>(OnChangedNavigationViewItem);
    }

    private static void OnChangedNavigationViewItem(FANavigationViewItem item, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not StarterIconSource iconSource)
        {
            item.IconSource = null;
            item.ActualThemeVariantChanged -= OnThemeVariantChanged;
            return;
        }

        item.IconSource = iconSource.Build(item.ActualThemeVariant == ThemeVariant.Light);
        item.ActualThemeVariantChanged += OnThemeVariantChanged;

        return;
        static void OnThemeVariantChanged(object? sender, EventArgs arg)
        {
            if (sender is not FANavigationViewItem item) return;
            if (GetStarterIconSource(item) is not { } icon) return;
            item.IconSource = icon.Build(item.ActualThemeVariant == ThemeVariant.Light);
        }
    }
}

public abstract class SettingsExpanderBehavior : AvaloniaObject
{
    public static readonly AttachedProperty<StarterIconSource?> StarterIconSourceProperty =
        AvaloniaProperty.RegisterAttached<SettingsExpanderBehavior, FASettingsExpander, StarterIconSource?>(nameof(StarterIconSource));

    public static StarterIconSource? GetStarterIconSource(FASettingsExpander item) =>
        item.GetValue(StarterIconSourceProperty);

    public static void SetStarterIconSource(FASettingsExpander item, StarterIconSource? value) =>
        item.SetValue(StarterIconSourceProperty, value);

    static SettingsExpanderBehavior()
    {
        StarterIconSourceProperty.Changed.AddClassHandler<FASettingsExpander>(OnChangedSettingsExpander);
    }

    private static void OnChangedSettingsExpander(FASettingsExpander item, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is not StarterIconSource iconSource)
        {
            item.IconSource = null;
            item.ActualThemeVariantChanged -= OnThemeVariantChanged;
            return;
        }

        item.IconSource = iconSource.Build(item.ActualThemeVariant == ThemeVariant.Light);
        item.ActualThemeVariantChanged += OnThemeVariantChanged;

        return;
        static void OnThemeVariantChanged(object? sender, EventArgs arg)
        {
            if (sender is not FASettingsExpander item) return;
            if (GetStarterIconSource(item) is not { } icon) return;
            item.IconSource = icon.Build(item.ActualThemeVariant == ThemeVariant.Light);
        }
    }
}
