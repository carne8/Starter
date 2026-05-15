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
    private readonly IDictionary<string, ScoreDbEntry> resultScoreDb;

    public event EventHandler? HideWindow;
    public event EventHandler<int>? ClearTextBox;

    public GreetingVm GreetingVm { get; } = new();

    public BehaviorSubject<Configuration> Config { get; private set; }
    public IObservable<Configuration> ConfigSystemObservable { get; private set; }
    public ObservableList<SearchResultData> SearchResults => searchResultStore.Results;

    [ObservableProperty]
    public partial string Text { get; set; } = string.Empty;
    [ObservableProperty]
    public partial ISearchEngineActivator? Activator { get; set; }


    public MainWindowViewModel(
        BehaviorSubject<Configuration> config,
        IDictionary<string, ScoreDbEntry> resultScoreDb,
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
        foreach (var se in searchEngineStore.StaticSearchEngines) searchResultStore.AddSource(se);
        foreach (var se in searchEngineStore.DynamicSearchEngines) searchResultStore.AddSource(se);
    }

    private void IncreaseResultScore(ISearchResult searchResult)
    {
        if (searchResult.Id is null) return;
        ScoreDbModule.increaseResultScore(searchResult.Id, resultScoreDb);
        ScoreDbModule.runMaxAgingPolicy(Const.ScoresMaxAging, resultScoreDb);
        ScoreDbModule.writeToFile(Const.ResultScoresFile, resultScoreDb);
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
    private void ResetActivator() => Activator = null;
}
