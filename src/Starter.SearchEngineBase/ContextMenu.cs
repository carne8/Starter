using System;

namespace Starter.SearchEngine;

/// <summary>
/// Anything returned by a `IContextMenuLoader`
/// </summary>
public interface IContextMenuResult;

/// <summary>
/// An invokable entry of a context menu
/// </summary>
public interface IContextMenuEntry : IContextMenuResult
{
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
/// A separator for a context menu, separating its entries
/// </summary>
public sealed class ContextMenuSeparator : IContextMenuResult
{
    private ContextMenuSeparator() {}
    public static readonly ContextMenuSeparator Instance = new();
}

/// <summary>
/// Something to load a context menu gradually
/// </summary>
public interface IContextMenuLoader
{
    public void LoadItems(
        Avalonia.Platform.IPlatformHandle? platformHandle,
        Action<int> itemsListed,
        Action<(IContextMenuResult, int)[]> resultLoaded,
        Action<(Exception, int)[]> resultFailed,
        Action completed
    );
}
