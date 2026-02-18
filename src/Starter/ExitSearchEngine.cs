using Avalonia.Threading;
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
}

internal class ExitSearchEngine : IStaticSearchEngine
{
    public string Id => nameof(ExitSearchEngine);
    public string Name => "Exit";
    public string ShortName => "Exit";
    public StarterIconSource Icon => Icons.Exit;
    public ISearchEngineActivator[] Activators => [];
    public event EventHandler? Changed;
    public event EventHandler<IEnumerable<ISearchResult>>? ResultsChanged;

    private static readonly IEnumerable<ISearchResult> SearchResults = [new ExitSearchResult()];

    public void SearchResultSelected(ISearchResult selectedSearchResult) => Dispatcher.UIThread.InvokeShutdown();
    public ValueTask<IEnumerable<ISearchResult>> LoadResults() => ValueTask.FromResult(SearchResults);
}
