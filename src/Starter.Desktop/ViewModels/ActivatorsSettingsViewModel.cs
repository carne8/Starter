using FluentAvalonia.UI.Controls;
using Microsoft.FSharp.Collections;
using Starter.SearchEngine;

namespace Starter.Desktop.ViewModels;

public partial class ActivatorInputFieldViewModel : ObservableObject
{
    private readonly ISearchEngineActivator activator;
    private readonly Action<ISearchEngineActivator, string> activatorPrefixChanged;

    public string Name => activator.Name;
    public IconSource IconSource { get; }
    [ObservableProperty] private string activationString;

    public ActivatorInputFieldViewModel(
        ISearchEngineActivator activator,
        FSharpMap<string,string> activatorPrefixes,
        Action<ISearchEngineActivator, string> activatorPrefixChanged
    )
    {
        this.activator = activator;
        this.activationString =
            activatorPrefixes.TryGetValue(activator.Id, out var activationString)
                ? activationString
                : string.Empty;

        this.activatorPrefixChanged = activatorPrefixChanged;

        // TODO: Make it change with system
        IconSource = Views.SettingsDataTemplates.BuildIconSource(activator.Icon, true);
    }

    partial void OnActivationStringChanged(string value) => activatorPrefixChanged.Invoke(activator, value);
}
