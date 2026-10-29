using Avalonia.Controls;
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

file class SettingsSearchResult(string name, string description, TargetPage targetPage, StarterIconSource icon) : SearchResult
{
    public TargetPage TargetPage { get; } = targetPage;
    public override string Name => name;
    public override string Description => description;
    public override StarterIconSource Icon => icon;
    public override bool ShowIfNoActivator => true;
}

internal class SettingsSearchEngine(ILogger logger, SettingsWindowViewModel windowVm) : StaticSearchEngine
{
    public override string Id => nameof(SettingsSearchEngine);
    public override string Name => "Settings";
    public override StarterIconSource Icon => Icons.Settings;

    private static readonly IEnumerable<SearchResult> Results =
    [
        new SettingsSearchResult("Logs", "Open logs", TargetPage.Logs, Icons.Logs),
        new SettingsSearchResult("Settings", "Open settings", TargetPage.Settings, Icons.Settings),
        new SettingsSearchResult("Options", "Open settings", TargetPage.Settings, Icons.Settings)
    ];
    private Views.SettingsWindow? window;

    public BehaviorSubject<Configuration> Config => windowVm.Config;

    public override void SearchResultSelected(SearchResult selectedSearchResult)
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

    public override ValueTask<IEnumerable<SearchResult>> LoadResults() => ValueTask.FromResult(Results);
}
