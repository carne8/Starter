using Avalonia;
using Avalonia.Controls;
using Starter.SearchEngine;
using Starter.TextMatching;

namespace Starter.Controls;

using FuzzyMatchResults = Dictionary<SearchResult, FuzzyMatch.FuzzyResult>;

public partial class SearchResultView : UserControl
{
    private readonly FuzzyMatchResults fuzzyMatchResults;
    public SearchResultView(FuzzyMatchResults fuzzyMatchResults)
    {
        this.fuzzyMatchResults = fuzzyMatchResults;
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != DataContextProperty) return;
        if (DataContext is not SearchResult entry) return;

        NameTextBlock.AccentuationMap = fuzzyMatchResults.GetValueOrDefault(entry).MatchingPositions;
    }
}
