namespace Starter.ViewModels

open Starter.SearchEngine
open Starter.Features.ResultScores
open Avalonia.Media
open Avalonia.Controls.Documents

type SearchResultViewModel(
    searchResult: ISearchResult,
    searchEngineId: string,
    searchEngineName: string
    ) =

    let inlineCollection = InlineCollection()
    let mutable fuzzyMatchResult: Fusil.Fusil.FuzzyResult option = None

    do
        inlineCollection.EnsureCapacity(searchResult.Name.Length)
        for i = 0 to searchResult.Name.Length-1 do
            let span = Span()
            searchResult.Name[i] |> string |> span.Inlines.Add
            span |> inlineCollection.Add

    member _.SearchResult = searchResult
    member _.SearchEngineId = searchEngineId
    member _.SearchEngineName = searchEngineName
    member _.FuzzyMatchResult = fuzzyMatchResult
    member _.Inlines = inlineCollection

    member _.SetFuzzyResult fuzzyResult =
        fuzzyMatchResult <- Some fuzzyResult

        for i = 0 to searchResult.Name.Length-1 do
            if fuzzyResult.MatchingPositions[i] then
                inlineCollection[i].FontWeight <- FontWeight.ExtraBold
            else
                inlineCollection[i].FontWeight <- FontWeight.Regular

    static member DesignVM = SearchResultViewModel(
        { new ISearchResult with
            member this.Id = ""
            member this.Name = "Zen Browser"
            member this.LoadIcon() = null },
        "fake",
        "Fake search engine"
    )

    static member create (se: ISearchEngine) (sr: ISearchResult) =
        SearchResultViewModel(sr, se.Id, se.DisplayName)

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
