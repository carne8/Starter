using R3;
using Starter.Features.Config;
using Starter.Features.PlatformInterop;

namespace Starter.Desktop.ViewModels;

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

    // TODO: Activator prefixes

    public SettingsViewModel(Configuration baseConfig, SearchEngineStore searchEngines)
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
    }

    public void OnOpened()
    {
        Task.Run(() =>
        {
            // Checks if launch at startup is enabled
            LaunchAtStartup = Platform.IsLaunchAtStartupEnabled();
            LaunchAtStartupLoading = false;
        });
    }

    partial void OnLaunchAtStartupChanged(bool value) => Task.Run(() => Platform.ToggleLaunchAtStartup(value));
    partial void OnSelectedBackgroundChanged(BackgroundKind value) => Config.OnNext(Config.Value.WithBackground(value.Value));
    partial void OnZoomedModeChanged(bool value) => Config.OnNext(Config.Value.WithZoomedMode(value));
}
// type SearchEnginePrefixViewModel(se: SearchEngine, prefix: string, onPrefixChanged) =
//     let mutable prefix = prefix
//
//     member this.Icon = se.Icon
//     member this.Name = se.Name
//     member this.Prefix
//         with get () = prefix
//         and set v = prefix <- v; v |> onPrefixChanged
//
//     // Activator prefixes
//     let onActivatorPrefixChanged activatorId newPrefix =
//         let newMap =
//             config.Value.ActivatorPrefixes |> Map.change activatorId (
//                 match newPrefix with
//                 | "" -> fun _ -> None
//                 | s -> fun _ -> Some s
//             )
//
//         config.OnNext <| { config.Value with ActivatorPrefixes = newMap }
//
//     let seActivatorsVms =
//         searchEngines |> Observable.map (Seq.map (fun kv ->
//             let searchEngine = kv.Value
//             let activators =
//                 searchEngine.Activators |> Observable.map (Seq.map (fun activator ->
//                     let prefix =
//                         config.Value.ActivatorPrefixes
//                         |> Map.tryFind activator.Id
//                         |> Option.defaultValue String.Empty
//                     struct (activator, prefix)
//                 ))
//
//             SearchEngineActivatorsViewModel(searchEngine, activators, onActivatorPrefixChanged)
//         ))
//
