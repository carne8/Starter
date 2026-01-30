using Avalonia.Controls;
using R3;
using Starter.Features.Config;
using Starter.SearchEngine;

namespace Starter.Desktop.ViewModels;

public partial class MenuItemViewModel(
    string title,
    StarterIconSource icon,
    Control control
) : ObservableObject
{
    public string Title { get; init; } = title;
    public Control Control { get; init; } = control;
    [ObservableProperty] private StarterIconSource icon = icon;

    public MenuItemViewModel(SearchEngine.SearchEngine engine, Control control) : this(engine.Name, engine.Icon, control) =>
        engine.Changed += (_, _) => Icon = engine.Icon;
}

public partial class SettingsWindowViewModel : ObservableObject
{
    [ObservableProperty] private List<MenuItemViewModel> pages = [];
    [ObservableProperty] private MenuItemViewModel selectedPage; // Currently selected settings page

    private readonly SettingsViewModel settingsVm;
    public BehaviorSubject<Configuration> Config => settingsVm.Config;
    public IObservable<Configuration> ConfigSystemObservable { get; private set; }

    public SettingsWindowViewModel(Configuration config, SearchEngineStore searchEngineStore)
    {
        settingsVm = new SettingsViewModel(config, searchEngineStore);
        ConfigSystemObservable = Config.AsSystemObservable();

        // Add Starter settings
        Pages.Add(new MenuItemViewModel(
            "Starter settings",
            Icons.Settings,
            new Views.Settings { DataContext = settingsVm }
        ));
        Pages.Add(new MenuItemViewModel(
            "Logs",
            Icons.Logs,
            new Views.Logs { DataContext = new LogsViewModel() }
        ));

        // Set default page
        selectedPage = pages[0]; // starter settings

        // Add search engine settings
        foreach (var kv in searchEngineStore.SearchEngines)
        {
            var control = kv.Value.LoadSettingsControl();
            if (control == null) continue;
            Pages.Add(new MenuItemViewModel(kv.Value, control));
        }
    }
}
