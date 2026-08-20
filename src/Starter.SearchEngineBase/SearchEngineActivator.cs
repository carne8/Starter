using System;

namespace Starter.SearchEngine;

public interface ISearchEngineActivator
{
    string Id { get; }
    string SearchEngineId { get; }
    string Name { get; }
    StarterIconSource Icon { get; }
}

public interface ISearchEngineDynamicActivator : ISearchEngineActivator
{
    public event EventHandler? Changed;
}

/// <summary>
/// Activator used when a search engine doesn't declare
/// activators and the user used the single-search-engine mode
/// </summary>
public readonly struct DefaultSearchEngineActivator(ISearchEngine se) : ISearchEngineActivator
{
    public string Id { get; } = se.Id;
    public string SearchEngineId { get; } = se.Id;
    public string Name { get; } = se.Name;
    public StarterIconSource Icon { get; } = se.Icon;
}
