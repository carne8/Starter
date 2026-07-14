using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Starter.Features;

namespace Starter.Controls;

public partial class ContextMenuResult : UserControl
{
    public ContextMenuResult() => InitializeComponent();
}

public class ContextMenuResultDataTemplate : IRecyclingDataTemplate
{
    public bool Match(object? data) => data is null or ContextMenuResultData;

    public Control? Build(object? param, Control? existing)
    {
        if (param is not ContextMenuResultData data) return null;

        // Entry
        if (data.TryGetEntry(out var entry))
        {
            var c = existing as ContextMenuResult ?? new ContextMenuResult();
            c.DataContext = entry;
            return c;
        }

        // Separator
        if (data.IsSeparator)
            return existing as ResultSeparator ?? new ResultSeparator();

        return null;
    }

    public Control? Build(object? param) => Build(param, null);
}
