using R3;
using Starter.Features.Config;
using Starter.Features.PlatformInterop;
using Starter.SearchEngine;

namespace Starter.ViewModels;

public record BackgroundKind(string Name, Background Value, bool Available);

public partial class SettingsViewModel : ObservableObject
{
    private static readonly PlatformInterop Platform = PlatformInteropFactory.GetPlatformInterop();
    public readonly BehaviorSubject<Configuration> Config;

    // Background launch at startup
    [ObservableProperty] private bool launchAtStartup;
    [ObservableProperty] private bool launchAtStartupLoading = true;

    // Background
    public static readonly BackgroundKind[] Backgrounds =
    [
        new("Acrylic", Background.Acrylic, !OperatingSystem.IsLinux()),
        new("Mica", Background.Mica, !OperatingSystem.IsLinux()),
        new("None", Background.None, true)
    ];
    public static string? BackgroundDescription =>
        OperatingSystem.IsLinux()
            ? "Acrylic and Mica background are not supported on Linux"
            : null;

    [ObservableProperty] private BackgroundKind selectedBackground;

    // Zoomed mode
    [ObservableProperty] private bool zoomedMode;

    // Keyboard shortcut
    public KeyboardShortcutInputViewModel KeyboardShortcutViewModel { get; }

    // Activator prefixes
    public ActivatorInputFieldViewModel[] ActivatorViewModels { get; }

    public SettingsViewModel(Configuration baseConfig, SearchEngineStore engines)
    {
        Config = new BehaviorSubject<Configuration>(baseConfig);
        selectedBackground = baseConfig.Background.Tag switch
        {
            Background.Tags.Acrylic => Backgrounds[0],
            Background.Tags.Mica => Backgrounds[1],
            /* Background.Tags.Mica */ _ => Backgrounds[2]
        };
        zoomedMode = baseConfig.ZoomedMode;

        KeyboardShortcutViewModel = new KeyboardShortcutInputViewModel(baseConfig.KeyboardShortcut);
        KeyboardShortcutViewModel.KeyboardShortcutChanged +=
            shortcut => Config.OnNext(Config.Value.WithKeyboardShortcut(shortcut));

        ActivatorViewModels = engines.SearchEngines.Values
            .SelectMany(engine => engine.Activators)
            .Select(activator => new ActivatorInputFieldViewModel(
                activator,
                baseConfig.ActivatorPrefixes,
                ActivatorPrefixChanged
            ))
            .ToArray();
    }

    private void ActivatorPrefixChanged(ISearchEngineActivator activator, string newPrefix)
    {
        var newMap = Config.Value.ActivatorPrefixes.Add(activator.Id, newPrefix);
        var newConfig = Config.Value.WithActivatorPrefixes(newMap);
        Config.OnNext(newConfig);
    }

    public void OnOpened()
    {
        Task.Run(() =>
        {
            // Checks if launch at startup is enabled
            try
            {
                LaunchAtStartup = Platform.IsLaunchAtStartupEnabled();
                LaunchAtStartupLoading = false;
            }
            catch (Exception)
            {
                LaunchAtStartup = false;
                LaunchAtStartupLoading = false;
            }
        });
    }

    partial void OnLaunchAtStartupChanged(bool value) => Task.Run(() => Platform.ToggleLaunchAtStartup(value));
    partial void OnSelectedBackgroundChanged(BackgroundKind value) => Config.OnNext(Config.Value.WithBackground(value.Value));
    partial void OnZoomedModeChanged(bool value) => Config.OnNext(Config.Value.WithZoomedMode(value));
}
