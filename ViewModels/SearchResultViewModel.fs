namespace Starter.ViewModels

open Starter.SearchEngine
open Starter.Features.ResultScoreDb

type private FakeSR =
    { Name: string }
    interface ISearchResult with
        member this.Id = ""
        member this.Name = this.Name
        member this.LoadIcon() = null

type SearchResultViewModel(seId: string, seName: string, sr: ISearchResult) =
    member _.Result = sr
    member _.SearchEngineId = seId
    member _.SearchEngineName = seName

    // Bindings
    member _.Name = sr.Name
    member _.LoadIcon() = sr.LoadIcon()

    static member CompareTwo resultScoreDb (sr1: SearchResultViewModel) (sr2: SearchResultViewModel) =
        let s1, d1 = sr1.Result.Id |> ScoreDb.getResultScore resultScoreDb
        let s2, d2 = sr2.Result.Id |> ScoreDb.getResultScore resultScoreDb
        compare (s1, sr1.Name, d1) (s2, sr2.Name, d2)

    static member DesignVM = SearchResultViewModel("fake", "Fake search engine", { Name = "Zen Browser" })
