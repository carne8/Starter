namespace Starter.ViewModels

open Starter.Features
open Starter.Features.InternalSearchEngines
open Starter.Features.ResultScores
open Starter.Features.CustomCollections
open Starter.SearchEngine

open System
open System.Collections.Generic
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
    // ---
    let config = new BehaviorSubject<_>(baseConfig)
    let fusilSlab = Slab.createDefault()

    // Search engines loading
    let staticSearchEngines, dynamicSearchEngines =
        Path.Combine(__SOURCE_DIRECTORY__, "../Plugins/Starter.ApplicationSearchEngine/bin/Debug/net9.0/")
        |> SearchEngineLoading.loadSearchEngineFromDirectory
        |> fun (staticSEs, dynamicSEs) -> staticSEs |> List, dynamicSEs |> List

    let searchEngines =
        Seq.append
            (staticSearchEngines |> Seq.cast<ISearchEngine>)
            (dynamicSearchEngines |> Seq.cast<ISearchEngine>)
        |> Seq.map (fun se -> KeyValuePair(se.Id, se))
        |> Dictionary

    let searchEngineFromPrefix = new BehaviorSubject<_>(Array.empty |> dict) // Bound to searchEngines in `do`

    // Commands (to interact with view)
    let hideCommand = ReactiveCommand.Create(fun () -> ())
    let emptyTextBoxCommand = ReactiveCommand.Create(fun () -> ())

    // State
    let staticSearchResults = new BehaviorSubject<SearchResultViewModel array>(Array.empty)
    let searchResults = ObservableList<SearchResultViewModel>(100)
    let mutable text = "starter"
    let mutable searchCts = new CancellationTokenSource()
    let mutable singleSearchEngineMode = new BehaviorSubject<ISearchEngine option>(None)

    let onTextChanged (newText: string) =
        match searchEngineFromPrefix.Value.TryGetValue newText with
        | false, _ -> ()
        | true, se ->
            se
            :> ISearchEngine
            |> Some
            |> singleSearchEngineMode.OnNext
            (emptyTextBoxCommand :> ICommand).Execute()

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
                |> Observable.subscribe (printfn "Dynamic results: %A")
                |> bindToCts
            | _ ->
                let isSearchEngineActivated seId =
                    match singleSe with
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
        // Load settings search engine
        let settingsSearchEngine = SettingsSearchEngine(config.Value, searchEngines)
        staticSearchEngines.Add(settingsSearchEngine)
        searchEngines.Add(settingsSearchEngine.Id, settingsSearchEngine)

        // Sync searchEngineFromPrefix with config
        config
        |> Observable.subscribe (fun config ->
            config.SearchEnginePrefixes
            |> Map.toSeq
            |> Seq.choose (fun (k, v) ->
                match searchEngines.TryGetValue k with
                | false, _ -> None
                | true, se -> Some (v, se)
            )
            |> dict
            |> searchEngineFromPrefix.OnNext
        )
        |> ignore

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

    member _.HideCommand = hideCommand
    member _.EmptyTextBoxCommand = emptyTextBoxCommand
    member _.ResetSingleSearchEngineMode() =
        match singleSearchEngineMode.Value with
        | None -> ()
        | Some _ -> singleSearchEngineMode.OnNext None

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
