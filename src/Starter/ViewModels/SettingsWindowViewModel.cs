using Avalonia.Controls;
using Avalonia.Platform.Storage;
using R3;
using Starter.Features.Config;
using Starter.SearchEngine;

namespace Starter.ViewModels;

public partial class MenuItemViewModel(
    string title,
    StarterIconSource icon,
    Control control
) : ObservableObject
{
    public string Title { get; init; } = title;
    public Control Control { get; init; } = control;
    [ObservableProperty] private StarterIconSource icon = icon;

    public MenuItemViewModel(ISearchEngine engine, Control control) : this(engine.Name, engine.Icon, control) =>
        engine.Changed += (_, _) => Icon = engine.Icon;
}

public partial class SettingsWindowViewModel : ObservableObject
{
    [ObservableProperty] private List<MenuItemViewModel> pages = [];
    [ObservableProperty] private MenuItemViewModel selectedPage; // Currently selected settings page

    private readonly MenuItemViewModel settingsPage;
    private readonly MenuItemViewModel logsPage;

    private readonly SettingsViewModel settingsVm;
    public BehaviorSubject<Configuration> Config => settingsVm.Config;
    public IObservable<Configuration> ConfigSystemObservable { get; private set; }

    public SettingsWindowViewModel(SearchEngineStore searchEngineStore, SettingsViewModel settingsVm)
    {
        this.settingsVm = settingsVm;
        ConfigSystemObservable = Config.AsSystemObservable();

        // Add Starter settings
        settingsPage = new MenuItemViewModel(
            "Starter settings",
            Icons.Settings,
            new Views.Settings { DataContext = settingsVm }
        );
        logsPage = new MenuItemViewModel(
            "Logs",
            Icons.Logs,
            new Views.Logs { DataContext = new LogsViewModel() }
        );
        Pages.Add(settingsPage);
        Pages.Add(logsPage);

        // Set default page
        selectedPage = settingsPage;

        // Add search engine settings
        foreach (var kv in searchEngineStore.SettingsControls)
        {
            if (!searchEngineStore.SearchEngines.TryGetValue(kv.Key, out var engine)) continue;
            Pages.Add(new MenuItemViewModel(engine, kv.Value));
        }
    }

    public void OpenSettingsPage() => SelectedPage = settingsPage;
    public void OpenLogsPage() => SelectedPage = logsPage;
}
