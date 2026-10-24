using Avalonia.Controls.ApplicationLifetimes;
using Starter.SearchEngine;

namespace Starter;

internal class ExitSearchResult : SearchResult
{
    public override string Id => nameof(ExitSearchResult);
    public override string Name => "Exit";
    public override string Description => "Quit starter";
    public override string[] Keywords => ["quit"];
    public override StarterIconSource Icon => Icons.Exit;
    public override bool ShowIfNoActivator => true;
}

internal class ExitSearchEngine(IClassicDesktopStyleApplicationLifetime lifetime) : StaticSearchEngine
{
    public override string Id => nameof(ExitSearchEngine);
    public override string Name => "Exit";
    public override StarterIconSource Icon => Icons.Exit;

    private static readonly IEnumerable<SearchResult> SearchResults = [new ExitSearchResult()];

    public override void SearchResultSelected(SearchResult selectedSearchResult) => lifetime.TryShutdown();
    public override ValueTask<IEnumerable<SearchResult>> LoadResults() => ValueTask.FromResult(SearchResults);
}
