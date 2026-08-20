using Avalonia.Controls.Templates;

namespace Starter.SearchEngine;

public interface ISearchResult
{
    string? Id { get; }
    string Name { get; }
    string? Description { get; }
    /// <summary>
    /// Additional strings that are compared to the user query
    /// </summary>
    string[]? Keywords { get; }
    StarterIconSource Icon { get; }
    /// <summary>
    /// Show this result in the default mode of Starter, without any activator being in use.
    /// </summary>
    bool ShowIfNoActivator { get; }
    ISearchEngineActivator[] ActivatorFilter { get; }
    IContextMenuLoader? ContextMenuLoader { get; }
}

public interface IControlSearchResult : ISearchResult
{
    bool ShowIcon { get; }
    object ControlDataContext { get; }
    IDataTemplate ControlDataTemplate { get; }
}
