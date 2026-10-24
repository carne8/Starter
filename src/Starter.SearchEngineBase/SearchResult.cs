using Avalonia.Controls.Templates;

namespace Starter.SearchEngine;

public abstract class SearchResult()
{
    public virtual string? Id { get; }
    public abstract string Name { get; }
    public virtual string? Description { get; }
    /// <summary>
    /// Additional strings that are compared to the user query
    /// </summary>
    public virtual string[]? Keywords { get; }
    public virtual StarterIconSource Icon { get; } = StarterIconSource.Empty;
    /// <summary>
    /// Show this result in the default mode of Starter, without any activator being in use.
    /// </summary>
    public virtual bool ShowIfNoActivator { get; } = true;
    public virtual SearchEngineActivator[]? ActivatorFilter { get; }
    public virtual IContextMenuLoader? ContextMenuLoader { get; }
}

public abstract class ControlSearchResult : SearchResult
{
    public virtual bool ShowIcon { get; }
    public abstract object ControlDataContext { get; }
    public abstract IDataTemplate ControlDataTemplate { get; }
}
