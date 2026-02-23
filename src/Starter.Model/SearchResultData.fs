namespace Starter.Features

open System
open Starter.Features
open Starter.SearchEngine
open Fusil

type SearchResultData =
    { SearchResult: ISearchResult
      Priority: ResultPriority
      SearchEngineId: string
      mutable FuzzyMatchResult: FuzzyResult voption
      mutable AccentuationMap: bool array | null }

    static member createStatic (searchEngine: IStaticSearchEngine) searchResult =
        { SearchResult = searchResult
          Priority = ResultPriority.Static
          SearchEngineId = searchEngine.Id
          FuzzyMatchResult = ValueNone
          AccentuationMap = null }

    static member createDynamic (searchEngine: IDynamicSearchEngine) searchResult =
        { SearchResult = searchResult
          Priority = searchEngine.ResultsPriority
          SearchEngineId = searchEngine.Id
          FuzzyMatchResult = ValueNone
          AccentuationMap = null }

    // Returns a low number for a result that should be on top of the list
    static member getWeight resultScoreDb (sr: SearchResultData) =
        let struct (usageScore, d) =
            sr.SearchResult.Id
            |> ValueOption.ofObj
            |> ValueOption.map (ScoreDb.getResultScore resultScoreDb)
            |> ValueOption.defaultValue (struct (Int32.MaxValue, TimeSpan.MaxValue))

        let fuzzyMatchScore =
            match sr.FuzzyMatchResult with
            | ValueSome fuzzyResult -> float fuzzyResult.Score
            | ValueNone -> 0.

        struct (
            sr.Priority,
            -(fuzzyMatchScore + (2. * usageScore)),
            d,
            sr.SearchResult.Name.Length,
            sr.SearchResult.Name
        )

    member this.IsCustomSearchResult = this.SearchResult :? ICustomSearchResult
    member this.CustomControl =
        match this.SearchResult  with
        | :? ICustomSearchResult as r -> r.Control
        | _ -> null
