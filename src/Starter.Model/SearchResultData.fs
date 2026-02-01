namespace Starter.Features

open System
open Starter.Features
open Starter.SearchEngine
open Fusil

type SearchResultKind =
    | DynamicUnique = 0s
    | Static = 1s
    | DynamicInstant = 2s
    | Dynamic = 3s

type SearchResultData =
    { SearchResult: ISearchResult
      SearchResultKind: SearchResultKind
      SearchEngineId: string
      mutable FuzzyMatchResult: FuzzyResult voption
      mutable AccentuationMap: bool array | null }

    static member createStatic (searchEngine: StaticSearchEngine) searchResult =
        { SearchResult = searchResult
          SearchResultKind = SearchResultKind.Static
          SearchEngineId = searchEngine.Id
          FuzzyMatchResult = ValueNone
          AccentuationMap = null }

    static member createDynamic (searchEngine: DynamicSearchEngine) kind searchResult =
        { SearchResult = searchResult
          SearchResultKind = kind
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
            sr.SearchResultKind,
            -(fuzzyMatchScore + (2. * usageScore)),
            d,
            sr.SearchResult.Name.Length,
            sr.SearchResult.Name
        )
