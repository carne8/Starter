using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Starter.SearchEngine;
using Starter.TextMatching;

namespace Starter.Controls;

using FuzzyMatchResults = Dictionary<SearchResult, FuzzyMatch.FuzzyResult>;
using ContextMenuFuzzyMatchResults = Dictionary<IContextMenuEntry, FuzzyMatch.FuzzyResult>;

public abstract class RecyclingDataTemplate<T> : IRecyclingDataTemplate where T : Control
{
    private readonly Queue<T> pool = new();

    public abstract bool Match(object? data);

    public Control Build(object? param) => Build(param, null);
    public Control Build(object? data, Control? existing)
    {
        if (existing is not T view && !pool.TryDequeue(out view!))
        {
            view = CreateControl();
            view.DetachedFromVisualTree += (_, _) =>
            {
                view.DataContext = null;
                pool.Enqueue(view);
            };
        }
        view.DataContext = data;
        return view;
    }

    protected abstract T CreateControl();
}

public class SearchResultDataTemplate(FuzzyMatchResults fuzzyMatchResults) : RecyclingDataTemplate<SearchResultView>
{
    public override bool Match(object? data) => data is SearchResult and not ControlSearchResult;
    protected override SearchResultView CreateControl() => new(fuzzyMatchResults);
}

public class ControlSearchResultDataTemplate : RecyclingDataTemplate<ControlSearchResultView>
{
    public override bool Match(object? data) => data is ControlSearchResult;
    protected override ControlSearchResultView CreateControl() => new();
}

public class ContextMenuEntryDataTemplate(ContextMenuFuzzyMatchResults fuzzyMatchResults) : RecyclingDataTemplate<ContextMenuResultView>
{
    public override bool Match(object? data) => data is IContextMenuEntry;
    protected override ContextMenuResultView CreateControl() => new(fuzzyMatchResults);
}
