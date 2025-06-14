namespace Starter.ViewModels

open Starter.SearchEngine
open Avalonia.Controls

type SingleSearchEngineViewModel =
    { SearchEngine: ISearchEngine
      ShortName: string
      Icon: Control | null }

    static member create (searchEngine: ISearchEngine) =
        { SearchEngine = searchEngine
          ShortName = searchEngine.ShortName
          Icon = searchEngine.Icon |> StarterIconSource.buildWithFontSize 17 }
