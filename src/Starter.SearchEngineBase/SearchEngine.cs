using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using R3;

namespace Starter.SearchEngine;

public enum ResultPriority
{
    /// <summary>
    /// Suitable for calculator engine results
    /// </summary>
    Unique,
    /// <summary>
    /// Automatically set by Starter for static search engines
    /// </summary>
    Static,
    /// <summary>
    /// Suitable for a search engine that produces results not directly
    /// linked to the query. For instance web search engine.
    /// </summary>
    Fallback,
    /// <summary>
    /// Suitable for a search engine that emits a lot of results
    /// </summary>
    Search
}

public abstract class SearchEngine
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public virtual StarterIconSource Icon { get; } = StarterIconSource.Empty;
    public virtual SearchEngineActivator[]? Activators { get; }
    public abstract void SearchResultSelected(SearchResult selectedSearchResult);
}

/// <summary>
/// A search engine that generates results once.
/// Fuzzy finding is applicable on its results.
/// Applicable for an application search engine.
/// </summary>
public abstract class StaticSearchEngine : SearchEngine
{
    public abstract ValueTask<IEnumerable<SearchResult>> LoadResults();
    public event EventHandler<IEnumerable<SearchResult>>? ResultsChanged;
    protected void OnResultsChanged(IEnumerable<SearchResult> results) => ResultsChanged?.Invoke(this, results);
}

/// <summary>
/// A search engine that generates results for each query.
/// Fuzzy finding is not applicable for its results.
/// Applicable for a web search engine.
/// </summary>
public abstract class DynamicSearchEngine : SearchEngine
{
    /// <summary>
    /// Indicate if the instant results from this search engine should be shown on
    /// top of others results (like for the calculator search engine) or if they
    /// should be shown in the last results (like for the URL search engine)
    /// </summary>
    public abstract ResultPriority ResultsPriority { get; }

    /// <summary>
    /// Indicate if the results from the observable should be buffered or not.
    /// Add some lag when true.
    /// </summary>
    public virtual bool BufferResults { get; }

    public abstract (IEnumerable<SearchResult>, Observable<IEnumerable<SearchResult>>) Search(string query, CancellationToken cancellationToken, SearchEngineActivator? activator);
}
