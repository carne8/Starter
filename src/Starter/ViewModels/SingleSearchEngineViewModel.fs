namespace Starter.ViewModels

open Starter.SearchEngine
open Avalonia.Controls

type SingleSearchEngineViewModel =
    { SearchEngine: SearchEngine
      ShortName: string
      Icon: Control | null }

    static member create (searchEngine: SearchEngine) =
        { SearchEngine = searchEngine
          ShortName = searchEngine.ShortName
          Icon = searchEngine.Icon |> StarterIconSource.buildWithFontSize 17 }
