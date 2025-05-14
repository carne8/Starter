namespace Starter.ViewModels

open Starter.SearchEngine
open Starter.Features.ResultScores

type private FakeSR =
    { Name: string }
    interface ISearchResult with
        member this.Id = ""
        member this.Name = this.Name
        member this.LoadIcon() = null

type SearchResultViewModel =
    { SearchResult: ISearchResult
      SearchEngineId: string
      SearchEngineName: string
      FuzzyMatchResult: Fusil.Fusil.FuzzyResult option }

    static member DesignVM =
        { SearchResult = { Name = "Zen Browser" }
          SearchEngineId = "fake"
          SearchEngineName = "Fake search engine"
          FuzzyMatchResult = None }

    static member create (se: ISearchEngine) fuzzyMatchResult (sr: ISearchResult) =
        { SearchResult = sr
          SearchEngineId = se.Id
          SearchEngineName = se.DisplayName
          FuzzyMatchResult = fuzzyMatchResult }

    static member mapForComparison resultScoreDb (sr: SearchResultViewModel) =
        let struct (usageScore, d) = sr.SearchResult.Id |> ScoreDb.getResultScore resultScoreDb
        let fuzzyMatchScore =
            match sr.FuzzyMatchResult with
            | Some fuzzyResult -> float fuzzyResult.Score
            | None -> 0.

        -(fuzzyMatchScore + (2. * usageScore)),
        d,
        sr.Name.Length,
        sr.Name

and SearchResultViewModel with
    // UI Bindings
    member this.Name : string = this.SearchResult.Name
    member this.LoadIcon() = this.SearchResult.LoadIcon()
