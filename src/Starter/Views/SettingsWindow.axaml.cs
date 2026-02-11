using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using FluentAvalonia.UI.Controls;
using Starter.Controls;
using Starter.SearchEngine;
using Starter.ViewModels;

namespace Starter.Views;

public static class SettingsDataTemplates
{
    public static IconSource BuildIconSource(StarterIconSource icon, bool lightMode) =>
        icon.Geometry is null
            ? new ImageIconSource { Source = icon.GetImage(lightMode) }
            : new PathIconSource { Data = icon.Geometry };

    public static readonly FuncDataTemplate<MenuItemViewModel> MenuItem = new((vm, _) =>
    {
        var control = new NavigationViewItem { Content = vm.Title };

        control.ActualThemeVariantChanged += (_, _) =>
            control.IconSource = BuildIconSource(vm.Icon, control.ActualThemeVariant == ThemeVariant.Light);
        control.Initialized += (_, _) =>
            control.IconSource = BuildIconSource(vm.Icon, control.ActualThemeVariant == ThemeVariant.Light);
        control.ResourcesChanged += (_, _) =>
        {
            if (!control.TryFindResource("JetBrainsMono", out var fontFamily)) return;
            control.FontFamily = fontFamily as FontFamily ?? control.FontFamily;
        };

        return control;
    });
}

public partial class SettingsWindow : TranslucentWindow
{
    private BindingExpressionBase? binding;

    public SettingsWindow()
    {
        InitializeComponent();

        // Use system decorations on Linux
        if (!OperatingSystem.IsLinux()) return;
        ExtendClientAreaToDecorationsHint = false;
        WindowTitle.IsVisible = false;
        NavigationView.Margin = new Thickness(0, 10, 0, 0);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Bind selected page control
        binding = ContentControl.Bind(ContentProperty, new Binding("SelectedPage.Control"));
        // Bind the content every time the data context changes to
        // prevent "The control ... already has a visual parent ...
        // while trying to add it as a child of ..." error
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        binding?.Dispose();
    }
}
