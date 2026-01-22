using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Starter.Desktop.Views;

public partial class Settings : UserControl
{
    public Settings() => InitializeComponent();

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is not ViewModels.SettingsViewModel vm) return;
        vm.OnOpened();
    }
}
