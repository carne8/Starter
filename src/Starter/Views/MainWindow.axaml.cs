using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.Globalization;
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

    public MainWindow()
    {
        // Dummy constructor to prevent XAML warnings
        throw new Exception("This constructor should never be called");
    }

    public MainWindow(IPlatformInterop platformInterop)
    {
        this.platformInterop = platformInterop;

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
            if (vm.ContextMenuActivated) vm.CloseContextMenuCommand.Execute(null);
            else if (vm.Activator is not null) vm.ResetActivatorCommand.Execute(null);
            else Dispatcher.UIThread.Post(Hide);
        });

        KeyBindings.Add(new KeyBinding
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
                .FirstOrDefault(v => v.DataContext is SearchResultData or ContextMenuResultData);

        if (clickedControl is null) return;

        if (clickedControl.DataContext is SearchResultData)
            vm.SelectResultCommand.Execute(clickedControl.DataContext);

        if (clickedControl.DataContext is ContextMenuResultData d && d.Result.IsSeparator)
            vm.SelectContextMenuResultCommand.Execute(clickedControl.DataContext);
    }

    private void TextBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (vm.ContextMenuActivated)
                    vm.SelectContextMenuResultCommand.Execute(ContextMenuResultList.SelectedItem);
                else
                    vm.SelectResultCommand.Execute(ResultList.SelectedItem);

                return;

            // If caret is at start and backspace
            case Key.Back when TextBox.CaretIndex == 0:
                if (vm.ContextMenuActivated) // Close context menu
                {
                    vm.CloseContextMenuCommand.Execute(null);
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
                vm.OpenContextMenuCommand.Execute(
                    vm.ContextMenuActivated
                        ? ContextMenuResultList.SelectedItem
                        : ResultList.SelectedItem
                );
                e.Handled = true;
                return;
        }

        // Set custom keyboard navigation
        // -> The goal is to be able to navigate in the listbox without losing the focus on the textbox
        int newSelectedIdx;
        var resultList =
            vm.ContextMenuActivated
                ? ContextMenuResultList
                : ResultList;

        if (e.Key.ToNavigationDirection() == NavigationDirection.Up)
            newSelectedIdx = Math.Max(resultList.SelectedIndex - 1, 0);
        else if (e.Key.ToNavigationDirection() == NavigationDirection.Down)
            newSelectedIdx = Math.Min(resultList.SelectedIndex + 1, resultList.ItemCount - 1);
        else return;

        // TODO: Always keep bottom padding
        // Scroll to top or bottom to preserve the paddings
        if (resultList.Scroll is not null)
        {
            if (newSelectedIdx == 0) resultList.Scroll.Offset = new Vector(0, 0);
            else if (newSelectedIdx == resultList.ItemCount - 1) resultList.Scroll.Offset = new Vector(0, resultList.Scroll.Extent.Height);
        }

        // Select next item
        resultList.SelectedIndex = newSelectedIdx;

        // Prevent focusing a separator
        if (vm.ContextMenuActivated
            && resultList.Selection.SelectedItem is ContextMenuResultData d
            && d.Result.IsSeparator)
            TextBox_OnKeyDown(sender, e);

        e.Handled = true;
    }
}
