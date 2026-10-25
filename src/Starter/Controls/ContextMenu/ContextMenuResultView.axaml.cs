using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Starter.SearchEngine;
using Starter.TextMatching;

namespace Starter.Controls;

using ContextMenuFuzzyMatchResults = Dictionary<IContextMenuEntry, FuzzyMatch.FuzzyResult>;

public static class ContextMenuStatusConverters
{
    public static FuncValueConverter<ContextMenuStatus, bool> IsLoading { get; } =
        new(status => status == ContextMenuStatus.Loading);
    public static FuncValueConverter<ContextMenuStatus, bool> IsLoaded { get; } =
        new(status => status == ContextMenuStatus.Loaded);
    public static FuncValueConverter<ContextMenuStatus, bool> IsFailed { get; } =
        new(status => status == ContextMenuStatus.Failed);
    public static FuncValueConverter<ContextMenuStatus, bool> IsSeparator { get; } =
        new(status => status == ContextMenuStatus.Separator);
}

public partial class ContextMenuResultView : UserControl
{
    private readonly ContextMenuFuzzyMatchResults fuzzyMatchResults;
    public ContextMenuResultView(ContextMenuFuzzyMatchResults fuzzyMatchResults)
    {
        this.fuzzyMatchResults = fuzzyMatchResults;
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != DataContextProperty) return;
        if (DataContext is not IContextMenuEntry entry) return;

        NameTextBlock.AccentuationMap = fuzzyMatchResults.GetValueOrDefault(entry).MatchingPositions;
    }
}
