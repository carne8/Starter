using Starter.Features;
using Starter.SearchEngine;

namespace Starter.Desktop;

public class SearchEngineStore
{
    public List<StaticSearchEngine> StaticSearchEngines = [];
    public List<DynamicSearchEngine> DynamicSearchEngines = [];
    public Dictionary<string, Starter.SearchEngine.SearchEngine> SearchEngines = new();

    // public event EventHandler? SearchEnginesChanged;

    public void LoadSearchEnginesFromDirectory(string directory)
    {
        var (staticSe, dynamicSe) = SearchEngineLoading.loadSearchEngineFromDirectory(directory);

        StaticSearchEngines.AddRange(staticSe);
        DynamicSearchEngines.AddRange(dynamicSe);

        foreach (var se in staticSe) SearchEngines.Add(se.Id, se);
        foreach (var se in dynamicSe) SearchEngines.Add(se.Id, se);

        // SearchEnginesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddSearchEngine(StaticSearchEngine se)
    {
        StaticSearchEngines.Add(se);
        SearchEngines.Add(se.Id, se);
        // SearchEnginesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddSearchEngine(DynamicSearchEngine se)
    {
        DynamicSearchEngines.Add(se);
        SearchEngines.Add(se.Id, se);
        // SearchEnginesChanged?.Invoke(this, EventArgs.Empty);
    }
}
