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

public interface ISearchEngine
{
    public string Id { get; }
    public string Name { get; }
    public StarterIconSource Icon { get; }
    public ISearchEngineActivator[] Activators { get; }
    public event EventHandler? Changed;
    public void SearchResultSelected(ISearchResult selectedSearchResult);
}

/// <summary>
/// A search engine that generates results once.
/// Fuzzy finding is applicable on its results.
/// Applicable for an application search engine.
/// </summary>
public interface IStaticSearchEngine : ISearchEngine
{
    public ValueTask<IEnumerable<ISearchResult>> LoadResults();
    public event EventHandler<IEnumerable<ISearchResult>>? ResultsChanged;
}

/// <summary>
/// A search engine that generates results for each query.
/// Fuzzy finding is not applicable for its results.
/// Applicable for a web search engine.
/// </summary>
public interface IDynamicSearchEngine : ISearchEngine
{
    /// <summary>
    /// Indicate if the instant results from this search engine should be shown on
    /// top of others results (like for the calculator search engine) or if they
    /// should be shown in the last results (like for the URL search engine)
    /// </summary>
    public ResultPriority ResultsPriority { get; }

    /// <summary>
    /// Indicate if the results from the observable should be buffered or not.
    /// Add some lag when true.
    /// </summary>
    public bool BufferResults { get; }

    public (IEnumerable<ISearchResult>, Observable<IEnumerable<ISearchResult>>) Search(string query, CancellationToken cancellationToken, ISearchEngineActivator? activator);
}
