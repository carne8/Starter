using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS9113 // Parameter unread

namespace Starter.SearchEngine;

public interface ISearchResult
{
    string Id { get; }
    string Name { get; }
    Task<Avalonia.Media.Imaging.Bitmap?> LoadIcon();
}

public abstract class SearchEngineBase(string pluginPath)
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract IObservable<IEnumerable<ISearchResult>> Search(string query, CancellationToken cancellationToken);
    public abstract void SearchResultSelected(ISearchResult selectedSearchResult);
}

public static class Constants
{
    public static readonly string[] SharedAssemblies = ["Avalonia.Base"];
}
