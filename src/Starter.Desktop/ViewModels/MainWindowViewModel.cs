using Serilog;
using Starter.Features;
using Starter.Features.Config;
using Starter.Features.ResultScores;
using Starter.Features.CustomCollections;

namespace Starter.Desktop.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SearchResultStore.SearchResultStore searchResultStore;

    [ObservableProperty] private Configuration config;
    [ObservableProperty] private string text = string.Empty;

    public ObservableList<SearchResultData> SearchResults => searchResultStore.Results;

    public MainWindowViewModel(
        Configuration config,
        IDictionary<string, ScoreDbEntry> resultScoreDb,
        SearchEngineStore searchEngineStore
    )
    {
        searchResultStore = new SearchResultStore.SearchResultStore(resultScoreDb);
        foreach (var se in searchEngineStore.StaticSearchEngines) searchResultStore.AddSource(se);
        foreach (var se in searchEngineStore.DynamicSearchEngines) searchResultStore.AddSource(se);

        this.config = config;
    }

    public event EventHandler? HideWindow;
    public event EventHandler? ClearTextBox;

    partial void OnTextChanged(string value) => searchResultStore.Query(value);

    [RelayCommand]
    private void SelectResult(SearchResultData searchResult)
    {
        Log.Debug("Selected {Result}", searchResult.SearchResult.Name);
        ClearTextBox?.Invoke(this, EventArgs.Empty);
        HideWindow?.Invoke(this, EventArgs.Empty);
    }
}
