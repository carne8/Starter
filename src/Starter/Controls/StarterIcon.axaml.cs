using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Starter.SearchEngine;

namespace Starter.Controls;

public partial class StarterIcon : UserControl
{
    public StarterIcon()
    {
        InitializeComponent();
    }

    public new static readonly StyledProperty<double> FontSizeProperty =
        TextBlock.FontSizeProperty.AddOwner<StarterIcon>();

    public static readonly StyledProperty<StarterIconSource> IconSourceProperty =
        AvaloniaProperty.Register<StarterIcon, StarterIconSource>(nameof(IconSource));

    public static readonly StyledProperty<double> ExtraPaddingForSymbolProperty =
        AvaloniaProperty.Register<StarterIcon, double>(nameof(ExtraPaddingForSymbol));

    public StarterIconSource IconSource
    {
        get => GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    public double ExtraPaddingForSymbol
    {
        get => GetValue(ExtraPaddingForSymbolProperty);
        set => SetValue(ExtraPaddingForSymbolProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == IconSourceProperty) SetContent(change.GetNewValue<StarterIconSource>());
        if (change.Property == FontSizeProperty
            || change.Property == ExtraPaddingForSymbolProperty)
        {
            if (Content is not PathIcon pathIcon) return;
            pathIcon.Width = FontSize - ExtraPaddingForSymbol;
            pathIcon.Height = FontSize - ExtraPaddingForSymbol;
        }

        if (change.NewValue is ThemeVariant theme && Content is Image image)
            image.Source = IconSource.GetImage(theme == ThemeVariant.Light);

        base.OnPropertyChanged(change);
    }

    private void SetContent(StarterIconSource? iconSource)
    {
        if (iconSource == null) return;
        Content = iconSource.Geometry is null
            ? new Image()
            {
                Source = iconSource.GetImage(ActualThemeVariant == ThemeVariant.Light)
            }
            : new PathIcon()
            {
                Data = iconSource.Geometry,
                Width = FontSize - ExtraPaddingForSymbol,
                Height = FontSize - ExtraPaddingForSymbol
            };
    }
}
