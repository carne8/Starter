using R3;
using Serilog;
using Starter.Features;
using Starter.Features.Config;
using Starter.Features.CustomCollections;
using Starter.SearchEngine;
using Const = Starter.Features.Constants;

namespace Starter.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SearchResultStore searchResultStore;
    private readonly SearchEngineStore searchEngineStore;
    private readonly ActivatorStore activatorStore;
    private readonly IScoreDb resultScoreDb;

    public event EventHandler? HideWindow;
    public event EventHandler<int>? ClearTextBox;

    public GreetingVm GreetingVm { get; } = new();

    public BehaviorSubject<Configuration> Config { get; private set; }
    public IObservable<Configuration> ConfigSystemObservable { get; private set; }
    public ObservableList<SearchResultData> SearchResults => searchResultStore.Results;
    public ObservableList<ContextMenuResultData> ContextMenuResults => searchResultStore.ContextMenuResults;
    public IObservable<TimeSpan?> LoadingTime => searchResultStore.LoadingTimes.AsSystemObservable();

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;
    [ObservableProperty]
    public partial ISearchEngineActivator? Activator { get; set; }

    private string? textBeforeContextMenu;
    [ObservableProperty]
    public partial bool ContextMenuActivated { get; set; }


    public MainWindowViewModel(
        BehaviorSubject<Configuration> config,
        IScoreDb resultScoreDb,
        SearchEngineStore searchEngineStore,
        ActivatorStore activatorStore
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
            if (se is IStaticSearchEngine staticSe) searchResultStore.AddSource(staticSe);
            else if (se is IDynamicSearchEngine dynamicSe) searchResultStore.AddSource(dynamicSe);
        };
        foreach (var se in searchEngineStore.StaticSearchEngines) searchResultStore.AddSource(se);
        foreach (var se in searchEngineStore.DynamicSearchEngines) searchResultStore.AddSource(se);
    }

    private void IncreaseResultScore(ISearchResult searchResult)
    {
        if (searchResult.Id is null) return;
        resultScoreDb.IncreaseResultScore(searchResult.Id);
        resultScoreDb.RunMaxAgingPolicy();
        resultScoreDb.SaveToFile(Const.ResultScoresFile);
        searchResultStore.SortResults(); // Sort results for next opening
    }

    partial void OnTextChanged(string value)
    {
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

    partial void OnActivatorChanged(ISearchEngineActivator? value)
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

    [RelayCommand]
    private void SelectContextMenuResult(ContextMenuResultData resultData)
    {
        HideWindow?.Invoke(this, EventArgs.Empty);

        try
        {
            resultData.Result.Invoke();
        }
        catch (Exception exn)
        {
            Log.Error(exn, "Failed to select context menu result: {Result}", resultData.Name);
        }
    }

    [RelayCommand]
    private void ResetActivator() => Activator = null;

    [RelayCommand]
    private void OpenContextMenu(SearchResultData? searchResult)
    {
        var contextMenu = searchResult?.SearchResult.GetContextMenu();
        if (contextMenu is null) return;

        searchResultStore.SetContextMenu(contextMenu);
        ContextMenuActivated = true;
        textBeforeContextMenu = Text;
        Text = string.Empty;
    }

    [RelayCommand]
    private void CloseContextMenu()
    {
        searchResultStore.ExitContextMenu();
        if (textBeforeContextMenu is not null) Text = textBeforeContextMenu;
        ContextMenuActivated = false;
    }
}
