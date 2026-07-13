using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Starter.Features;
using Starter.SearchEngine;

namespace Starter.Controls;

public partial class SearchResult : UserControl
{
    public SearchResult() => InitializeComponent();
}

public class SearchResultDataTemplate : IRecyclingDataTemplate
{
    public bool Match(object? data) => data is null || data is SearchResultData;

    public Control? Build(object? param, Control? existing)
    {
        if (param is not SearchResultData data) return null;

        return data.SearchResult is IControlSearchResult
            ? existing as ControlSearchResult ?? new ControlSearchResult()
            : existing ?? new SearchResult();
    }

    public Control? Build(object? param) => Build(param, null);
}
