namespace Starter.ViewModels

open Starter.SearchEngine

type SingleSearchEngineViewModel =
    { SearchEngine: SearchEngine
      ShortName: string
      Icon: StarterIconSource }

    static member create (searchEngine: SearchEngine) =
        { SearchEngine = searchEngine
          ShortName = searchEngine.ShortName
          Icon = searchEngine.Icon }
