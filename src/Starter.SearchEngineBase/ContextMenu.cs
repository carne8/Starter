using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Starter.SearchEngine;

public enum ContextMenuStatus
{
    Loading,
    Loaded,
    Failed,
    Separator
}

/// <summary>
/// An invokable entry of a context menu
/// </summary>
public interface IContextMenuEntry
{
    ContextMenuStatus Status { get; }

    string? Id { get; }
    string Name { get; }
    string? Description { get; }
    /// <summary>
    /// Additional strings that are compared to the user query
    /// </summary>
    string[]? Keywords { get; }
    StarterIconSource Icon { get; }
    IContextMenuLoader? Invoke();
}

/// <summary>
/// Something to load a context menu gradually
/// </summary>
public interface IContextMenuLoader
{
    public Task<ObservableCollection<IContextMenuEntry>> LoadItems(Avalonia.Platform.IPlatformHandle? platformHandle);
}
