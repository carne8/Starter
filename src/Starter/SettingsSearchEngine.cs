using Avalonia.Controls;
using Avalonia.Platform.Storage;
using R3;
using Serilog;
using Starter.Features.Config;
using Starter.SearchEngine;
using Starter.ViewModels;

namespace Starter;

file enum TargetPage
{
    Settings,
    Logs
}

file class SettingsSearchResult(string name, string description, TargetPage targetPage, StarterIconSource icon)
    : ISearchResult
{
    public TargetPage TargetPage { get; } = targetPage;
    public string? Id => null;
    public string Name => name;
    public string Description => description;
    public string[]? Keywords => null;
    public StarterIconSource Icon => icon;
    public bool ShowIfNoActivator => true;
    public ISearchEngineActivator[] ActivatorFilter => [];
}

internal class SettingsSearchEngine(ILogger logger, ILauncher launcher, Configuration config, SearchEngineStore searchEngineStore) : IStaticSearchEngine
{
    public string Id => nameof(SettingsSearchEngine);
    public string Name => "Settings";
    public string ShortName => "Settings";
    public StarterIconSource Icon => Icons.Settings;
    public ISearchEngineActivator[] Activators => [];

    public event EventHandler? Changed
    {
        add { }
        remove { }
    }
    public event EventHandler<IEnumerable<ISearchResult>>? ResultsChanged
    {
        add { }
        remove { }
    }

    private static readonly IEnumerable<ISearchResult> Results =
    [
        new SettingsSearchResult("Logs", "Open logs", TargetPage.Logs, Icons.Logs),
        new SettingsSearchResult("Settings", "Open settings", TargetPage.Settings, Icons.Settings),
        new SettingsSearchResult("Options", "Open settings", TargetPage.Settings, Icons.Settings)
    ];

    private readonly SettingsWindowViewModel windowVm = new(launcher, config, searchEngineStore);
    private Views.SettingsWindow? window;

    public BehaviorSubject<Configuration> Config => windowVm.Config;

    public void SearchResultSelected(ISearchResult selectedSearchResult)
    {
        if (selectedSearchResult is not SettingsSearchResult result) return;
        switch (result.TargetPage)
        {
            case TargetPage.Logs: windowVm.OpenLogsPage(); break;
            case TargetPage.Settings: windowVm.OpenSettingsPage(); break;
            default: logger.Warning("Unexpected settings page: {Page}", result.TargetPage); break;
        }

        try
        {
            // `window` is null at the beginning
            // That, the first time we open the settings, we set
            // CompositionBackdropCornerRadius to zero, fixing the window corner radius.
            if (window is null) throw new NullReferenceException();

            window.Show();
            window.Activate();
            window.WindowState = WindowState.Normal; // Unminimize if minimized
        }
        catch (Exception)
        {
            logger.Verbose("Failed to show window");
            Program.Win32PlatformOptions.WinUICompositionBackdropCornerRadius = 0;
            window = new Views.SettingsWindow { DataContext = windowVm };
            window.Show();
        }
    }

    public ValueTask<IEnumerable<ISearchResult>> LoadResults() => ValueTask.FromResult(Results);
}
