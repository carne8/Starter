namespace Starter.SearchEngine;

public abstract class SearchEngineActivator
{
    public abstract string Id { get; }
    public abstract string SearchEngineId { get; }
    public abstract string Name { get; }
    public virtual StarterIconSource Icon { get; } = StarterIconSource.Empty;
}

/// <summary>
/// Activator used when a search engine doesn't declare
/// activators and the user used the single-search-engine mode
/// </summary>
public class DefaultSearchEngineActivator(SearchEngine se) : SearchEngineActivator
{
    public override string Id => se.Id;
    public override string SearchEngineId => se.Id;
    public override string Name => se.Name;
    public override StarterIconSource Icon => se.Icon;
}
