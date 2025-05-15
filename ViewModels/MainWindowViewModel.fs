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

open Avalonia.Threading
open ReactiveUI
open FsToolkit.ErrorHandling

type MainWindowViewModel(baseConfig: Config.Configuration, resultScoreDb: ResultScores.ScoreDb) =
    inherit ViewModelBase()

    // ---
    let config = new BehaviorSubject<_>(baseConfig)
    let fusilSlab = Slab.createDefault()

    // Search engines loading
    let settingsSearchEngine = SettingsSearchEngine config.Value
    let staticSearchEngines, dynamicSearchEngines =
        Path.Combine(__SOURCE_DIRECTORY__, "../Plugins/Starter.ApplicationSearchEngine/bin/Debug/net9.0/")
        |> SearchEngineLoading.loadSearchEngineFromDirectory

        // Add settings search engine
        |> fun (staticSEs, dynamicSEs) ->
            staticSEs |> Array.append [| settingsSearchEngine |],
            dynamicSEs

    let searchEngines =
        Array.append
            (staticSearchEngines |> unbox<ISearchEngine array>)
            (dynamicSearchEngines |> unbox<ISearchEngine array>)
        |> Array.map (fun se -> se.Id, se)
        |> dict

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
                    staticResults |> Array.filter (fun srVm ->
                        match Fusil.fuzzyMatch false true true fusilSlab query srVm.Name with
                        | Some fusilResult when fusilResult.Score > 0s ->
                            srVm.SetFuzzyResult fusilResult
                            true
                        | _ -> false
                    )

                filteredResults |> searchResults.AddRange
                searchResults.Sort(SearchResultViewModel.mapForComparison resultScoreDb)
            )
        searchCts.Token.Register(fun _ -> staticResultSubscription.Dispose()) |> ignore

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
            let loadSearchEngineResults (se: StaticSearchEngine) =
                se.LoadResults() |> Task.bind (fun results ->
                    Dispatcher.UIThread
                        .InvokeAsync(fun () -> results |> Array.map (SearchResultViewModel.create se))
                        .GetTask()
                )

            let! resultVMs =
                staticSearchEngines
                |> Array.Parallel.map loadSearchEngineResults
                |> Task.WhenAll
                |> Task.map Array.concat

            resultVMs |> Array.Parallel.sortInPlaceBy (SearchResultViewModel.mapForComparison resultScoreDb)
            Dispatcher.UIThread.Post(fun () ->
                staticSearchResults.OnNext resultVMs
            )
        } |> ignore

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
