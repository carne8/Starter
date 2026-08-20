using Avalonia.Controls.Templates;
using Avalonia.Input.Platform;
using Serilog;

namespace Starter.SearchEngine;

#pragma warning disable CS9113 // Parameter unread

/// <summary>
/// An object that gives search engines and their settings pages to Starter.
/// This is tha entry point of a Starter extension.
/// </summary>
/// <param name="pluginDirectory">The plugin installation directory</param>
public abstract class SearchEngineFactory(string pluginDirectory)
{
    public class SearchEngineSettings(object dataContext, IDataTemplate dataTemplate)
    {
        public readonly object DataContext = dataContext;
        public readonly IDataTemplate DataTemplate = dataTemplate;
    }

    /// <remarks>
    /// Returned ids should contain only valid filename characters
    /// </remarks>
    public abstract string[] LoadSearchEngineIds();

    public abstract (ISearchEngine searchEngine, SearchEngineSettings? settings) LoadSearchEngine(
        string searchEngineId,
        string pluginConfigDirectory,
        ILogger logger,
        IClipboard clipboard
    );
}
