using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Starter.SearchEngine;

public interface ISearchResult
{
    string Name { get; }
    Task<Avalonia.Media.Imaging.Bitmap?> LoadIcon();
}

public interface ISearchEngine
{
    string Id { get; }
    string DisplayName { get; }
    IObservable<IEnumerable<ISearchResult>> Search(CancellationToken cancellationToken, string query);
    void SearchResultSelected(ISearchResult selectedSearchResult);
}

public static class Constants
{
    public static readonly string[] SharedAssemblies = [ "Avalonia.Base" ];
}
