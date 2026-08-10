using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Serilog;
using Starter.Controls.ContextMenu;
using Starter.Features;
using Starter.SearchEngine;

namespace Starter.Controls;

public class SearchResultDataTemplate : IRecyclingDataTemplate
{
    public bool Match(object? data) => data is null || data is SearchResultData;

    public Control? Build(object? param, Control? existing)
    {
        if (param is not SearchResultData data) return null;

        return data.SearchResult is IControlSearchResult
            ? existing as ControlSearchResult ?? new ControlSearchResult { DataContext = data.SearchResult }
            : existing ?? new SearchResult();
    }

    public Control? Build(object? param) => Build(param, null);
}


public class ContextMenuResultDataTemplate : IRecyclingDataTemplate
{
    public bool Match(object? data) => data is null or ContextMenuResultData;

    public Control? Build(object? param, Control? existing)
    {
        if (param is not ContextMenuResultData data) return null;

        if (data.IsLoading) return new ContextMenuLoadingResult();
        if (data.IsLoadFailed) return null;
        if (data.IsSeparator)
            return existing as ContextMenuResultSeparator ?? new ContextMenuResultSeparator();

        // Entry
        if (data.TryGetEntry(out var entry))
        {
            var c = existing as ContextMenuResult ?? new ContextMenuResult();
            c.DataContext = entry;
            return c;
        }

        return null;
    }

    public Control? Build(object? param) => Build(param, null);
}
