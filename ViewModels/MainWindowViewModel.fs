namespace Starter.ViewModels

open Starter.Features
open Starter.Features.InternalSearchEngines
open Starter.Features.ResultScoreDb
open Starter.Features.CustomCollections

open System.Threading
open System.Windows.Input
open System.Reactive.Subjects

open ReactiveUI
open FsToolkit.ErrorHandling

module private Constants =
    let [<Literal>] ScoresFile = "result-scores.db"
    let [<Literal>] ScoresMaxAging = 10_000

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
    let mutable resultScoreDb = None

    // State
    let mutable searchResults = ObservableList<SearchResultViewModel>(50)
    let mutable text = "Starter"
    let mutable searchCts = new CancellationTokenSource()

    let onTextChanged newText =
        searchCts.Cancel()
        searchCts <- new CancellationTokenSource()
        searchResults.Clear()

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
                    |> searchResults.AddRange
                    resultScoreDb |> Option.iter (fun scoreDb -> searchResults.Sort(SearchResultViewModel.CompareTwo scoreDb))
                )

                searchCts.Token.Register(fun _ -> sub.Dispose()) |> ignore
            with e -> printfn $"Search engine query failed ({searchEngine.DisplayName}): {e.Message}"

    let validateResult (result: SearchResultViewModel) =
        task {
            match resultScoreDb with
            | None -> ()
            | Some scores ->
                ScoreDb.increaseAppScore scores result.Result.Id
                ScoreDb.runMaxAgingPolicy Constants.ScoresMaxAging scores

                // Save changes to file
                scores
                |> ScoreDb.writeToFile Constants.ScoresFile
                |> ignore

            let se = searchEngines[result.SearchEngineId]
            se.SearchResultSelected result.Result
        }

    do
        // Sync config changes with the settings search engine (and the settings page)
        settingsSearchEngine.Configuration
        |> Observable.subscribe config.OnNext
        |> ignore

        // Load result scores
        Constants.ScoresFile
        |> ScoreDb.readFromFile
        |> Task.map (fun scoreDb ->
            resultScoreDb <- Some scoreDb

            // Sort already loaded results
            searchResults.Sort(SearchResultViewModel.CompareTwo scoreDb)
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

    member this.SearchResults = searchResults
    member _.Text
        with get () = text
        and set v = text <- v; onTextChanged v

    #if DEBUG
    static member DesignVM = MainWindowViewModel()
    #endif
