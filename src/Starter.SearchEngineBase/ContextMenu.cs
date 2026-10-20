using System;
using System.Threading.Tasks;

namespace Starter.SearchEngine;

/// <summary>
/// An invokable entry of a context menu
/// </summary>
public interface IContextMenuEntry
{
    bool Loaded { get; }
    event EventHandler<bool>? OnLoaded;

    bool IsSeparator { get; }

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
