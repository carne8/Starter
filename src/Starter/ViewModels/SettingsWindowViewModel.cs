using Avalonia.Controls.Templates;
using R3;
using Starter.Features.Config;
using Starter.SearchEngine;

namespace Starter.ViewModels;

public partial class MenuItemViewModel(
    string title,
    StarterIconSource icon,
    object vm,
    IDataTemplate dataTemplate
) : ObservableObject
{
    public string Title { get; init; } = title;
    public object ViewModel { get; init; } = vm;
    public IDataTemplate DataTemplate { get; init; } = dataTemplate;

    [ObservableProperty] public partial StarterIconSource Icon { get; set; } = icon;

    public MenuItemViewModel(ISearchEngine engine, object vm, IDataTemplate dataTemplate) :
        this(engine.Name, engine.Icon, vm, dataTemplate)
        =>
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
            settingsVm,
            new FuncDataTemplate<SettingsViewModel>((vm, _) =>
                new Views.Settings { DataContext = vm }
            )
        );
        logsPage = new MenuItemViewModel(
            "Logs",
            Icons.Logs,
            new LogsViewModel(),
            new FuncDataTemplate<LogsViewModel>((vm, _) =>
                new Views.Logs { DataContext = vm }
            )
        );
        Pages.Add(settingsPage);
        Pages.Add(logsPage);

        // Set default page
        selectedPage = settingsPage;

        // Add search engine settings
        searchEngineStore.SearchEngineSettingsAdded += (seId, settings) =>
        {
            if (!searchEngineStore.SearchEngines.TryGetValue(seId, out var engine)) return;
            Pages.Add(new MenuItemViewModel(
                engine,
                settings.DataContext,
                settings.DataTemplate
            ));
        };
        foreach (var kv in searchEngineStore.Settings)
        {
            if (!searchEngineStore.SearchEngines.TryGetValue(kv.Key, out var engine)) continue;
            Pages.Add(new MenuItemViewModel(
                engine,
                kv.Value.DataContext,
                kv.Value.DataTemplate
            ));
        }
    }

    public void OpenSettingsPage() => SelectedPage = settingsPage;
    public void OpenLogsPage() => SelectedPage = logsPage;
}
