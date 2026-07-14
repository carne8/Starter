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

type ContextMenuEntryData =
    { Result: IContextMenuEntry
      mutable FuzzyMatchResult: FuzzyResult voption
      mutable AccentuationMap: bool array | null }

    member this.Name = this.Result.Name

    static member create result =
        { Result = result
          FuzzyMatchResult = ValueNone
          AccentuationMap = null }

    // Returns a low value for a result that should be on top of the list
    static member getWeight (resultScoreDb: IScoreDb) (r: ContextMenuEntryData) =
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

[<RequireQualifiedAccess>]
type ContextMenuResultData =
    | Separator
    | Entry of ContextMenuEntryData

    static member create (result: IContextMenuResult) =
        match result with
        | :? IContextMenuEntry as entry ->
            entry
            |> ContextMenuEntryData.create
            |> ContextMenuResultData.Entry

        | _ -> ContextMenuResultData.Separator

    static member getWeight (resultScoreDb: IScoreDb) (r: ContextMenuResultData) =
        match r with
        | Separator ->
            struct (
                ResultPriority.Search,
                Double.MaxValue,
                TimeSpan.MaxValue,
                Int32.MaxValue,
                String.Empty
            )
        | Entry entry -> ContextMenuEntryData.getWeight resultScoreDb entry

    member this.TryGetEntry(entry: ContextMenuEntryData outref) =
        match this with
        | Separator -> false
        | Entry data -> entry <- data; true
