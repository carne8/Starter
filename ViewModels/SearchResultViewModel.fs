namespace Starter.ViewModels

open Starter.ViewModels
open Starter.SearchEngine

type SearchResultViewModel(se: string, sr: ISearchResult) =
    member _.Result = sr
    member _.SearchEngineName = se

    // Bindings
    member _.Name = sr.Name
    member _.LoadIcon() = sr.LoadIcon()
