namespace Starter.ViewModels

open Starter.Features
open Starter.Features.InternalSearchEngines

open System
open System.Threading
open System.Windows.Input
open System.Collections.Generic
open System.Reactive.Subjects

open ReactiveUI
open FsToolkit.ErrorHandling

module private Constants =
    let [<Literal>] ScoresFile = "result.scores"
    let [<Literal>] ScoresMaxAging = 10_000

/// App score based on https://github.com/ajeetdsouza/zoxide/wiki/Algorithm
module private Scores =
    let getResultSortIdx (scores: ScoresSaver.Scores) (result: SearchResultViewModel) =
        match scores.TryGetValue result.Result.Id with
        | false, _ -> 0., result.Name.ToLowerInvariant(), TimeSpan.MaxValue
        | true, (score, lastAccessDate) ->
            let d = DateTimeOffset.Now - lastAccessDate
            let s = float -score // That way, the highest score will be the first item of the array

            let f =
                if d.TotalHours < 1 then s * 4.
                elif d.TotalDays < 1 then s * 2.
                elif d.TotalDays < 7 then s / 2.
                else s / 4.

            f, result.Name.ToLowerInvariant(), d

    let increaseAppScore (scores: ScoresSaver.Scores) (resultId: string) =
        match scores.TryGetValue resultId with
        | true, (prevScore, _) ->
            scores[resultId] <- prevScore + 1, DateTimeOffset.Now
        | false, _ ->
            scores.Add(resultId, (1, DateTimeOffset.Now))

    let checkScoresMaxAging maxAge (scores: ScoresSaver.Scores) =
        let totalScore =
            scores
            |> Seq.sumBy (_.Value >> fst)
            |> float

        if totalScore > maxAge then
            let k = (0.9 * maxAge) / totalScore

            for kv in scores do
                let score, lastAccessDate = kv.Value
                let newScore = float score * k |> Math.Round |> int

                match newScore with
                | 0 -> scores.Remove kv.Key |> ignore
                | _ -> scores[kv.Key] <- newScore, lastAccessDate

type MainWindowViewModel() =
    inherit ViewModelBase()

    let baseConfig =
        match Config.getConfig() with
        | Error _ -> failwith "Error"
        | Ok r ->
            match r with
            | null -> failwith "Error"
            | r -> r

    // ---
    let config = new BehaviorSubject<_>(baseConfig)
    let settingsSearchEngine = SettingsSearchEngine config.Value
    let searchEngines =
        [| System.IO.Path.Combine(
            __SOURCE_DIRECTORY__,
            "../Plugins/Starter.ApplicationSearchEngine/bin/Debug/net9.0/Starter.ApplicationSearchEngine.dll"
        ) |]
        |> Array.collect SearchEngineLoading.loadSearchEngines
        |> Array.append [| settingsSearchEngine |]
        |> Array.map (fun searchEngine -> searchEngine.Id, searchEngine)
        |> dict
    let mutable resultScores = None

    // State
    let searchResults = new BehaviorSubject<SearchResultViewModel array>(Array.empty)
    let mutable sortedSearchResults: IObservable<_> = searchResults
    let mutable text = "Starter"
    let mutable searchCts = new CancellationTokenSource()

    let onTextChanged newText =
        searchCts.Cancel()
        searchCts <- new CancellationTokenSource()
        let resultsList = List<_>() // Store all results for the current query

        for kv in searchEngines do
            let searchEngine = kv.Value
            try
                let obs = searchEngine.Search(newText, searchCts.Token)
                let sub = obs |> Observable.subscribe (fun results ->
                    results
                    |> Array.map (fun result ->
                        SearchResultViewModel(
                            searchEngine.Id,
                            searchEngine.DisplayName,
                            result
                        )
                    )
                    |> resultsList.AddRange

                    resultsList.ToArray()
                    |> searchResults.OnNext
                )

                searchCts.Token.Register(fun _ -> sub.Dispose()) |> ignore
            with e -> printfn "Search engine query failed (%s): %s" searchEngine.DisplayName e.Message

    let validateResult (result: SearchResultViewModel) =
        task {
            match resultScores with
            | None -> ()
            | Some scores ->
                Scores.increaseAppScore scores result.Result.Id
                Scores.checkScoresMaxAging Constants.ScoresMaxAging scores

                // Save to file
                scores
                |> ScoresSaver.writeToFile Constants.ScoresFile
                |> ignore

            let se = searchEngines[result.SearchEngineId]
            se.SearchResultSelected result.Result
        }

    do
        sortedSearchResults |> Observable.subscribe (printfn "%A") |> ignore

        // Sync config changes with the settings search engine (and the settings page)
        settingsSearchEngine.Configuration
        |> Observable.subscribe config.OnNext
        |> ignore

        // Load result scores
        Constants.ScoresFile
        |> ScoresSaver.readFromFile
        |> Task.map (fun scores ->
            resultScores <- Some scores
            sortedSearchResults <- searchResults |> Observable.map (Array.sortBy (Scores.getResultSortIdx scores))
            searchResults.Value |> searchResults.OnNext // Sort already loaded results
        )
        |> ignore

    let hideCommand = ReactiveCommand.Create(fun () -> ())
    member _.HideCommand = hideCommand

    member _.ValidateCommand(searchResult: SearchResultViewModel | null) =
        match searchResult with
        | null -> ()
        | searchResult -> searchResult |> validateResult |> ignore
        (hideCommand :> ICommand).Execute()

    member _.Config = config

    member this.SearchResults = sortedSearchResults
    member _.Text
        with get () = text
        and set v = text <- v; onTextChanged v

    #if DEBUG
    static member DesignVM = MainWindowViewModel()
    #endif
