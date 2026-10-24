namespace Starter.Features

open System
open Starter.Features
open Starter.SearchEngine
open Starter.TextMatching.FuzzyMatch
open Starter.TextMatching.TextNormalization

type SearchResultData =
    { SearchResult: SearchResult
      NormalizedName: Text.Rune array voption
      Priority: ResultPriority
      SearchEngineId: string
      mutable FuzzyMatchResult: FuzzyResult voption
      mutable AccentuationMap: bool array | null }

    static member createStatic (searchEngineId: string) searchResult =
        { SearchResult = searchResult
          NormalizedName = searchResult.Name |> String.normalize |> ValueSome
          Priority = ResultPriority.Static
          SearchEngineId = searchEngineId
          FuzzyMatchResult = ValueNone
          AccentuationMap = null }

    static member createDynamic (searchEngine: DynamicSearchEngine) searchResult =
        { SearchResult = searchResult
          NormalizedName = ValueNone
          Priority = searchEngine.ResultsPriority
          SearchEngineId = searchEngine.Id
          FuzzyMatchResult = ValueNone
          AccentuationMap = null }

    // Returns a low value for a result that should be on top of the list
    static member getWeight (resultScoreDb: IScoreDb) (sr: SearchResultData) =
        let struct (usageScore, d) =
            sr.SearchResult.Id
            |> ValueOption.ofObj
            |> ValueOption.map resultScoreDb.GetResultScore
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

module ContextMenuEntry =
    // Returns a low value for a result that should be on top of the list
    let getWeight (resultScoreDb: IScoreDb) (f: FuzzyResult voption) (r: IContextMenuEntry) =
        let struct (usageScore, d) =
            r.Id
            |> ValueOption.ofObj
            |> ValueOption.map resultScoreDb.GetResultScore
            |> ValueOption.defaultValue struct (Int32.MaxValue, TimeSpan.MaxValue)

        let fuzzyMatchScore =
            match f with
            | ValueSome fuzzyResult -> float fuzzyResult.Score
            | ValueNone -> 0.

        struct (
            ResultPriority.Static,
            -(fuzzyMatchScore + (2. * usageScore)),
            d,
            r.Name.Length,
            r.Name
        )
