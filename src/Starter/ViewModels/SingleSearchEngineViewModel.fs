namespace Starter.ViewModels

open Starter.SearchEngine
open FluentAvalonia.UI.Controls

type SingleSearchEngineViewModel =
    { SearchEngine: ISearchEngine
      ShortName: string
      Icon: IconSource }

    static member create (searchEngine: ISearchEngine) =
        { SearchEngine = searchEngine
          ShortName = searchEngine.ShortName
          Icon = searchEngine.Icon |> StarterIconSource.build }
