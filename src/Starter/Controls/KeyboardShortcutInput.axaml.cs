using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Starter.ViewModels;

namespace Starter.Controls;

public partial class KeyboardShortcutInput : UserControl
{
    public static readonly StyledProperty<Control?> ControlToFocusProperty = AvaloniaProperty.Register<KeyboardShortcutInput, Control?>(nameof(ControlToFocus));

    public Control? ControlToFocus
    {
        get => GetValue(ControlToFocusProperty);
        set => SetValue(ControlToFocusProperty, value);
    }

    public KeyboardShortcutInput() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not KeyboardShortcutInputViewModel vm) return;
        vm.StoppedListening += OnStoppedListening;
    }

    private void OnStoppedListening() => ControlToFocus?.Focus();

    private void TextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not KeyboardShortcutInputViewModel vm) return;
        vm.OnKeyDown(e.Key);
        e.Handled = true;
    }

    private void TextBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (DataContext is not KeyboardShortcutInputViewModel vm) return;
        vm.StartListening();
    }

    private void TextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not KeyboardShortcutInputViewModel vm) return;
        vm.Cancel();
    }
}
