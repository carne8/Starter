using Serilog;
using Starter.Features.Config;
using Starter.Features.PlatformInterop;
using Key = Avalonia.Input.Key;

namespace Starter.ViewModels;

public partial class KeyboardShortcutInputViewModel : ObservableObject
{
    private static readonly IPlatformInterop Platform = PlatformInterop.GetPlatformInterop();

    private bool listenKeys;
    public event Action? StoppedListening;
    public event Action<KeyboardShortcut>? KeyboardShortcutChanged;

    private KeyboardShortcut keyboardShortcut;
    private readonly HashSet<Key> pressedModifiers = [];
    private Key pressedKey = Key.None;
    [ObservableProperty] private string text = "";

    public static bool Enabled => Platform.HotkeyRegistrable;
    public static bool NotEnabled => !Platform.HotkeyRegistrable;

    public KeyboardShortcutInputViewModel(KeyboardShortcut initialKeyboardShortcut)
    {
        keyboardShortcut = initialKeyboardShortcut;
        ResetText();
    }

    private void ResetText()
    {
        var text = string.Join(" + ", keyboardShortcut.Modifiers.Select(key => key.ToString()));
        Text = keyboardShortcut.Key == Key.None ? text : $"{text} + {keyboardShortcut.Key}";
    }

    private void RefreshText()
    {
        var text = string.Join(" + ", pressedModifiers.Select(key => key.ToString()));
        Text = pressedKey == Key.None ? text : $"{text} + {pressedKey}";
    }

    public void StartListening() => listenKeys = true;
    private void StopListening()
    {
        listenKeys = false;
        StoppedListening?.Invoke();
    }

    public void Cancel()
    {
        pressedKey = Key.None;
        pressedModifiers.Clear();
        ResetText();
        StopListening();
    }

    public void Validate()
    {
        if (pressedModifiers.Count == 0 || pressedKey == Key.None)
        {
            Cancel();
            return;
        }

        keyboardShortcut = new KeyboardShortcut(pressedModifiers.ToArray(), pressedKey);
        Log.Information("Keyboard shortcut changed: %A{KeyboardShortcut}", keyboardShortcut);
        RefreshText();
        KeyboardShortcutChanged?.Invoke(keyboardShortcut);

        StopListening();
    }

    public void OnKeyDown(Key key)
    {
        if (!listenKeys) return;
        switch (key)
        {
            case Key.Escape: Cancel(); break;
            case Key.Enter: Validate(); break;

            // Modifiers
            case Key.LWin:
            case Key.RWin:
            case Key.LeftAlt:
            case Key.RightAlt:
            case Key.LeftCtrl:
            case Key.RightCtrl:
            case Key.LeftShift:
            case Key.RightShift:
                pressedModifiers.Add(key);
                RefreshText();
                break;

            // Other keys
            default:
                pressedKey = key;
                RefreshText();
                break;
        }
    }
}
