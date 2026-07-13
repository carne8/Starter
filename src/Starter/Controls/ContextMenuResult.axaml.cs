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

        // Separator
        if (data.Result.IsSeparator)
            return existing as ResultSeparator ?? new ResultSeparator();

        // No recycling
        return existing as ContextMenuResult ?? new ContextMenuResult();
    }

    public Control? Build(object? param)
    {
        if (param is not ContextMenuResultData data) return null;
        return data.Result.IsSeparator
            ? new ResultSeparator()
            : new ContextMenuResult();
    }
}
