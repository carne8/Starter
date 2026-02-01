using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Serilog;
using Starter.Desktop.ViewModels;
using Starter.Desktop.Views;
using Starter.Features;
using Starter.Features.Config;
using Starter.Features.PlatformInterop;
using Const = Starter.Features.Constants;

namespace Starter.Desktop;

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

        DisableAvaloniaDataAnnotationValidation();
        lifetime.Exit += (_, _) =>
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
        Configuration.ensurePluginsSymlinkExists();

        var initialConfig = LoadConfiguration();
        var (searchEngineStore, config) = LoadSearchEngines(initialConfig);

        var resultScoreDb = ScoreDbModule.readFromFile(Const.ResultScoresFile);
        var activatorStore = new ActivatorStore(config);
        foreach (var kv in searchEngineStore.SearchEngines) activatorStore.AddSearchEngineActivators(kv.Value);

        // Create the window
        var viewModel = new MainWindowViewModel(config, resultScoreDb, searchEngineStore, activatorStore);
        window = new MainWindow { DataContext = viewModel };

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

    private static (SearchEngineStore, R3.BehaviorSubject<Configuration>) LoadSearchEngines(Configuration config)
    {
        var searchEngineStore = new SearchEngineStore();
#if DEBUG
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.UrlSearchEngine/bin/Debug/net10.0/");
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.WebSearchEngine/bin/Debug/net10.0/");
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.WorkspaceSearchEngine/bin/Debug/net10.0/");
        searchEngineStore.LoadSearchEnginesFromDirectory(
            OperatingSystem.IsWindows()
                ? "./src/Starter.ApplicationSearchEngine/bin/Debug/net10.0-windows10.0.19041.0/"
                : "./src/Starter.ApplicationSearchEngine/bin/Debug/net10.0/"
        );
#else
        Directory.GetDirectories(Constants.PluginsDirectory);
#endif

        var settingsSearchEngine = new SettingsSearchEngine(
            Log.Logger.ForContext("Context", "Starter/Settings"),
            config,
            searchEngineStore
        );
        searchEngineStore.AddSearchEngine(settingsSearchEngine);

        Log.Debug("Plugins loaded");
        return (searchEngineStore, settingsSearchEngine.Config);
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
        // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins

        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
