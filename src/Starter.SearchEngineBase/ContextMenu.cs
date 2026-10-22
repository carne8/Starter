using System;
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
    event EventHandler? OnLoaded;

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
    public Task<IContextMenuEntry[]> LoadItems(
        Avalonia.Platform.IPlatformHandle? platformHandle,
        Action<IContextMenuEntry, int> newEntryLoaded
    );
}
