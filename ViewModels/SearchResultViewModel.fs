namespace Starter.ViewModels

open Starter.SearchEngine

type private FakeSR =
    { Name: string }
    interface ISearchResult with
        member this.Id = ""
        member this.Name = this.Name
        member this.LoadIcon() = task { return null }

type SearchResultViewModel(seId: string, seName: string, sr: ISearchResult) =
    member _.Result = sr
    member _.SearchEngineId = seId
    member _.SearchEngineName = seName

    // Bindings
    member _.Name = sr.Name
    member _.LoadIcon() = sr.LoadIcon()

    static member DesignVM = SearchResultViewModel("fake", "Fake search engine", { Name = "Zen Browser" })
