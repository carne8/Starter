namespace Starter.Features

open System
open Starter.Features
open Starter.SearchEngine
open Starter.TextMatching.FuzzyMatch
open Starter.TextMatching.TextNormalization

[<Struct>]
type NormalizedStrings =
    { Name: System.Text.Rune array
      Keywords: System.Text.Rune array array | null }

type SearchResultData =
    { SearchResult: ISearchResult
      NormalizedStrings: NormalizedStrings voption
      Priority: ResultPriority
      SearchEngineId: string
      mutable FuzzyMatchResult: FuzzyResult voption
      mutable AccentuationMap: bool array | null }

    static member createStatic (searchEngine: IStaticSearchEngine) searchResult =
        { SearchResult = searchResult
          NormalizedStrings =
            ValueSome {
                Name = String.normalize searchResult.Name
                Keywords =
                    match searchResult.Keywords with
                    | null -> null
                    | keywords -> keywords |> Array.map String.normalize
            }
          Priority = ResultPriority.Static
          SearchEngineId = searchEngine.Id
          FuzzyMatchResult = ValueNone
          AccentuationMap = null }

    static member createDynamic (searchEngine: IDynamicSearchEngine) searchResult =
        { SearchResult = searchResult
          NormalizedStrings = ValueNone
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
