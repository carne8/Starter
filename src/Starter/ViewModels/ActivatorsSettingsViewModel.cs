using Microsoft.FSharp.Collections;
using Starter.SearchEngine;

namespace Starter.ViewModels;

public partial class ActivatorInputFieldViewModel : ObservableObject
{
    private readonly ISearchEngineActivator activator;
    private readonly Action<ISearchEngineActivator, string> activatorPrefixChanged;

    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial StarterIconSource Icon { get; set; }
    [ObservableProperty] public partial string ActivationString { get; set; }

    public ActivatorInputFieldViewModel(
        ISearchEngineActivator activator,
        FSharpMap<string,string> activatorPrefixes,
        Action<ISearchEngineActivator, string> activatorPrefixChanged
    )
    {
        this.activatorPrefixChanged = activatorPrefixChanged;

        this.activator = activator;
        ActivationString =
            activatorPrefixes.TryGetValue(activator.Id, out var activationString)
                ? activationString
                : string.Empty;
        Name = activator.Name;
        Icon = activator.Icon;
        if (activator is ISearchEngineDynamicActivator dynActivator) dynActivator.Changed += DynamicActivatorOnChanged;
    }

    private void DynamicActivatorOnChanged(object? sender, EventArgs e)
    {
        Name = activator.Name;
        Icon = activator.Icon;
    }

    partial void OnActivationStringChanged(string value) => activatorPrefixChanged.Invoke(activator, value);
}
