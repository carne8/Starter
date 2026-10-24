using System.ComponentModel;
using Microsoft.FSharp.Collections;
using Starter.SearchEngine;

namespace Starter.ViewModels;

public partial class ActivatorInputFieldViewModel : ObservableObject
{
    private readonly SearchEngineActivator activator;
    private readonly Action<SearchEngineActivator, string> activatorPrefixChanged;

    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial StarterIconSource Icon { get; set; }
    [ObservableProperty] public partial string ActivationString { get; set; }

    public ActivatorInputFieldViewModel(
        SearchEngineActivator activator,
        FSharpMap<string,string> activatorPrefixes,
        Action<SearchEngineActivator, string> activatorPrefixChanged
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
        if (activator is INotifyPropertyChanged dynActivator) dynActivator.PropertyChanged += DynamicActivatorOnChanged;
    }

    private void DynamicActivatorOnChanged(object? sender, PropertyChangedEventArgs e)
    {
        Name = activator.Name;
        Icon = activator.Icon;
    }

    partial void OnActivationStringChanged(string value) => activatorPrefixChanged.Invoke(activator, value);
}
