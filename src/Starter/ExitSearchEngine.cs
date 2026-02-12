using Avalonia.Controls;
using Avalonia.Threading;
using R3;
using Serilog;
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

internal class ExitSearchEngine() : StaticSearchEngine("", "", Log.Logger)
{
    public override string Id => nameof(ExitSearchEngine);
    public override string Name => "Exit";
    public override string ShortName => "Exit";
    public override StarterIconSource Icon => Icons.Exit;
    public override ISearchEngineActivator[] Activators => [];

    private static readonly IEnumerable<ISearchResult> SearchResults = [new ExitSearchResult()];

    public override void SearchResultSelected(ISearchResult selectedSearchResult) => Dispatcher.UIThread.InvokeShutdown();
    public override Control? LoadSettingsControl() => null;
    public override Task<(IEnumerable<ISearchResult>, Observable<IEnumerable<ISearchResult>>)> LoadResults() =>
        Task.FromResult((
            SearchResults,
            Observable.Empty<IEnumerable<ISearchResult>>()
        ));
}
