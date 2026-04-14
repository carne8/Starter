using Avalonia.Data;
using Avalonia.Interactivity;
using Starter.Controls;

namespace Starter.Views;

public partial class SettingsWindow : TranslucentWindow
{
    private BindingExpressionBase? binding;

    public SettingsWindow() => InitializeComponent();

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Bind selected page control
        binding = ContentControl.Bind(ContentProperty, new Binding("SelectedPage.Control"));
        // Bind the content every time the data context changes to
        // prevent "The control ... already has a visual parent ...
        // while trying to add it as a child of ..." error
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        binding?.Dispose();
    }
}
