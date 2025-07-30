namespace Starter.ViewModels

open Starter.Features
open Starter.Features.Config
open Starter.Features.InternalSearchEngines
open Starter.Features.Logging
open Starter.Features.ResultScores
open Starter.Features.CustomCollections
open Starter.SearchEngine

open System.Collections.Generic
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Windows.Input

open Fusil
open Fusil.TextNormalization
open Avalonia.Threading
open ReactiveUI
open R3

type SearchEngines =
    { Statics: List<StaticSearchEngine>
      Dynamics: List<DynamicSearchEngine>
      Dict: BehaviorSubject<Dictionary<string, SearchEngine>> }

    static member create () =
        { Statics = List()
          Dynamics = List()
          Dict = new BehaviorSubject<_>(Dictionary()) }

    static member private addSearchEngineCore (se: SearchEngine) searchEngines =
        searchEngines.Dict.Value.Add(se.Id, se)

    static member loadFromDirectories (directories: string array) (searchEngines: SearchEngines) =
        for dir in directories do
            let statics, dynamics = dir |> SearchEngineLoading.loadSearchEngineFromDirectory
            statics |> searchEngines.Statics.AddRange
            dynamics |> searchEngines.Dynamics.AddRange

            statics |> Seq.iter (fun se -> searchEngines.Dict.Value.Add(se.Id, se))
            dynamics |> Seq.iter (fun se -> searchEngines.Dict.Value.Add(se.Id, se))

        searchEngines.Dict.Value |> searchEngines.Dict.OnNext
        searchEngines

    static member addSearchEngine (se: SearchEngine) (searchEngines: SearchEngines) =
        match se with
        | :? StaticSearchEngine as se -> searchEngines.Statics.Add se
        | :? DynamicSearchEngine as se -> searchEngines.Dynamics.Add se
        | _ -> ()

        searchEngines.Dict.Value.Add(se.Id, se)
        searchEngines.Dict.Value |> searchEngines.Dict.OnNext
        searchEngines

type MainWindowViewModel(baseConfig: Configuration, resultScoreDb: ResultScores.ScoreDb) =
    // ---
    let config = new BehaviorSubject<_>(baseConfig)
    let fusilSlab = Slab.createDefault()

    // --- Search engines store
    let searchEngines = SearchEngines.create()
    let activatorStore = ActivatorStore(config)

    // --- Commands (to interact with view)
    let hideCommand = ReactiveCommand.Create(fun () -> ())
    let clearTextBoxCommand = new Subject<int>()

    // --- State
    /// Pre-loaded static results
    let staticSearchResults = new BehaviorSubject<SearchResultViewModel array>(Array.empty)
    /// Results matching to the current query
    let searchResults = ObservableList<SearchResultViewModel>(100)
    let mutable text = "starter"
    let mutable searchCts = new CancellationTokenSource()
    let mutable currentActivator = new BehaviorSubject<ISearchEngineActivator option>(None)

    let setStaticResultForSearchEngine (se: StaticSearchEngine) results =
        Dispatcher.UIThread.Post(fun () ->
            let othersResults =
                staticSearchResults.Value
                |> Array.filter (_.SearchEngineId >> (<>) se.Id)

            let newStaticResults =
                results
                |> Seq.map (SearchResultViewModel.create SearchResultKind.Static se)
                |> Seq.append othersResults
                |> Seq.toArray

            newStaticResults |> Array.Parallel.sortInPlaceBy (SearchResultViewModel.mapForComparison resultScoreDb)
            staticSearchResults.OnNext newStaticResults
            logger.Information $"{se.Name} results loaded"
        )

    let subscribeToDynamicSearchEngine activator (ct: CancellationToken) query (se: DynamicSearchEngine) =
        try
            let struct (instantResults, obs) = se.Search(query, searchCts.Token, activator |> Option.toObj)

            obs.ObserveOnUIThreadDispatcher()
               .Subscribe(fun results ->
                results
                |> Seq.map (SearchResultViewModel.create SearchResultKind.Dynamic se)
                |> searchResults.AddRange

                searchResults.Sort(SearchResultViewModel.mapForComparison resultScoreDb)
                searchResults.NotifyChanges()
            )
            |> disposeOnCancelled ct

            let instantSrPos =
                match se.ImportantResults with
                | true -> SearchResultKind.DynamicUnique
                | false -> SearchResultKind.DynamicInstant

            instantResults
            |> Seq.map (SearchResultViewModel.create instantSrPos se)
            |> searchResults.AddRange
        with e ->
            logger.Error(e, $"Failed to get results from dynamic search engine: {se.Name}")

    let onTextChanged (newText: string) =
        // Cancel previous search
        searchCts.Cancel()
        searchCts <- new CancellationTokenSource()

        match activatorStore.GetActivatorFromPrefix newText with
        | Some activator ->
            // Update the current activator
            activator
            |> Some
            |> currentActivator.OnNext
            clearTextBoxCommand.OnNext(newText.Length)

            // Clear the results (as the textbox is empty)
            searchResults.Clear()
            searchResults.NotifyChanges()

        | None ->
            let query =
                newText
                |> String.normalize
                |> Array.map System.Text.Rune.ToLowerInvariant

            let fuzzyMatch = Fusil.fuzzyMatch false true true fusilSlab query

            currentActivator.Subscribe(fun activator ->
                let targetSearchEngine =
                    match activator with
                    | None -> Choice1Of4 () // No activator -> show all static results
                    | Some activator ->
                        match searchEngines.Dict.Value.TryGetValue(activator.Id) with
                        | true, (:? DynamicSearchEngine as se) -> Choice2Of4 struct (activator, se) // Activator from dynamic search engine -> show only its results
                        | true, se -> Choice3Of4 se // Activator from static search engine -> show only its results
                        | false, _ -> Choice4Of4 () // Activator from unknown search engine -> logging an error

                match targetSearchEngine with
                | Choice2Of4 (activator, se) -> // Only dynamic se
                    searchResults.Clear()
                    se |> subscribeToDynamicSearchEngine (Some activator) searchCts.Token newText
                    searchResults.NotifyChanges()

                | Choice3Of4 se -> // Only static se
                    staticSearchResults.Subscribe(fun staticResults ->
                        searchResults.Clear()

                        staticResults
                        |> Array.filter (fun result ->
                            if result.SearchEngineId = se.Id then
                                match result.Name |> fuzzyMatch with
                                | Some fusilResult when fusilResult.Score > 0s ->
                                    result.AccentuationMap <- fusilResult.MatchingPositions
                                    true
                                | _ -> false
                            else
                                false
                        )
                        |> searchResults.AddRange

                        searchResults.Sort(SearchResultViewModel.mapForComparison resultScoreDb)
                        searchResults.NotifyChanges()
                    ) |> disposeOnCancelled searchCts.Token

                | Choice1Of4 () -> // All results
                    staticSearchResults.Subscribe(fun staticResults ->
                        searchResults.Clear()

                        searchEngines.Dynamics |> Seq.iter (subscribeToDynamicSearchEngine None searchCts.Token newText)

                        staticResults
                        |> Array.filter (fun result ->
                            match result.Name |> fuzzyMatch with
                            | Some fusilResult when fusilResult.Score > 0s ->
                                result.AccentuationMap <- fusilResult.MatchingPositions
                                true
                            | _ -> false
                        )
                        |> searchResults.AddRange

                        searchResults.Sort(SearchResultViewModel.mapForComparison resultScoreDb)
                        searchResults.NotifyChanges()
                    ) |> disposeOnCancelled searchCts.Token

                | Choice4Of4 () -> logger.Error $"Failed to find search engine associated with activator: {activator |> Option.map _.Id}"
            ) |> disposeOnCancelled searchCts.Token

    // ReSharper disable once FSharpRedundantDotInIndexer
    let validateResult (result: SearchResultViewModel) =
        task {
            // Send the result to the search engine
            let se = searchEngines.Dict.Value[result.SearchEngineId]
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
        // Load search engines
        let pluginDirectories =
            #if DEBUG
            [| Path.Combine(__SOURCE_DIRECTORY__, "../../Starter.UrlSearchEngine/bin/Debug/net9.0/")
               Path.Combine(__SOURCE_DIRECTORY__, "../../Starter.ApplicationSearchEngine/bin/Debug/net9.0-windows10.0.19041.0/")
               Path.Combine(__SOURCE_DIRECTORY__, "../../Starter.WebSearchEngine/bin/Debug/net9.0/") |]
            #else
            Constants.PluginsDirectory |> Directory.GetDirectories
            #endif

        let settingsSearchEngine = SettingsSearchEngine(config.Value, searchEngines.Dict)

        logger.Debug "Loading plugin assemblies"
        searchEngines
        |> SearchEngines.loadFromDirectories pluginDirectories
        |> SearchEngines.addSearchEngine settingsSearchEngine
        |> ignore
        logger.Debug "Assemblies loaded"

        // TODO: First use of search engines is slow, but RuntimeHelpers.PrepareMethod doesn't work (HELP wanted)
        // Precompile search engine methods
        // dynamicSearchEngines |> Seq.iter SearchEngineLoading.prepareSearchEngine

        // Sync settings search engine (and the settings page) with config
        // Save config to a file when it changes
        settingsSearchEngine.Configuration.Subscribe(fun newConfig ->
            config.OnNext newConfig
            newConfig |> Configuration.save Constants.ConfigFile |> ignore
        )
        |> ignore

        // Load static results
        for se in searchEngines.Statics do
            Task.Run<unit>(fun () -> task {
                try
                    let! results, resultsChanged = se.LoadResults()
                    results |> setStaticResultForSearchEngine se
                    resultsChanged.Subscribe(setStaticResultForSearchEngine se) |> ignore
                with e -> logger.Error $"{se.Name} failed to load results:\n{e.Message}"
            }) |> ignore

        for kv in searchEngines.Dict.Value do
            activatorStore.AddSearchEngineActivators(kv.Value)

    member _.HideCommand = hideCommand
    member _.ClearTextBoxCommand = clearTextBoxCommand
    member _.ResetActivator() =
        match currentActivator.Value with
        | None -> ()
        | Some _ -> currentActivator.OnNext None

    member _.ValidateResult(searchResult: SearchResultViewModel | null) =
        match searchResult with
        | null -> ()
        | searchResult ->
            searchResult |> validateResult |> ignore
            logger.Debug $"{searchResult.Name} selected"

        (hideCommand :> ICommand).Execute()

    member _.Config = config

    member this.SearchResults = searchResults
    member _.Text
        with get () = text
        and set v = text <- v; onTextChanged v

    member this.CurrentActivator = currentActivator

    #if DEBUG
    static member DesignVM = MainWindowViewModel(Configuration.Default, Array.empty |> dict)
    #endif
