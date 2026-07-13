using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input.Platform;
using Avalonia.Media;
using R3;
using Serilog;

#pragma warning disable CS9113 // Parameter unread

namespace Starter.SearchEngine;

public class StarterIconSource()
{
    public static readonly StarterIconSource Empty = new();

    public StarterIconSource(IImage lightImage, IImage darkImage) : this()
    {
        LightImage = lightImage;
        DarkImage = darkImage;
    }
    public StarterIconSource(Geometry geometry) : this() => Geometry = geometry;

    public readonly Geometry? Geometry;
    public readonly IImage? LightImage;
    public readonly IImage? DarkImage;

    public IImage? GetImage(bool lightMode) => lightMode ? LightImage : DarkImage;
}

public interface ISearchEngineActivator
{
    string Id { get; }
    string SearchEngineId { get; }
    string Name { get; }
    string ShortName { get; }
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
    public string ShortName { get; } = se.ShortName;
    public StarterIconSource Icon { get; } = se.Icon;
}

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

    IContextMenuResult[]? GetContextMenu();
}

public interface IContextMenuResult
{
    string? Id { get; }
    string Name { get; }
    string? Description { get; }
    /// <summary>
    /// Additional strings that are compared to the user query
    /// </summary>
    string[]? Keywords { get; }
    StarterIconSource? Icon { get; }
    void Invoke();
}

public interface IControlSearchResult : ISearchResult
{
    bool ShowIcon { get; }
    object ControlDataContext { get; }
    IDataTemplate ControlDataTemplate { get; }
}

public interface ISearchEngine
{
    public string Id { get; }
    public string Name { get; }
    public string ShortName { get; }
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


public abstract class SearchEngineFactory(string pluginDirectory)
{
    /// <remarks>
    /// Returned ids should contain only valid filename characters
    /// </remarks>
    public abstract string[] LoadSearchEngineIds();

    public class SearchEngineSettings(object dataContext, IDataTemplate dataTemplate)
    {
        public readonly object DataContext = dataContext;
        public readonly IDataTemplate DataTemplate = dataTemplate;
    }

    public abstract (
        ISearchEngine searchEngine,
        SearchEngineSettings? settings
    ) LoadSearchEngine(
        string searchEngineId,
        string pluginConfigDirectory,
        ILogger logger,
        IClipboard clipboard
    );
}

public static class Constants
{
    public static readonly string[] SharedAssemblies = [
        "Avalonia.Base",
        "Avalonia.Controls",
        "Avalonia.DesignerSupport",
        "Avalonia.Dialogs",
        "Avalonia",
        "Avalonia.Markup",
        "Avalonia.Markup.Xaml",
        "Avalonia.Metal",
        "Avalonia.MicroCom",
        "Avalonia.OpenGL",
        "Avalonia.Remote.Protocol",
        "Avalonia.Skia",
        "Avalonia.Vulkan",
        "Svg.Controls.Skia.Avalonia",
        "Svg.Custom",
        "Svg.Model",
        "Svg.Skia",
        "FluentAvalonia"
    ];
}
