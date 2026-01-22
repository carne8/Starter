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

    public SettingsViewModel(Configuration baseConfig, SearchEngineStore searchEngines)
    {
        Config = new BehaviorSubject<Configuration>(baseConfig);
        selectedBackground = baseConfig.Background.Tag switch
        {
            Background.Tags.Acrylic => Backgrounds[0],
            Background.Tags.Mica => Backgrounds[1],
            _ => Backgrounds[2]
        };
        zoomedMode = baseConfig.ZoomedMode;
    }


    // TODO: Activator prefixes
    // TODO: Keyboard shortcut

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
// type BackgroundComboBoxItemViewModel =
//    { Name: string
//      Value: Background
//      IsEnabled: bool }
//
// [<AutoOpen>]
// module private Helpers =
//     type Background with
//         static member toString =
//             function
//             | Background.Acrylic -> "Acrylic"
//             | Background.Mica -> "Mica"
//             | Background.None -> "None"
//
//         static member fromString =
//             function
//             | Background.Acrylic -> "Acrylic"
//             | Background.Mica -> "Mica"
//             | Background.None -> "None"



//     // Keyboard shortcut
//     let onKeyboardShortcutChanged newShortcut =
//         { config.Value with KeyboardShortcut = newShortcut }
//         |> config.OnNext
//     let keyboardShortcutViewModel = KeyboardShortcutInputViewModel(baseConfig.KeyboardShortcut, onKeyboardShortcutChanged)
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
//     interface IDisposable with
//         override _.Dispose() = config.Dispose()
//
//     member _.Configuration =
//         config.Skip(1).Debounce(TimeSpan.FromMilliseconds 100L)
//
//     // --- Settings bindings ---
//
//     // Background
//     member this.Backgrounds = backgrounds
//     member this.SelectedBackgroundIdx
//         with get () = backgrounds |> Array.findIndex (_.Value >> (=) config.Value.Background)
//         and set v =
//             let { Value = value } = backgrounds |> Array.item v
//             config.OnNext <| { config.Value with Background = value }
//     member this.BackgroundDescription : string | null =
//         if OperatingSystem.IsLinux() then
//             "Acrylic and Mica background are not supported on Linux"
//         else null
//
//     // Search engine prefixes
//     member this.SearchEngineActivators = seActivatorsVms
//
//     // Zoom mode activated
//     member this.ZoomedModeActivated
//         with get () = config.Value.ZoomedMode
//         and set v = config.OnNext <| { config.Value with ZoomedMode = v }
