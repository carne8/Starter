using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Starter.Features;
using Starter.SearchEngine;

namespace Starter.Controls;

public partial class SearchResult : UserControl
{
    public static readonly StyledProperty<bool[]?> AccentuationMapProperty =
        AccentuatedTextBlock.AccentuationMapProperty.AddOwner<SearchResult>();

    public bool[]? AccentuationMap
    {
        get => GetValue(AccentuationMapProperty);
        set => SetValue(AccentuationMapProperty, value);
    }

    public SearchResult() => InitializeComponent();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AccentuationMapProperty)
            NameTextBlock.AccentuationMap = AccentuationMap;
    }
}

public class SearchResultDataTemplate : IRecyclingDataTemplate
{
    public bool Match(object? data) => data is SearchResultData;

    public Control? Build(object? param, Control? existing)
    {
        if (param is not SearchResultData data) return null;
        if (data.SearchResult is IControlSearchResult controlSr)
        {
            return existing is ControlSearchResult
                ? existing
                : new ControlSearchResult { DataContext = controlSr };
        }

        if (existing is SearchResult srControl)
        {
            srControl[!SearchResult.AccentuationMapProperty] = new Binding(nameof(data.AccentuationMap));
        }

        return new SearchResult
            {
                DataContext = data.SearchResult,
                [!SearchResult.AccentuationMapProperty] = new Binding(nameof(data.AccentuationMap))
            };
    }

    public Control? Build(object? param)
    {
        if (param is not SearchResultData data) return null;
        return data.SearchResult is IControlSearchResult controlSr
            ? new ControlSearchResult { DataContext = controlSr }
            : new SearchResult
            {
                DataContext = data.SearchResult,
                [!SearchResult.AccentuationMapProperty] = new Binding(nameof(data.AccentuationMap))
            };
    }
}
