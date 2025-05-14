namespace Starter.ViewModels

open Fusil
open Starter.Features
open Starter.Features.InternalSearchEngines
open Starter.Features.ResultScores
open Starter.Features.CustomCollections
open Starter.SearchEngine

open System.IO
open System.Threading
open System.Threading.Tasks
open System.Windows.Input
open System.Reactive.Subjects

open ReactiveUI
open FsToolkit.ErrorHandling

type MainWindowViewModel(baseConfig: Config.Configuration, resultScoreDb: ResultScores.ScoreDb) =
    inherit ViewModelBase()

    // ---
    let config = new BehaviorSubject<_>(baseConfig)
    let fusilSlab = Slab.createDefault()

    // Search engines loading
    let settingsSearchEngine = SettingsSearchEngine config.Value
    let staticSearchEngines, _dynamicSearchEngines, searchEngines =
        Path.Combine(__SOURCE_DIRECTORY__, "../Plugins/Starter.ApplicationSearchEngine/bin/Debug/net9.0/")
        |> SearchEngineLoading.loadSearchEngineFromDirectory

        // Add settings search engine
        |> fun (staticSEs, dynamicSEs, searchEngines) ->
            searchEngines.Add(settingsSearchEngine.Id, settingsSearchEngine)
            staticSEs |> Array.append [| settingsSearchEngine |],
            dynamicSEs,
            searchEngines

    // State
    let staticSearchResults = new BehaviorSubject<SearchResultViewModel array>(Array.empty)
    let mutable searchResults = ObservableList<SearchResultViewModel>(50)
    let mutable text = "starter"
    let mutable searchCts = new CancellationTokenSource()

    let onTextChanged (newText: string) =
        searchCts.Cancel()
        searchCts <- new CancellationTokenSource()
        searchResults.Clear()

        // Load static results
        let query = newText.ToCharArray()
        let staticResultSubscription =
            staticSearchResults |> Observable.subscribe (fun staticResults ->
                let filteredResults =
                    staticResults |> Array.choose (fun srVm ->
                        match Fusil.fuzzyMatch false true true fusilSlab query srVm.Name with
                        | Some fusilResult when fusilResult.Score > 0s -> Some { srVm with FuzzyMatchResult = Some fusilResult }
                        | _ -> None
                    )

                filteredResults |> searchResults.AddRange
                searchResults.Sort(SearchResultViewModel.mapForComparison resultScoreDb)

                // System.Console.Clear()
                // printfn "Displaying the static results"
                // searchResults.List |> Seq.iter (fun (r: SearchResultViewModel) ->
                //     printfn "%s: %i -> %f"
                //         r.Name
                //         r.FuzzyMatchResult.Value.Score
                //         (r
                //          |> SearchResultViewModel.mapForComparison resultScoreDb
                //          |> fun (x, _, _, _) -> x)
                // )
            )
        searchCts.Token.Register(fun _ -> staticResultSubscription.Dispose()) |> ignore

        // Load dynamic results
        // for searchEngine in dynamicSearchEngines do
        //     try
        //         let obs = searchEngine.Search(newText, searchCts.Token)
        //         let sub = obs |> Observable.subscribe (fun results ->
        //             results
        //             |> Array.map (fun result ->
        //                 SearchResultViewModel(
        //                     searchEngine.Id,
        //                     searchEngine.DisplayName,
        //                     result
        //                 )
        //             )
        //             |> searchResults.AddRange
        //             resultScoreDb |> Option.iter (fun scoreDb -> searchResults.Sort(SearchResultViewModel.CompareTwo scoreDb))
        //         )
        //
        //         searchCts.Token.Register(fun _ -> sub.Dispose()) |> ignore
            // with e -> printfn $"Search engine query failed ({searchEngine.DisplayName}): {e.Message}"

    let validateResult (result: SearchResultViewModel) =
        task {
            // Send the result to the search engine
            let se = searchEngines[result.SearchEngineId]
            se.SearchResultSelected result.SearchResult

            // Increase score
            resultScoreDb |> ScoreDb.increaseAppScore result.SearchResult.Id
            resultScoreDb |> ScoreDb.runMaxAgingPolicy Constants.ScoresMaxAging

            // Resort results (for next opening)
            searchResults.Sort(SearchResultViewModel.mapForComparison resultScoreDb)

            // Save score changes to file
            resultScoreDb
            |> ScoreDb.writeToFile Constants.ResultScoresFile
            |> ignore
        }

    do
        // Sync config changes with the settings search engine (and the settings page)
        // Save config to a file when it changes
        settingsSearchEngine.Configuration
        |> Observable.subscribe (fun newConfig ->
            config.OnNext newConfig
            newConfig |> Config.saveConfig Constants.ConfigFile |> ignore
        )
        |> ignore

        // Load static results
        task {
            let! resultVMs =
                staticSearchEngines
                |> Array.Parallel.map (fun se ->
                    se.LoadResults()
                    |> Task.map (Array.map (SearchResultViewModel.create se None))
                )
                |> Task.WhenAll
                |> Task.map Array.concat

            resultVMs |> Array.Parallel.sortInPlaceBy (SearchResultViewModel.mapForComparison resultScoreDb)

            staticSearchResults.OnNext resultVMs
        }
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
    static member DesignVM = MainWindowViewModel(Config.Configuration.Default, Array.empty |> dict)
    #endif
