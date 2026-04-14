using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using R3;
using Serilog;
using Starter.Features;
using Starter.Features.Config;
using Starter.Features.PlatformInterop;
using Starter.ViewModels;
using Starter.Views;
using Const = Starter.Features.Constants;

namespace Starter;

public class App : Application
{
    private Window? window;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            Log.Fatal("Unexpected ApplicationLifetime is not initialized.");
            throw new Exception("Unexpected ApplicationLifetime is not initialized.");
        }

        lifetime.ShutdownRequested += (_, _) =>
        {
            Log.Information("---*--- Exiting ---*---");
            Log.CloseAndFlushAsync().AsTask().Wait();
        };

        try
        {
            Launch(lifetime);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error during initialization.");
            lifetime.Shutdown();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void Launch(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        window = new MainWindow();
        if (window.Clipboard is null) throw new Exception("No clipboard");
        Configuration.ensurePluginsSymlinkExists();

        var initialConfig = LoadConfiguration();
        var (searchEngineStore, config) = LoadSearchEngines(initialConfig, window.Clipboard);

        var resultScoreDb = ScoreDbModule.readFromFile(Const.ResultScoresFile);
        var activatorStore = new ActivatorStore(config);
        foreach (var kv in searchEngineStore.SearchEngines) activatorStore.AddSearchEngineActivators(kv.Value);

        // Create the window
        window.DataContext = new MainWindowViewModel(config, resultScoreDb, searchEngineStore, activatorStore);

        // Register hotkey
        var keyboardShortcut = initialConfig.KeyboardShortcut;
        var platformInterop = PlatformInteropFactory.GetPlatformInterop();
        if (!platformInterop.HotkeyRegistrable) return;

        platformInterop
            .RegisterHotkey(keyboardShortcut, window)
            .AsTask()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Log.Error(task.Exception, "Failed to setup keyboard shortcut");
                    lifetime.Shutdown();
                    return;
                }
                Log.Debug("Launched");
            });
    }

    private static Configuration LoadConfiguration()
    {
        var configRes = Configuration.loadFromFile(Const.ConfigFile);
        if (configRes.IsError)
        {
            Log.Fatal("Failed to decode configuration: {ConfigErrorValue}", configRes.ErrorValue);
            throw new Exception($"Failed to decode configuration: {configRes.ErrorValue}");
        }

        Log.Debug("Config loaded");
        return configRes.ResultValue;
    }

    private static async void UpdateConfiguration(Configuration config)
    {
        try
        {
            var res = await Configuration.save(Const.ConfigFile, config);
            if (!res.IsError) return;
            Log.Error("Failed to save configuration: {ConfigError}", res.ErrorValue);
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to save configuration");
        }
    }

    private(SearchEngineStore, BehaviorSubject<Configuration>) LoadSearchEngines(Configuration config, IClipboard clipboard)
    {
        var searchEngineStore = new SearchEngineStore();
#if DEBUG
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.UrlSearchEngine/bin/Debug/net10.0/", clipboard);
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.WebSearchEngine/bin/Debug/net10.0/", clipboard);
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.WorkspaceSearchEngine/bin/Debug/net10.0/", clipboard);
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.CalculatorSearchEngine/bin/Debug/net10.0/", clipboard);
        searchEngineStore.LoadSearchEnginesFromDirectory(
            OperatingSystem.IsWindows()
                ? "./src/Starter.ApplicationSearchEngine/bin/Debug/net10.0-windows10.0.19041.0/"
                : "./src/Starter.ApplicationSearchEngine/bin/Debug/net10.0/",
            clipboard
        );

        if (OperatingSystem.IsWindows())
            searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.EverythingSearchEngine/bin/Debug/net10.0/", clipboard);
#else
        foreach (var pluginDir in Directory.GetDirectories(Const.PluginsDirectory))
            searchEngineStore.LoadSearchEnginesFromDirectory(pluginDir, clipboard);
#endif

        var settingsSearchEngine = new SettingsSearchEngine(
            Log.Logger.ForContext("Context", "Starter/Settings"),
            config,
            searchEngineStore
        );
        settingsSearchEngine.Config.Subscribe(UpdateConfiguration);
        searchEngineStore.AddSearchEngine(settingsSearchEngine);

        searchEngineStore.AddSearchEngine(new ExitSearchEngine());

        DataTemplates.AddRange(searchEngineStore.DataTemplates);
        searchEngineStore.DataTemplates.Clear();

        Log.Debug("Plugins loaded");
        return (searchEngineStore, settingsSearchEngine.Config);
    }
}
