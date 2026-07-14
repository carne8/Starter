using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Globalization;
using Avalonia.Platform;
using R3;
using Serilog;
using Starter.Controls;
using Starter.Features;
using Starter.Features.PlatformInterop;
using Starter.ViewModels;

namespace Starter.Views;

public class FirstNonNullConverter : IMultiValueConverter
{
    public object? Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => values.OfType<object>().FirstOrDefault();
}

public class TimeSpanConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not TimeSpan t) return string.Empty;

        if ((int)t.TotalMilliseconds > 0)
            return $"{t.TotalMilliseconds.ToString("N2", culture)} ms";

        if ((int)t.TotalMicroseconds > 0)
            return $"{t.TotalMilliseconds.ToString("N2", culture)} μs";

        return $"{t.TotalNanoseconds.ToString("N2", culture)} ns";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public class IsNotZeroConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int i
            ? i > 0
            : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
}

public partial class MainWindow : TranslucentWindow
{
    private MainWindowViewModel vm = null!;
    private readonly IPlatformInterop platformInterop;
    private readonly IPlatformHandle platformHandle;

    private readonly Stack<(string? text, int caretPosition, object? selectedItem)> previousSelectionState = new();

    private ListBox CurrentResultList =>
        vm.ContextMenuActivated
            ? ContextMenuResultList
            : ResultList;

    public MainWindow()
    {
        // Dummy constructor to prevent XAML warnings
        platformHandle = null!;
        throw new Exception("This constructor should never be called");
    }

    public MainWindow(IPlatformInterop platformInterop)
    {
        this.platformInterop = platformInterop;

        var platformHandle = TryGetPlatformHandle();
        if (platformHandle is null)
        {
            Log.Error("Failed to get platform handle");
            throw new Exception("Failed to get platform handle");
        }
        this.platformHandle = platformHandle;

        InitializeComponent();
        platformInterop.SetupHotkeyCallback(this);
        TextBox.AddHandler(KeyDownEvent, TextBox_OnKeyDown, RoutingStrategies.Tunnel);

        Closing += (_, args) =>
        {
            if (args.IsProgrammatic) return;
            args.Cancel = true;
            Hide();
        };

        Activated += (_, _) => OnActivated();
#if !DEBUG
        Deactivated += (_, _) => Hide();
#endif
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != IsVisibleProperty) return;
        if (!change.GetNewValue<bool>()) return;
        vm.GreetingVm.RefreshGreetingCommand.Execute(null);
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
            TextBox.CaretIndex = 0; // Change caret index before text prevents a color bug
            TextBox.Text = TextBox.Text?[prefixLength..];
        });
        vm.HideWindow += (_, _) => Dispatcher.UIThread.Post(Hide);

        // Bind zoomed mode
        vm.Config
            .Select(config => config.ZoomedMode)
            .DistinctUntilChanged()
            .Subscribe(SetResourceDictionary);
        SetResourceDictionary(vm.Config.Value.ZoomedMode);

        // Refresh keyboard shortcut when needed
        if (!platformInterop.HotkeyRegistrable) return;
        vm.Config
            .Select(config => config.KeyboardShortcut)
            .DistinctUntilChanged()
            .Subscribe(shortcut => platformInterop.RegisterHotkey(shortcut, this));
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
                (int)Math.Round((screen.WorkingArea.Height - 470 * screen.Scaling) / 2.0)
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
            if (vm.ContextMenuActivated) CloseContextMenu();
            else if (vm.Activator is not null) vm.ResetActivatorCommand.Execute(null);
            else Dispatcher.UIThread.Post(Hide);
        });

        KeyBindings.Add(new KeyBinding
        {
            Command = onEscape,
            Gesture = new KeyGesture(Key.Escape)
        });
    }

    private void SelectContextMenuResult(object? result)
    {
        if (result is not ContextMenuResultData data) return;
        if (!data.TryGetEntry(out var entry)) return;

        previousSelectionState.Push((
            TextBox.Text,
            TextBox.CaretIndex,
            CurrentResultList.SelectedItem
        ));

        var openedNewContextMenu = vm.SelectContextMenuResult(entry, platformHandle);
        if (!openedNewContextMenu) previousSelectionState.Pop();
    }

    private void OpenContextMenu(object? result)
    {
        if (result is not SearchResultData data) return;

        previousSelectionState.Push((
            TextBox.Text,
            TextBox.CaretIndex,
            CurrentResultList.SelectedItem
        ));

        var openedNewContextMenu = vm.OpenContextMenu(data);
        if (!openedNewContextMenu) previousSelectionState.Pop();
    }

    private void CloseContextMenu()
    {
        vm.CloseContextMenuCommand.Execute(null);
        if (!previousSelectionState.TryPop(out var state)) return;

        TextBox.Text = state.text;
        TextBox.CaretIndex = 0; // Fixes caret visual position bug
        TextBox.CaretIndex = state.caretPosition;
        CurrentResultList.SelectedItem = state.selectedItem;
    }

    private void ResultList_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pos = e.GetPosition(this);
        var clickedControl =
            this.GetVisualsAt(pos)
                .FirstOrDefault(v => v.DataContext is SearchResultData or ContextMenuResultData);

        if (clickedControl is null) return;

        if (clickedControl.DataContext is SearchResultData)
            vm.SelectResultCommand.Execute(clickedControl.DataContext);

        if (clickedControl.DataContext is ContextMenuResultData { IsSeparator: false })
            SelectContextMenuResult(clickedControl.DataContext);
    }

    private void TextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (vm.ContextMenuActivated)
                    SelectContextMenuResult(ContextMenuResultList.SelectedItem);
                else
                    vm.SelectResultCommand.Execute(ResultList.SelectedItem);

                return;

            // If caret is at start and backspace
            case Key.Back when TextBox.CaretIndex == 0:
                if (vm.ContextMenuActivated) // Close context menu
                {
                    CloseContextMenu();
                    e.Handled = true;
                    return;
                }

                if (vm.Activator is not null) // Disable activator
                {
                    vm.ResetActivatorCommand.Execute(null);
                    e.Handled = true;
                    return;
                }

                break;

            // Tab opens the context menu
            case Key.Tab:
                if (vm.ContextMenuActivated)
                    SelectContextMenuResult(ContextMenuResultList.SelectedItem);
                else
                    OpenContextMenu(ResultList.SelectedItem);

                e.Handled = true;
                return;
        }

        // Set custom keyboard navigation
        // -> The goal is to be able to navigate in the listbox without losing the focus on the textbox
        int newSelectedIdx;

        if (e.Key.ToNavigationDirection() == NavigationDirection.Up)
            newSelectedIdx = Math.Max(CurrentResultList.SelectedIndex - 1, 0);
        else if (e.Key.ToNavigationDirection() == NavigationDirection.Down)
            newSelectedIdx = Math.Min(CurrentResultList.SelectedIndex + 1, CurrentResultList.ItemCount - 1);
        else return;

        // TODO: Always keep bottom padding
        // Scroll to top or bottom to preserve the paddings
        if (CurrentResultList.Scroll is not null)
        {
            if (newSelectedIdx == 0) CurrentResultList.Scroll.Offset = new Vector(0, 0);
            else if (newSelectedIdx == CurrentResultList.ItemCount - 1) CurrentResultList.Scroll.Offset = new Vector(0, CurrentResultList.Scroll.Extent.Height);
        }

        // Select next item
        CurrentResultList.SelectedIndex = newSelectedIdx;

        // Prevent focusing a separator
        if (vm.ContextMenuActivated
            && ContextMenuResultList.SelectedIndex != CurrentResultList.ItemCount - 1
            && ContextMenuResultList.Selection.SelectedItem is ContextMenuResultData { IsSeparator: true })
            TextBox_OnKeyDown(sender, e);

        e.Handled = true;
    }
}
