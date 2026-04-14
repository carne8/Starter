using Avalonia.Controls;
using Avalonia.Controls.Templates;
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
    public readonly Dictionary<string, Control> SettingsControls = new();
    public readonly List<IDataTemplate> DataTemplates = new();

    // public event EventHandler? SearchEnginesChanged;

    public void LoadSearchEnginesFromDirectory(string directory, IClipboard clipboard)
    {
        foreach (var factory in SearchEngineLoading.loadFactoriesFromDirectory(directory))
        {
            LoadSearchEnginesFromFactory(factory, clipboard);
            if (factory.LoadDataTemplates() is { } dataTemplates)
                DataTemplates.AddRange(dataTemplates);
        }

        // SearchEnginesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void LoadSearchEnginesFromFactory(SearchEngineFactory factory, IClipboard clipboard)
    {
        foreach (var (engine, settingsControl) in SearchEngineLoading.loadSearchEnginesFromFactory(clipboard, factory))
        {
            switch (engine)
            {
                case IStaticSearchEngine staticEngine: StaticSearchEngines.Add(staticEngine); break;
                case IDynamicSearchEngine dynamicEngine: DynamicSearchEngines.Add(dynamicEngine); break;
                default:
                    Log.Warning("Unknown search engine type: {Engine}", engine);
                    return;
            }

            SearchEngines.Add(engine.Id, engine);
            if (settingsControl is not null) SettingsControls.Add(engine.Id, settingsControl);
        }
    }

    public void AddSearchEngine(IStaticSearchEngine se)
    {
        StaticSearchEngines.Add(se);
        SearchEngines.Add(se.Id, se);
        // SearchEnginesChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddSearchEngine(IDynamicSearchEngine se)
    {
        DynamicSearchEngines.Add(se);
        SearchEngines.Add(se.Id, se);
        // SearchEnginesChanged?.Invoke(this, EventArgs.Empty);
    }
}
