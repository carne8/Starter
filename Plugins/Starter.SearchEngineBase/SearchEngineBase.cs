using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS9113 // Parameter unread

namespace Starter.SearchEngine;

public interface ISearchResult
{
    string Id { get; }
    string Name { get; }
    Avalonia.Media.Imaging.Bitmap? LoadIcon();
}

public interface ISearchEngine
{
    public string Id { get; }
    public string DisplayName { get; }
    public void SearchResultSelected(ISearchResult selectedSearchResult);
}

/// <summary>
/// A search engine that generates results once.
/// Fuzzy finding is applicable on its results.
/// Applicable for an application search engine.
/// </summary>
public abstract class StaticSearchEngine(string pluginPath) : ISearchEngine
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract Task<ISearchResult[]> LoadResults();
    public abstract void SearchResultSelected(ISearchResult selectedSearchResult);
}

/// <summary>
/// A search engine that generates results for each query.
/// Fuzzy finding is not applicable for its results.
/// Applicable for a web search engine.
/// </summary>
public abstract class DynamicSearchEngine(string pluginPath) : ISearchEngine
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract IObservable<ISearchResult[]> Search(string query, CancellationToken cancellationToken);
    public abstract void SearchResultSelected(ISearchResult selectedSearchResult);
}

public static class Constants
{
    public static readonly string[] SharedAssemblies = ["Avalonia.Base"];
}
