using Avalonia.Controls;
using R3;
using Starter.Features.Config;
using Starter.SearchEngine;

namespace Starter.Desktop.ViewModels;

public record MenuItemViewModel(
    string Title,
    StarterIconSource Icon,
    Control Control
);

public partial class SettingsWindowViewModel : ObservableObject
{
    [ObservableProperty] private List<MenuItemViewModel> pages = [];
    [ObservableProperty] private MenuItemViewModel selectedPage; // Currently selected settings page

    private readonly SettingsViewModel settingsVm;
    public BehaviorSubject<Configuration> Config => settingsVm.Config;

    public SettingsWindowViewModel(Configuration config, SearchEngineStore searchEngineStore)
    {
        settingsVm = new SettingsViewModel(config, searchEngineStore);

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
            Pages.Add(new MenuItemViewModel(kv.Value.Name, kv.Value.Icon, control));
        }
    }
}
