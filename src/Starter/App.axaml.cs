using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
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
    public bool IsTestMode = false;
    private MainWindow? window;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            base.OnFrameworkInitializationCompleted();
            if (IsTestMode) return;
            Log.Fatal("Unexpected ApplicationLifetime is not initialized.");
            throw new Exception("Unexpected ApplicationLifetime is not initialized.");
        }

        lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        lifetime.ShutdownRequested += (_, _) =>
        {
            Log.Information("---*--- Exiting ---*---");
            Log.CloseAndFlush();
        };

        // UI thread exceptions
        Dispatcher.UIThread.UnhandledException += (_, e) => Log.Error(e.Exception, "Unhandled UI exception");

        try
        {
            Launch(lifetime);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal error during initialization.");
            lifetime.TryShutdown();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void Launch(IClassicDesktopStyleApplicationLifetime lifetime)
    {
        var serviceCollection = new ServiceCollection();
        if (OperatingSystem.IsWindows())
            serviceCollection.AddSingleton<IPlatformInterop, WindowsPlatformInterop>();
        else if (OperatingSystem.IsLinux())
            serviceCollection.AddSingleton<IPlatformInterop, LinuxPlatformInterop>();
        else
            throw new PlatformNotSupportedException();

        serviceCollection.AddKeyedSingleton("initial-config", (provider, _) =>
        {
            var config = provider.GetRequiredService<IPlatformInterop>();
            return LoadConfiguration(config);
        });

        // Main window
        serviceCollection.AddSingleton<MainWindow>(provider =>
        {
            var platformInterop = provider.GetRequiredService<IPlatformInterop>();
            return new MainWindow(platformInterop);
        });

        serviceCollection.AddSingleton(lifetime);
        serviceCollection.AddSingleton<ILauncher>(provider => provider.GetRequiredService<MainWindow>().Launcher);
        serviceCollection.AddSingleton<IClipboard>(provider =>
        {
            var window = provider.GetRequiredService<MainWindow>();
            return window.Clipboard ?? throw new Exception("No clipboard");
        });

        // Engine store
        serviceCollection.AddSingleton<SearchEngineStore>(provider =>
        {
            var clipboard = provider.GetRequiredService<IClipboard>();
            var appLifetime = provider.GetRequiredService<IClassicDesktopStyleApplicationLifetime>();
            var engineStore = new SearchEngineStore();

            Task.Run(() => LoadSearchEngines(engineStore, clipboard, appLifetime));

            return engineStore;
        });

        // Settings
        serviceCollection.AddSingleton<BehaviorSubject<Configuration>>(provider =>
        {
            var engineStore = provider.GetRequiredService<SearchEngineStore>();
            var settingsWindowViewModel = provider.GetRequiredService<SettingsWindowViewModel>();

            var settings = new SettingsSearchEngine(
                Log.Logger.ForContext("Context", "Starter/Settings"),
                settingsWindowViewModel
            );

            engineStore.AddSearchEngine(settings);

            settings.Config.Subscribe(UpdateConfiguration);
            return settings.Config;
        });

        // Load other things
        serviceCollection.AddSingleton<IScoreDb>(
            ScoreDb.ReadFromFile(Const.ResultScoresFile, Const.ScoresMaxAging)
        );
        serviceCollection.AddSingleton<ActivatorStore>(provider =>
        {
            var config = provider.GetRequiredService<BehaviorSubject<Configuration>>();
            var engineStore = provider.GetRequiredService<SearchEngineStore>();

            var activatorStore = new ActivatorStore(config);
            engineStore.SearchEngineAdded += activatorStore.AddSearchEngineActivators;
            foreach (var kv in engineStore.SearchEngines)
                activatorStore.AddSearchEngineActivators(kv.Value);

            return activatorStore;
        });

        // View models
        serviceCollection.AddTransient<KeyboardShortcutInputViewModel>();
        serviceCollection.AddTransient<SettingsViewModel>();
        serviceCollection.AddTransient<SettingsWindowViewModel>();
        serviceCollection.AddTransient<MainWindowViewModel>();

        // Start things
        var serviceProvider = serviceCollection.BuildServiceProvider();
        window = serviceProvider.GetRequiredService<MainWindow>();
        window.DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>();

        // Register hotkey
        var initialConfig = serviceProvider.GetRequiredKeyedService<Configuration>("initial-config");
        var platformInterop = serviceProvider.GetRequiredService<IPlatformInterop>();
        if (!platformInterop.HotkeyRegistrable) return;

        platformInterop
            .RegisterHotkey(initialConfig.KeyboardShortcut, window)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Log.Error(task.Exception, "Failed to setup keyboard shortcut");
                    lifetime.TryShutdown();
                    return;
                }
                Log.Debug("Launched");
            });
    }

    private static Configuration LoadConfiguration(IPlatformInterop platform)
    {
        Configuration.ensureDirectoriesExists();
        var configRes = Configuration.loadFromFile(Const.ConfigFile);
        if (configRes.IsError)
        {
            Log.Fatal("Failed to decode configuration: {ConfigErrorValue}", configRes.ErrorValue);
            throw new Exception($"Failed to decode configuration: {configRes.ErrorValue}");
        }

        Log.Debug("Config loaded");
        return platform.EnsureConfigCompatibility(configRes.ResultValue);
    }

    private static void LoadSearchEngines(SearchEngineStore searchEngineStore, IClipboard clipboard, IClassicDesktopStyleApplicationLifetime appLifetime)
    {
#if DEBUG
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.UrlSearchEngine/Starter.UrlSearchEngine/bin/Debug/net10.0/", clipboard);
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.WebSearchEngine/bin/Debug/net10.0/", clipboard);
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.WorkspaceSearchEngine/bin/Debug/net10.0/", clipboard);
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.Calculator/bin/Debug/net10.0/", clipboard);
        searchEngineStore.LoadSearchEnginesFromDirectory("./src/Starter.EverythingSearchEngine/bin/Debug/net10.0/", clipboard);
        searchEngineStore.LoadSearchEnginesFromDirectory(
            OperatingSystem.IsWindows()
                ? "./src/Starter.ApplicationSearchEngine/bin/Debug/net10.0-windows10.0.19041.0/"
                : "./src/Starter.ApplicationSearchEngine/bin/Debug/net10.0/",
            clipboard
        );
#else
        foreach (var pluginDir in Directory.GetDirectories(Const.PluginsDirectory))
            searchEngineStore.LoadSearchEnginesFromDirectory(pluginDir, clipboard);
#endif

        // Exit search engine
        searchEngineStore.AddSearchEngine(
            new ExitSearchEngine(appLifetime)
        );

        Log.Debug("Plugins loaded");
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

    private void TrayIcon_OnClicked(object? sender, EventArgs e)
    {
        window?.Show();
        window?.Activate();
    }

    private void NativeMenuItem_OnClickQuit(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        desktop.TryShutdown();
    }
}
