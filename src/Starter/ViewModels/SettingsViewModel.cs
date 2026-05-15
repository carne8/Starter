using Avalonia.Platform.Storage;
using R3;
using Starter.Features.Config;
using Starter.Features.PlatformInterop;
using Starter.SearchEngine;

namespace Starter.ViewModels;

public record BackgroundKind(string Name, Background Value, bool Available);
public record AntialiasingKind(string Name, Antialiasing Value);

public partial class SettingsViewModel : ObservableObject
{
    private readonly ILauncher launcher;
    private static readonly PlatformInterop Platform = PlatformInteropFactory.GetPlatformInterop();
    public readonly BehaviorSubject<Configuration> Config;

    // Background launch at startup
    [ObservableProperty] public partial bool LaunchAtStartup { get; set; }
    [ObservableProperty] public partial bool LaunchAtStartupLoading { get; set; } = true;

    // Background
    public static readonly BackgroundKind[] Backgrounds =
    [
        new("Acrylic", Background.Acrylic, !OperatingSystem.IsLinux()),
        new("Mica", Background.Mica, !OperatingSystem.IsLinux()),
        new("None", Background.None, true)
    ];
    [ObservableProperty] public partial BackgroundKind SelectedBackground { get; set; }

    // Zoomed mode
    [ObservableProperty] public partial bool ZoomedMode { get; set; }

    // Antialiasing
    public static readonly AntialiasingKind[] Antialiasings =
    [
        new("Alias", Antialiasing.Alias),
        new("Grayscale", Antialiasing.Grayscale),
        new("Subpixel", Antialiasing.Subpixel),
        new("Platform default", Antialiasing.PlatformDefault)
    ];
    [ObservableProperty] public partial AntialiasingKind SelectedAntialiasing { get; set; }

    // Keyboard shortcut
    public KeyboardShortcutInputViewModel KeyboardShortcutViewModel { get; }

    // Activator prefixes
    public ActivatorInputFieldViewModel[] ActivatorViewModels { get; }

    public SettingsViewModel(ILauncher launcher, Configuration baseConfig, SearchEngineStore engines)
    {
        this.launcher = launcher;
        Config = new BehaviorSubject<Configuration>(baseConfig);
        SelectedBackground = baseConfig.Background.Tag switch
        {
            Background.Tags.Acrylic => Backgrounds[0],
            Background.Tags.Mica => Backgrounds[1],
            /* Background.Tags.Mica */ _ => Backgrounds[2]
        };
        SelectedAntialiasing = baseConfig.Antialiasing.Tag switch
        {
            Antialiasing.Tags.Alias => Antialiasings[0],
            Antialiasing.Tags.Grayscale => Antialiasings[1],
            Antialiasing.Tags.Subpixel => Antialiasings[2],
            /* Antialiasing.Tags.PlatformDefault */ _ => Antialiasings[3]
        };
        ZoomedMode = baseConfig.ZoomedMode;

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
        var newMap =
            string.IsNullOrEmpty(newPrefix)
                ? Config.Value.ActivatorPrefixes.Remove(activator.Id)
                : Config.Value.ActivatorPrefixes.Add(activator.Id, newPrefix);
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
    partial void OnSelectedAntialiasingChanged(AntialiasingKind value) => Config.OnNext(Config.Value.WithAntialiasing(value.Value));

    [RelayCommand]
    public void OpenConfigDirectory()
    {
        var dir = new DirectoryInfo(Features.Constants.ConfigDirectory);
        launcher.LaunchDirectoryInfoAsync(dir);
    }

    [RelayCommand]
    public void OpenSearchEnginesDirectory()
    {
        var dir = new DirectoryInfo(Features.Constants.PluginsDirectory);
        launcher.LaunchDirectoryInfoAsync(dir);
    }

    [RelayCommand]
    public void OpenLogsDirectory()
    {
        var dir = new DirectoryInfo(Features.Constants.LogDirectory);
        launcher.LaunchDirectoryInfoAsync(dir);
    }
}
