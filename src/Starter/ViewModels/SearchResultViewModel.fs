namespace Starter.ViewModels

open Starter.SearchEngine
open Starter.Features.ResultScores

type SearchResultKind =
    | DynamicUnique = 0s
    | Static = 1s
    | DynamicInstant = 2s
    | Dynamic = 3s

type SearchResultViewModel(
    pos: SearchResultKind,
    searchResult: ISearchResult,
    searchEngineId: string
    ) =

    let mutable fuzzyMatchScore = 0s
    let mutable accentuationMap = Array.empty<bool>
    let icon = searchResult.Icon

    member _.Position = pos
    member _.SearchResult = searchResult
    member _.SearchEngineId = searchEngineId
    member _.FuzzyMatchScore
        with get () = fuzzyMatchScore
        and set v = fuzzyMatchScore <- v
    member _.AccentuationMap
        with get () = accentuationMap
        and set v = accentuationMap <- v

    static member DesignVM = SearchResultViewModel(
        SearchResultKind.Static,
        { new ISearchResult with
            member this.Id = ""
            member this.Name = "Zen Browser"
            member this.Description = "Application"
            member this.Keywords = Array.empty
            member this.Icon = StarterIconSource.Empty
            member this.ShowIfNoActivator = true
            member this.ActivatorFilter = Array.empty },
        "fake"
    )

    static member create pos (se: SearchEngine) (sr: ISearchResult) =
        SearchResultViewModel(pos, sr, se.Id)

    // Returns a low number for a result that should be on top of the list
    static member mapForComparison resultScoreDb (sr: SearchResultViewModel) =
        let struct (usageScore, d) =
            sr.SearchResult.Id
            |> ValueOption.ofObj
            |> ValueOption.map (ScoreDb.getResultScore resultScoreDb)
            |> ValueOption.defaultValue (struct (System.Int32.MaxValue, System.TimeSpan.MaxValue))
        let fuzzyMatchScore = float sr.FuzzyMatchScore

        struct (
            sr.Position,
            -(fuzzyMatchScore + (2. * usageScore)),
            d,
            sr.Name.Length,
            sr.Name
        )

    // UI Bindings
    member this.Name : string = this.SearchResult.Name
    member this.Icon = icon
