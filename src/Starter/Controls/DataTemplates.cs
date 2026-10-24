using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Starter.Features;
using Starter.SearchEngine;

namespace Starter.Controls;

public class SearchResultDataTemplate : IRecyclingDataTemplate
{
    public bool Match(object? data) => data is SearchResultData or null;

    public Control? Build(object? param, Control? existing)
    {
        if (param is not SearchResultData data) return null;

        return data.SearchResult is ControlSearchResult
            ? existing as ControlSearchResult ?? new ControlSearchResult { DataContext = data.SearchResult }
            : existing ?? new SearchResult();
    }

    public Control? Build(object? param) => Build(param, null);
}

public class ContextMenuDataTemplate(Dictionary<IContextMenuEntry, TextMatching.FuzzyMatch.FuzzyResult> fuzzyMatchResults) : IRecyclingDataTemplate
{
    public bool Match(object? data) => data is IContextMenuEntry or null;

    public Control Build(object? param) => new ContextMenuResult(fuzzyMatchResults);

    public Control Build(object? data, Control? existing) =>
        existing is ContextMenuResult
            ? existing
            : Build(data);
}
