using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using R3;
using Serilog.Core;

#pragma warning disable CS9113 // Parameter unread

namespace Starter.SearchEngine;

public class StarterIconSource()
{
    public static StarterIconSource Empty = new();

    public StarterIconSource(IImage lightImage, IImage darkImage) : this()
    {
        LightImage = lightImage;
        DarkImage = darkImage;
    }
    public readonly IImage? LightImage;
    public readonly IImage? DarkImage;
    public IImage? GetImage(bool lightMode) => lightMode ? LightImage : DarkImage;

    public StarterIconSource(Geometry geometry) : this() => Geometry = geometry;
    public readonly Geometry? Geometry;
}

public interface ISearchEngineActivator
{
    string Id { get; }
    string SearchEngineId { get; }
    string Name { get; }
    string ShortName { get; }
    StarterIconSource Icon { get; }
}

/// <summary>
/// Activator used when a search engine doesn't declare
/// activators and the user used the single-search-engine mode
/// </summary>
public readonly struct DefaultSearchEngineActivator(SearchEngine se) : ISearchEngineActivator
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
    string Description { get; }
    /// <summary>
    /// Additional strings that are compared to the user query
    /// </summary>
    string[] Keywords { get; }
    StarterIconSource Icon { get; }
    /// <summary>
    /// Show this result in the default mode of Starter, without any activator being in use.
    /// </summary>
    bool ShowIfNoActivator { get; }
    ISearchEngineActivator[] ActivatorFilter { get; }
}

public abstract class SearchEngine(string pluginPath, string configDir, Logger logger)
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string ShortName { get; }
    public abstract StarterIconSource Icon { get; }

    public readonly BehaviorSubject<IEnumerable<ISearchEngineActivator>> Activators = new([]);
    public void LoadActivators() => Activators.OnNext([new DefaultSearchEngineActivator(this)]);

    public abstract void SearchResultSelected(ISearchResult selectedSearchResult);
    public abstract Control? LoadSettingsControl();
}

/// <summary>
/// A search engine that generates results once.
/// Fuzzy finding is applicable on its results.
/// Applicable for an application search engine.
/// </summary>
public abstract class StaticSearchEngine(string pluginPath, string configDir, Logger logger) : SearchEngine(pluginPath, configDir, logger)
{
    public abstract Task<(IEnumerable<ISearchResult>, Observable<IEnumerable<ISearchResult>>)> LoadResults();
}

/// <summary>
/// A search engine that generates results for each query.
/// Fuzzy finding is not applicable for its results.
/// Applicable for a web search engine.
/// </summary>
public abstract class DynamicSearchEngine(string pluginPath, string configDir, Logger logger) : SearchEngine(pluginPath, configDir, logger)
{
    /// <summary>
    /// Indicate if the instant results from this search engine should be shown on
    /// top of others results (like for the calculator search engine) or if they
    /// should be shown in the last results (like for the URL search engine)
    /// </summary>
    public abstract bool ImportantResults { get; }
    public abstract (IEnumerable<ISearchResult>, Observable<IEnumerable<ISearchResult>>) Search(string query, CancellationToken cancellationToken, ISearchEngineActivator? activator);
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
