using Avalonia.Controls;
using R3;
using Serilog;
using Starter.Desktop.ViewModels;
using Starter.Features.Config;
using Starter.SearchEngine;

namespace Starter.Desktop;

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

internal class SettingsSearchEngine : StaticSearchEngine
{
    public override string Id => nameof(SettingsSearchEngine);
    public override string Name => "Settings";
    public override string ShortName => "Settings";
    public override StarterIconSource Icon => Icons.Settings;
    public override ISearchEngineActivator[] Activators => [];

    private static readonly IEnumerable<ISearchResult> Results =
    [
        new SettingsSearchResult("Logs", "Open logs", TargetPage.Logs, Icons.Logs),
        new SettingsSearchResult("Settings", "Open settings", TargetPage.Settings, Icons.Settings),
        new SettingsSearchResult("Options", "Open settings", TargetPage.Settings, Icons.Settings)
    ];

    private readonly SettingsWindowViewModel windowVm;
    private Views.SettingsWindow window;

    public BehaviorSubject<Configuration> Config => windowVm.Config;

    public SettingsSearchEngine(ILogger logger, Configuration config, SearchEngineStore searchEngineStore) : base("", "", logger)
    {
        windowVm = new SettingsWindowViewModel(config, searchEngineStore);
        window = new Views.SettingsWindow { DataContext = windowVm };
    }

    public override void SearchResultSelected(ISearchResult selectedSearchResult)
    {
        if (selectedSearchResult is not SettingsSearchResult result) return;
        switch (result.TargetPage)
        {
            case TargetPage.Logs: windowVm.OpenLogsPage(); break;
            case TargetPage.Settings: windowVm.OpenSettingsPage(); break;
            default: Log.Warning("Unexpected settings page: {Page}", result.TargetPage); break;
        }

        try
        {
            window.Show();
            window.Activate();
            window.WindowState = WindowState.Normal; // Unminimize if minimized
        }
        catch (Exception)
        {
            Log.Verbose("Failed to show window");
            window = new Views.SettingsWindow { DataContext = windowVm };
            window.Show();
        }
    }

    public override Control? LoadSettingsControl() => null;

    public override Task<(IEnumerable<ISearchResult>, Observable<IEnumerable<ISearchResult>>)> LoadResults() =>
        Task.FromResult((
            Results,
            Observable.Empty<IEnumerable<ISearchResult>>()
        ));
}
