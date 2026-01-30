using Microsoft.FSharp.Collections;
using Starter.SearchEngine;

namespace Starter.Desktop.ViewModels;

public partial class ActivatorInputFieldViewModel : ObservableObject
{
    private readonly ISearchEngineActivator activator;
    private readonly Action<ISearchEngineActivator, string> activatorPrefixChanged;

    [ObservableProperty] private string name;
    [ObservableProperty] private StarterIconSource icon;
    [ObservableProperty] private string activationString;

    public ActivatorInputFieldViewModel(
        ISearchEngineActivator activator,
        FSharpMap<string,string> activatorPrefixes,
        Action<ISearchEngineActivator, string> activatorPrefixChanged
    )
    {
        this.activatorPrefixChanged = activatorPrefixChanged;

        this.activator = activator;
        this.activationString =
            activatorPrefixes.TryGetValue(activator.Id, out var activationString)
                ? activationString
                : string.Empty;

        name = activator.Name;
        icon = activator.Icon;
        if (activator is ISearchEngineDynamicActivator dynActivator) dynActivator.Changed += DynamicActivatorOnChanged;
    }

    private void DynamicActivatorOnChanged(object? sender, EventArgs e)
    {
        Name = activator.Name;
        Icon = activator.Icon;
    }

    partial void OnActivationStringChanged(string value) => activatorPrefixChanged.Invoke(activator, value);
}
