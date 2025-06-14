using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using FluentIcons.Common;
using R3;
using Serilog.Core;

#pragma warning disable CS9113 // Parameter unread

namespace Starter.SearchEngine;

public class StarterIconSource()
{
    public static StarterIconSource Empty = new();
    public StarterIconSource(IImage image) : this() => Image = image;
    public StarterIconSource(Icon icon) : this() => Icon = icon;

    public readonly IImage? Image;
    public readonly Icon? Icon;
}

public interface ISearchResult
{
    string? Id { get; }
    string Name { get; }
    string Description { get; }
    StarterIconSource Icon { get; }
}

public abstract class SearchEngine(string pluginPath, Logger logger)
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string ShortName { get; }
    public abstract StarterIconSource Icon { get; }
    public abstract void SearchResultSelected(ISearchResult selectedSearchResult);
    public abstract Control? LoadSettingsControl();
}

/// <summary>
/// A search engine that generates results once.
/// Fuzzy finding is applicable on its results.
/// Applicable for an application search engine.
/// </summary>
public abstract class StaticSearchEngine(string pluginPath, Logger logger) : SearchEngine(pluginPath, logger)
{
    public abstract Task<ISearchResult[]> LoadResults();
}

/// <summary>
/// A search engine that generates results for each query.
/// Fuzzy finding is not applicable for its results.
/// Applicable for a web search engine.
/// </summary>
public abstract class DynamicSearchEngine(string pluginPath, Logger logger) : SearchEngine(pluginPath, logger)
{
    /// <summary>
    /// Indicate if the instant results from this search engine should be shown on
    /// top of others results (like for the calculator search engine) or if they
    /// should be shown in the last results (like for the URL search engine)
    /// </summary>
    public abstract bool ImportantResults { get; }
    public abstract (ISearchResult[], Observable<ISearchResult[]>) Search(string query, CancellationToken cancellationToken, bool singleSearchEngineModeActivated);
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
        "Svg.Skia"
    ];
}
