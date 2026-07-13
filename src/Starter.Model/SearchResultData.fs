namespace Starter.Features

open System
open Starter.Features
open Starter.SearchEngine
open Starter.TextMatching.FuzzyMatch
open Starter.TextMatching.TextNormalization

type SearchResultData =
    { SearchResult: ISearchResult
      NormalizedName: System.Text.Rune array voption
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

    static member createDynamic (searchEngine: IDynamicSearchEngine) searchResult =
        { SearchResult = searchResult
          NormalizedName = ValueNone
          Priority = searchEngine.ResultsPriority
          SearchEngineId = searchEngine.Id
          FuzzyMatchResult = ValueNone
          AccentuationMap = null }

    // Returns a low number for a result that should be on top of the list
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


type ContextMenuResultData =
    { Result: IContextMenuResult
      mutable FuzzyMatchResult: FuzzyResult voption
      mutable AccentuationMap: bool array | null }

    member this.Name = this.Result.Name

    static member create result =
        { Result = result
          FuzzyMatchResult = ValueNone
          AccentuationMap = null }

    // Returns a low number for a result that should be on top of the list
    static member getWeight (resultScoreDb: IScoreDb) (r: ContextMenuResultData) =
        let struct (usageScore, d) =
            r.Result.Id
            |> ValueOption.ofObj
            |> ValueOption.map resultScoreDb.GetResultScore
            |> ValueOption.defaultValue (struct (Int32.MaxValue, TimeSpan.MaxValue))

        let fuzzyMatchScore =
            match r.FuzzyMatchResult with
            | ValueSome fuzzyResult -> float fuzzyResult.Score
            | ValueNone -> 0.

        struct (
            ResultPriority.Static,
            -(fuzzyMatchScore + (2. * usageScore)),
            d,
            r.Name.Length,
            r.Name
        )
