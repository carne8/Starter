namespace Starter.ViewModels

open Starter.SearchEngine
open Starter.Features.ResultScores

type SearchResultPosition =
    | Important = 0s
    | Normal = 1s
    | Low = 2s

type SearchResultViewModel(
    pos: SearchResultPosition,
    searchResult: ISearchResult,
    searchEngineId: string
    ) =

    let mutable fuzzyMatchResult: Fusil.Fusil.FuzzyResult option = None
    let mutable accentuationMap = Array.empty<bool>
    let icon = searchResult.Icon |> StarterIconSource.buildWithFontSize 35

    member _.Position = pos
    member _.SearchResult = searchResult
    member _.SearchEngineId = searchEngineId
    member _.FuzzyMatchResult = fuzzyMatchResult
    member _.AccentuationMap
        with get () = accentuationMap
        and set v = accentuationMap <- v

    static member DesignVM = SearchResultViewModel(
        SearchResultPosition.Normal,
        { new ISearchResult with
            member this.Id = ""
            member this.Name = "Zen Browser"
            member this.Description = "Application"
            member this.Icon = StarterIconSource.Empty },
        "fake"
    )

    static member create pos (se: ISearchEngine) (sr: ISearchResult) =
        SearchResultViewModel(pos, sr, se.Id)

    static member mapForComparison resultScoreDb (sr: SearchResultViewModel) =
        let struct (usageScore, d) = sr.SearchResult.Id |> ScoreDb.getResultScore resultScoreDb
        let fuzzyMatchScore =
            match sr.FuzzyMatchResult with
            | Some fuzzyResult -> float fuzzyResult.Score
            | None -> 0.

        sr.Position,
        -(fuzzyMatchScore + (2. * usageScore)),
        d,
        sr.Name.Length,
        sr.Name

    // UI Bindings
    member this.Name : string = this.SearchResult.Name
    member this.Icon = icon
