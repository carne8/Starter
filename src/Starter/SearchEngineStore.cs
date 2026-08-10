using Avalonia.Input.Platform;
using Serilog;
using Starter.Features;
using Starter.SearchEngine;

namespace Starter;

public class SearchEngineStore
{
    public readonly List<IStaticSearchEngine> StaticSearchEngines = [];
    public readonly List<IDynamicSearchEngine> DynamicSearchEngines = [];
    public readonly Dictionary<string, ISearchEngine> SearchEngines = new();
    public readonly Dictionary<string, SearchEngineFactory.SearchEngineSettings> Settings = new();

    public event Action<ISearchEngine>? SearchEngineAdded;
    public event Action<string, SearchEngineFactory.SearchEngineSettings>? SearchEngineSettingsAdded;

    public void LoadSearchEnginesFromDirectory(string directory, IClipboard clipboard)
    {
        foreach (var factory in SearchEngineLoading.loadFactoriesFromDirectory(directory))
            LoadSearchEnginesFromFactory(factory, clipboard);
    }

    private ILogger log = Log.ForContext("Context", "Starter/EngineLoading");

    private void LoadSearchEnginesFromFactory(SearchEngineFactory factory, IClipboard clipboard)
    {
        foreach (var (engine, settings) in SearchEngineLoading.loadSearchEnginesFromFactory(clipboard, factory))
        {
            switch (engine)
            {
                case IStaticSearchEngine staticEngine: StaticSearchEngines.Add(staticEngine); break;
                case IDynamicSearchEngine dynamicEngine: DynamicSearchEngines.Add(dynamicEngine); break;
                default:
                    log.Warning("Unknown search engine type: {Engine}", engine);
                    return;
            }

            if (!SearchEngines.TryAdd(engine.Id, engine))
            {
                log.Error(
                    "Several engines have the same id: ({Engine1}, {Engine1Name}) and ({Engine2}, {Engine2Name})",
                    engine.Id,
                    engine.Name,
                    engine.Id,
                    SearchEngines[engine.Id].Name
                );
                return;
            }

            SearchEngineAdded?.Invoke(engine);
            log.Information("Loaded {EngineId}", engine.Id);
            if (settings is not null)
            {
                Settings.Add(engine.Id, settings);
                SearchEngineSettingsAdded?.Invoke(engine.Id, settings);
            }
        }
    }

    public void AddSearchEngine(IStaticSearchEngine se)
    {
        StaticSearchEngines.Add(se);
        SearchEngines.Add(se.Id, se);
        SearchEngineAdded?.Invoke(se);
    }

    public void AddSearchEngine(IDynamicSearchEngine se)
    {
        DynamicSearchEngines.Add(se);
        SearchEngines.Add(se.Id, se);
        SearchEngineAdded?.Invoke(se);
    }
}
