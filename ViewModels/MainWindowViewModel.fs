namespace Starter.ViewModels

open System
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

open Fusil
open Fusil.TextNormalization
open Avalonia.Threading
open ReactiveUI

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
    let mutable singleSearchEngineMode = new BehaviorSubject<ISearchEngine option>(None)

    let onTextChanged (newText: string) =
        searchCts.Cancel()
        searchCts <- new CancellationTokenSource()
        let bindToCts (sub: IDisposable) = searchCts.Token.Register(fun _ -> sub.Dispose()) |> ignore

        let query =
            newText
            |> String.normalize
            |> Array.map System.Text.Rune.ToLowerInvariant

        let fuzzyMatch = Fusil.fuzzyMatch false true true fusilSlab query

        singleSearchEngineMode.Subscribe(fun singleSe ->
            match singleSe with
            | Some (:? DynamicSearchEngine as se) -> // TODO: Load dynamic results
                se.Search(newText, searchCts.Token)
                |> Observable.subscribe (fun r -> printfn "Dynamic results: %A" r)
                |> bindToCts
            | _ ->
                let isSearchEngineActivated seId =
                    match singleSearchEngineMode.Value with
                    | None -> true
                    | Some se -> se.Id = seId

                staticSearchResults.Subscribe(fun staticResults ->
                    let filteredResults =
                        staticResults |> Array.filter (fun srVm ->
                            if srVm.SearchEngineId |> isSearchEngineActivated |> not then
                                false
                            else
                                srVm.Name
                                |> fuzzyMatch
                                |> function
                                    | Some fusilResult when fusilResult.Score > 0s ->
                                        srVm.SetFuzzyResult fusilResult
                                        true
                                    | _ -> false
                        )

                    searchResults.Clear()
                    filteredResults |> searchResults.AddRange
                    searchResults.Sort(SearchResultViewModel.mapForComparison resultScoreDb)
                )
                |> bindToCts
        )
        |> bindToCts

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
        for se in staticSearchEngines do
            Task.Run<unit>(fun () -> task {
                let! results = se.LoadResults()

                // SearchResultViewModel instantiation must happen on UI thread in order to create span controls
                // Also staticSearchResults.OnNext must happen on UI thread
                Dispatcher.UIThread.Post(fun () ->
                    let newStaticResults =
                        results
                        |> Array.map (SearchResultViewModel.create se)
                        |> Array.append staticSearchResults.Value

                    newStaticResults |> Array.Parallel.sortInPlaceBy (SearchResultViewModel.mapForComparison resultScoreDb)
                    staticSearchResults.OnNext newStaticResults
                    printfn "%s results loaded" se.DisplayName
                )
            }) |> ignore

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

    member this.SingleSearchEngineMode = singleSearchEngineMode

    #if DEBUG
    static member DesignVM = MainWindowViewModel(Config.Configuration.Default, Array.empty |> dict)
    #endif
