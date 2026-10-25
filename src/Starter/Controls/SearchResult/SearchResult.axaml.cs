using Avalonia;
using Avalonia.Controls;
using Starter.TextMatching;

namespace Starter.Controls;

using FuzzyMatchResults = Dictionary<SearchEngine.SearchResult, FuzzyMatch.FuzzyResult>;

public partial class SearchResult : UserControl
{
    private readonly FuzzyMatchResults fuzzyMatchResults;
    public SearchResult(FuzzyMatchResults fuzzyMatchResults)
    {
        this.fuzzyMatchResults = fuzzyMatchResults;
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != DataContextProperty) return;
        if (DataContext is not SearchEngine.SearchResult entry) return;

        NameTextBlock.AccentuationMap = fuzzyMatchResults.GetValueOrDefault(entry).MatchingPositions;
    }
}
