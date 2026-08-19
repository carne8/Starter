using Avalonia.Controls.ApplicationLifetimes;
using Starter.SearchEngine;

namespace Starter;

internal class ExitSearchResult : ISearchResult
{
    public string Id => nameof(ExitSearchResult);
    public string Name => "Exit";
    public string Description => "Quit starter";
    public string[] Keywords => ["quit"];
    public StarterIconSource Icon => Icons.Exit;
    public bool ShowIfNoActivator => true;
    public ISearchEngineActivator[] ActivatorFilter => [];
    public IContextMenuLoader? ContextMenuLoader => null;
}

internal class ExitSearchEngine(IClassicDesktopStyleApplicationLifetime lifetime) : IStaticSearchEngine
{
    public string Id => nameof(ExitSearchEngine);
    public string Name => "Exit";
    public string ShortName => "Exit";
    public StarterIconSource Icon => Icons.Exit;
    public ISearchEngineActivator[] Activators => [];

    public event EventHandler? Changed
    {
        add { }
        remove { }
    }
    public event EventHandler<IEnumerable<ISearchResult>>? ResultsChanged
    {
        add { }
        remove { }
    }

    private static readonly IEnumerable<ISearchResult> SearchResults = [new ExitSearchResult()];

    public void SearchResultSelected(ISearchResult selectedSearchResult) => lifetime.TryShutdown();
    public ValueTask<IEnumerable<ISearchResult>> LoadResults() => ValueTask.FromResult(SearchResults);
}
