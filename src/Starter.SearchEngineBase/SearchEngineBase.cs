using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using R3;

#pragma warning disable CS9113 // Parameter unread

namespace Starter.SearchEngine;

public class StarterIconSource()
{
    public static StarterIconSource Empty = new();
    public StarterIconSource(IImage image) : this() => SourceImage = image;
    public StarterIconSource(Symbol symbol) : this() => Symbol = symbol;

    public IImage? SourceImage;
    public Symbol? Symbol;
}

public interface ISearchResult
{
    string Id { get; }
    string Name { get; }
    string Description { get; }
    StarterIconSource Icon { get; }
}

public interface ISearchEngine
{
    string Id { get; }
    string Name { get; }
    string ShortName { get; }
    StarterIconSource Icon { get; }
    void SearchResultSelected(ISearchResult selectedSearchResult);
}

/// <summary>
/// A search engine that generates results once.
/// Fuzzy finding is applicable on its results.
/// Applicable for an application search engine.
/// </summary>
public abstract class StaticSearchEngine(string pluginPath) : ISearchEngine
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string ShortName { get; }
    public abstract StarterIconSource Icon { get; }
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
    public abstract string Name { get; }
    public abstract string ShortName { get; }
    public abstract StarterIconSource Icon { get; }

    /// <summary>
    /// Indicate if the result from this search engine should be shown in
    /// the first results (like for the calculator search engine) or if they
    /// should be shown in the last results (like for the URL search engine)
    /// </summary>
    public abstract bool ImportantResults { get; }
    public abstract (ISearchResult[], Observable<ISearchResult[]>) Search(string query, CancellationToken cancellationToken);
    public abstract void SearchResultSelected(ISearchResult selectedSearchResult);
}

public static class Constants
{
    public static readonly string[] SharedAssemblies = [
        "Avalonia.Base",
        "FluentAvalonia"
    ];
}
