using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Serilog;
using Starter.Desktop.Controls;
using Starter.Desktop.ViewModels;
using Starter.Features;
using Starter.Features.PlatformInterop;
using R3;

namespace Starter.Desktop.Views;

public partial class MainWindow : TranslucentWindow
{
    private MainWindowViewModel vm = null!;
    private readonly PlatformInterop platformInterop = PlatformInteropFactory.GetPlatformInterop();

    public MainWindow()
    {
        InitializeComponent();
        platformInterop.SetupHotkeyCallback(this);
        TextBox.AddHandler(KeyDownEvent, TextBox_OnKeyDown, RoutingStrategies.Tunnel);

        Activated += (_, _) => OnActivated();
#if !DEBUG
        Deactivated += (_, _) => Hide();
#endif
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is not MainWindowViewModel dataContext) return;
        vm = dataContext;

        SetupKeyboardShortcuts();

        // Subscribe to view model commands
        vm.ClearTextBox += (_, prefixLength) => Dispatcher.UIThread.Post(() =>
        {
            TextBox.Text = TextBox.Text?[prefixLength..];
            TextBox.CaretIndex -= prefixLength;
        });
        vm.HideWindow += (_, _) => Dispatcher.UIThread.Post(Hide);

        // Bind zoomed mode
        vm.Config
            .Select(config => config.ZoomedMode)
            .DistinctUntilChanged()
            .Subscribe(SetResourceDictionary);

        // Refresh keyboard shortcut when needed
        vm.Config
            .Select(config => config.KeyboardShortcut)
            .DistinctUntilChanged()
            .Subscribe(shortcut => platformInterop.RegisterHotkey(shortcut, this));

        SetResourceDictionary(vm.Config.Value.ZoomedMode);
        platformInterop.RegisterHotkey(vm.Config.Value.KeyboardShortcut, this);
    }

    private void OnActivated()
    {
        // Center window
        var screen = Screens.ScreenFromTopLevel(this) ?? Screens.Primary;
        if (screen is null)
        {
            Log.Error("No screen available. Can't center window");
            return;
        }

        Position =
            screen.WorkingArea.TopLeft
            + new PixelPoint(
                (int)Math.Round((screen.WorkingArea.Width - Width * screen.Scaling) / 2.0),
                (int)Math.Round(screen.WorkingArea.Height * (5.0 / 16.0))
            );

        // Reset focus
        TextBox.Focus();
        TextBox.SelectAll();

        // Reset selection
        ResultList.Selection.Select(0);

        // Scroll to top to preserve the top padding
        if (ResultList.ItemCount != 0)
            ResultList.Scroll?.Offset = new Vector(0, 0); // Scroll to home
    }

    private void SetResourceDictionary(bool zoomedMode)
    {
        var found = this.TryFindResource(
            zoomedMode ? "ZoomedMode" : "NormalMode",
            ActualThemeVariant,
            out var resourceDict
        );

        if (!found || resourceDict is not IResourceDictionary v) return;
        Resources = v;
    }

    private void SetupKeyboardShortcuts()
    {
        // Add escape key binding
        var onEscape = new RelayCommand(() =>
        {
            if (vm.Activator is not null) vm.ResetActivatorCommand.Execute(null);
            else Dispatcher.UIThread.Post(Hide);
        });

        KeyBindings.Add(new KeyBinding()
        {
            Command = onEscape,
            Gesture = new KeyGesture(Key.Escape)
        });
    }

    private void ResultList_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pos = e.GetPosition(this);
        var clickedControl =
            this.GetVisualsAt(pos)
                .FirstOrDefault(v => v.DataContext is SearchResultData);

        vm.SelectResultCommand.Execute(clickedControl?.DataContext);
    }

    private void TextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Remove activator if caret is at start
        if (e.Key == Key.Back && vm.Activator is not null && TextBox.CaretIndex == 0)
        {
            vm.ResetActivatorCommand.Execute(null);
            e.Handled = true;
            return;
        }

        // Set custom keyboard navigation
        // -> The goal is to be able to navigate in the listbox without losing the focus on the textbox
        int newSelectedIdx;

        if (e.Key == Key.Tab && vm.SearchResults.Count > 0)
        {
            newSelectedIdx = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
                ? Math.Max(ResultList.SelectedIndex - 1, 0)
                : Math.Min(ResultList.SelectedIndex + 1, vm.SearchResults.Count - 1);
        }
        else if (e.Key.ToNavigationDirection() == NavigationDirection.Up)
            newSelectedIdx = Math.Max(ResultList.SelectedIndex - 1, 0);
        else if (e.Key.ToNavigationDirection() == NavigationDirection.Down)
            newSelectedIdx = Math.Min(ResultList.SelectedIndex + 1, vm.SearchResults.Count - 1);
        else return;

        // TODO: Always keep bottom padding
        // Scroll to top or bottom to preserve the paddings
        if (ResultList.Scroll is not null)
        {
            if (newSelectedIdx == 0) ResultList.Scroll.Offset = new Vector(0, 0);
            else if (newSelectedIdx == ResultList.ItemCount - 1) ResultList.Scroll.Offset = new Vector(0, ResultList.Scroll.Extent.Height);
        }

        // Select next item
        ResultList.Selection.SelectedIndex = newSelectedIdx;
        e.Handled = true;
    }
}
