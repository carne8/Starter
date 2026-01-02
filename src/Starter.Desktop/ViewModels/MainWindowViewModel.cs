using Serilog;
using Starter.Features;
using Starter.Features.Config;
using Starter.Features.CustomCollections;
using Starter.SearchEngine;

namespace Starter.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SearchResultStore searchResultStore;
    private readonly ActivatorStore activatorStore;

    public event EventHandler? HideWindow;
    public event EventHandler<int>? ClearTextBox;

    [ObservableProperty] private Configuration config;
    [ObservableProperty] private string text = string.Empty;
    [ObservableProperty] private ISearchEngineActivator? activator;
    public ObservableList<SearchResultData> SearchResults => searchResultStore.Results;

    public MainWindowViewModel(
        Configuration config,
        IDictionary<string, ScoreDbEntry> resultScoreDb,
        SearchEngineStore searchEngineStore,
        ActivatorStore activatorStore
    )
    {
        this.activatorStore = activatorStore;
        this.config = config;

        searchResultStore = new SearchResultStore(resultScoreDb, searchEngineStore.SearchEngines);
        foreach (var se in searchEngineStore.StaticSearchEngines) searchResultStore.AddSource(se);
        foreach (var se in searchEngineStore.DynamicSearchEngines) searchResultStore.AddSource(se);
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

    partial void OnActivatorChanged(ISearchEngineActivator? value) => searchResultStore.Query(Text, value);
    partial void OnConfigChanged(Configuration value) => activatorStore.SetConfig(value);

    [RelayCommand]
    private void SelectResult(SearchResultData searchResult)
    {
        Log.Debug("Selected {Result}", searchResult.SearchResult.Name);
        HideWindow?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ResetActivator() => Activator = null;
}
