using Avalonia.Platform;
using R3;
using Serilog;
using Starter.Features;
using Starter.Features.Config;
using Starter.Features.CustomCollections;
using Starter.SearchEngine;
using Const = Starter.Features.Constants;

namespace Starter.ViewModels;

using FuzzyMatchResults = Dictionary<IContextMenuEntry, TextMatching.FuzzyMatch.FuzzyResult>;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SearchEngineStore searchEngineStore;
    private readonly SearchResultStore searchResultStore;
    private readonly ContextMenuStore contextMenuStore;
    private readonly ActivatorStore activatorStore;
    private readonly IScoreDb resultScoreDb;

    public event EventHandler? HideWindow;
    public event EventHandler<int>? ClearTextBox;

    public GreetingVm GreetingVm { get; } = new();

    public BehaviorSubject<Configuration> Config { get; private set; }
    public IObservable<Configuration> ConfigSystemObservable { get; private set; }
    public ContextMenuStore ContextMenu => contextMenuStore;
    public ObservableList<SearchResultData> SearchResults => searchResultStore.Results;
    public ObservableList<IContextMenuEntry> ContextMenuResults => contextMenuStore.ContextMenuResults;
    public IObservable<TimeSpan?> LoadingTime => searchResultStore.LoadingTimes.AsSystemObservable();

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;
    [ObservableProperty]
    public partial SearchEngineActivator? Activator { get; set; }


    public MainWindowViewModel(
        BehaviorSubject<Configuration> config,
        IScoreDb resultScoreDb,
        SearchEngineStore searchEngineStore,
        ActivatorStore activatorStore,
        FuzzyMatchResults fuzzyMatchResults
    )
    {
        Config = config;
        ConfigSystemObservable = config.AsSystemObservable();

        this.activatorStore = activatorStore;
        this.searchEngineStore = searchEngineStore;
        this.resultScoreDb = resultScoreDb;

        searchResultStore = new SearchResultStore(resultScoreDb, searchEngineStore.SearchEngines);
        searchEngineStore.SearchEngineAdded += se =>
        {
            if (se is StaticSearchEngine staticSe) searchResultStore.AddSource(staticSe);
            else if (se is DynamicSearchEngine dynamicSe) searchResultStore.AddSource(dynamicSe);
        };
        foreach (var se in searchEngineStore.StaticSearchEngines) searchResultStore.AddSource(se);
        foreach (var se in searchEngineStore.DynamicSearchEngines) searchResultStore.AddSource(se);

        contextMenuStore = new ContextMenuStore(resultScoreDb, fuzzyMatchResults);
    }

    private void IncreaseResultScore(string resultId)
    {
        resultScoreDb.IncreaseResultScore(resultId);
        resultScoreDb.RunMaxAgingPolicy();
        resultScoreDb.SaveToFile(Const.ResultScoresFile);
        searchResultStore.SortResults(); // Sort results for next opening
    }

    private void IncreaseResultScore(SearchResult searchResult)
    {
        if (searchResult.Id is null) return;
        IncreaseResultScore(searchResult.Id);
    }

    private void IncreaseResultScore(IContextMenuEntry result)
    {
        if (result.Id is null) return;
        IncreaseResultScore(result.Id);
    }

    partial void OnTextChanged(string value)
    {
        if (ContextMenu.ContextMenuEnabled)
        {
            contextMenuStore.Query(value);
            return;
        }

        var res = activatorStore.GetActivatorFromText(value);
        if (res.IsSome)
        {
            var (activator, prefix) = res.Value;
            ClearTextBox?.Invoke(this, prefix.Length);
            searchResultStore.ClearResults();
            Activator = activator;
        }
        else searchResultStore.Query(value, Activator);
    }

    partial void OnActivatorChanged(SearchEngineActivator? value)
    {
        if (value is not null) return;
        searchResultStore.Query(Text, value);
    }

    [RelayCommand]
    private void SelectResult(SearchResultData searchResult)
    {
        HideWindow?.Invoke(this, EventArgs.Empty);

        if (!searchEngineStore.SearchEngines.TryGetValue(searchResult.SearchEngineId, out var searchEngine))
        {
            Log.Error("Could not find search engine {SearchEngineId}", searchResult.SearchEngineId);
            return;
        }

        try
        {
            searchEngine.SearchResultSelected(searchResult.SearchResult);
        }
        catch (Exception exn)
        {
            Log.Error(exn, "Failed to select result: {Result}", searchResult.SearchResult.Name);
        }

        IncreaseResultScore(searchResult.SearchResult);
    }

    public bool SelectContextMenuResult(IContextMenuEntry entry, IPlatformHandle platformHandle)
    {
        try
        {
            var newContextMenu = entry.Invoke();
            if (newContextMenu is null)
            {
                HideWindow?.Invoke(this, EventArgs.Empty);
                return false;
            }

            contextMenuStore.SetContextMenu(platformHandle, newContextMenu);
            if (Text == string.Empty) OnTextChanged(Text);
            else Text = string.Empty;

            IncreaseResultScore(entry);
            return true;
        }
        catch (Exception exn)
        {
            Log.Error(exn, "Failed to invoke context menu result: {Result}", entry.Name);
            return false;
        }
    }

    [RelayCommand]
    private void ResetActivator() => Activator = null;

    public bool OpenContextMenu(SearchResultData result, IPlatformHandle platformHandle)
    {
        try
        {
            var contextMenu = result.SearchResult.ContextMenuLoader;
            if (contextMenu is null) return false;

            contextMenuStore.SetContextMenu(platformHandle, contextMenu);
            Text = string.Empty;

            IncreaseResultScore(result.SearchResult);
            return true;
        }
        catch (Exception exn)
        {
            Log.Error(exn, "Failed to open context menu: {Result}", result.SearchResult.Name);
            return false;
        }
    }

    [RelayCommand]
    private void CloseContextMenu()
    {
        contextMenuStore.ExitContextMenu();
        OnTextChanged(Text);
    }
}
