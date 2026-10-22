namespace Starter.Features

open System
open System.ComponentModel
open System.Collections.Generic
open Starter.Features
open Starter.SearchEngine
open Starter.TextMatching.FuzzyMatch
open Starter.TextMatching.TextNormalization

type SearchResultData =
    { SearchResult: ISearchResult
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
      mutable AccentuationMap: bool array | null
      EventHandlers: List<PropertyChangedEventHandler> }

    member this.Name = this.Result.Name
    member this.Description = this.Result.Description
    member this.Icon = this.Result.Icon
    member this.IsLoading = this.Result.Status = ContextMenuStatus.Loading
    member this.IsLoaded = this.Result.Status = ContextMenuStatus.Loaded
    member this.IsFailed = this.Result.Status = ContextMenuStatus.Failed
    member this.IsSeparator = this.Result.Status = ContextMenuStatus.Separator

    static member create (result: IContextMenuEntry) =
        let handlers = List<_>()
        result.OnLoaded.Subscribe(fun _ ->
            for i = 0 to handlers.Count - 1 do
                let h: PropertyChangedEventHandler = handlers[i]
                h.Invoke(null, PropertyChangedEventArgs null)
        ) |> ignore

        { Result = result
          FuzzyMatchResult = ValueNone
          AccentuationMap = null
          EventHandlers = handlers }

    // Returns a low value for a result that should be on top of the list
    static member getWeight (resultScoreDb: IScoreDb) (r: ContextMenuEntryData) =
        let struct (usageScore, d) =
            r.Result.Id
            |> ValueOption.ofObj
            |> ValueOption.map resultScoreDb.GetResultScore
            |> ValueOption.defaultValue struct (Int32.MaxValue, TimeSpan.MaxValue)

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

    interface INotifyPropertyChanged with
        member this.add_PropertyChanged (handler: (PropertyChangedEventHandler | null)) =
            match handler with
            | null -> ()
            | handler -> this.EventHandlers.Add handler
        member this.remove_PropertyChanged (handler: (PropertyChangedEventHandler | null)) =
            match handler with
            | null -> ()
            | handler -> this.EventHandlers.Remove handler |> ignore