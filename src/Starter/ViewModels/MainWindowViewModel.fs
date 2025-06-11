namespace Starter.ViewModels

open Starter.Features
open Starter.Features.Config
open Starter.Features.InternalSearchEngines
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
      Dict: BehaviorSubject<Dictionary<string, ISearchEngine>> }

    static member create () =
        { Statics = List()
          Dynamics = List()
          Dict = new BehaviorSubject<_>(Dictionary()) }

    static member loadFromDirectories (directories: string array) (searchEngines: SearchEngines) =
        for dir in directories do
            let statics, dynamics = dir |> SearchEngineLoading.loadSearchEngineFromDirectory
            statics |> searchEngines.Statics.AddRange
            dynamics |> searchEngines.Dynamics.AddRange

            statics |> Seq.iter (fun s -> searchEngines.Dict.Value.Add(s.Id, s))
            dynamics |> Seq.iter (fun s -> searchEngines.Dict.Value.Add(s.Id, s))

        searchEngines.Dict.Value |> searchEngines.Dict.OnNext
        searchEngines

    static member addSearchEngine (se: ISearchEngine) (searchEngines: SearchEngines) =
        match se with
        | :? StaticSearchEngine as se -> searchEngines.Statics.Add se
        | :? DynamicSearchEngine as se -> searchEngines.Dynamics.Add se
        | _ -> ()

        searchEngines.Dict.Value.Add(se.Id, se)
        searchEngines.Dict.Value
        |> searchEngines.Dict.OnNext

        searchEngines


type MainWindowViewModel(baseConfig: Configuration, resultScoreDb: ResultScores.ScoreDb) =
    // ---
    let config = new BehaviorSubject<_>(baseConfig)
    let fusilSlab = Slab.createDefault()

    // --- Search engines store
    let searchEngines = SearchEngines.create()
    let searchEngineFromPrefix = new BehaviorSubject<(string * ISearchEngine) array>(Array.empty) // Bound to searchEngines in `do`

    // --- Commands (to interact with view)
    let hideCommand = ReactiveCommand.Create(fun () -> ())
    let clearTextBoxCommand = new Subject<int>()

    // --- State
    /// Static results pre-loaded
    let staticSearchResults = new BehaviorSubject<SearchResultViewModel array>(Array.empty)
    /// Results matching to the current query
    let searchResults = ObservableList<SearchResultViewModel>(100)
    let mutable text = "starter"
    let mutable searchCts = new CancellationTokenSource()
    let mutable singleSearchEngineMode = new BehaviorSubject<SingleSearchEngineViewModel option>(None)

    let subscribeToDynamicSearchEngine (ct: CancellationToken) query (se: DynamicSearchEngine) =
        try
            let struct (instantResults, obs) = se.Search(query, searchCts.Token)

            let srPos =
                match se.ImportantResults with
                | true -> SearchResultPosition.Important
                | false -> SearchResultPosition.Low

            obs.ObserveOnUIThreadDispatcher()
               .Subscribe(fun results ->
                results
                |> Array.map (SearchResultViewModel.create srPos se)
                |> searchResults.AddRange

                searchResults.Sort(SearchResultViewModel.mapForComparison resultScoreDb)
                searchResults.NotifyChanges()
            )
            |> disposeOnCancelled ct

            instantResults
            |> Array.map (SearchResultViewModel.create srPos se)
            |> searchResults.AddRange
        with _ -> () // TODO: Add error / logs

    let onTextChanged (newText: string) =
        // Cancel previous search
        searchCts.Cancel()
        searchCts <- new CancellationTokenSource()

        match searchEngineFromPrefix.Value |> Array.tryFind (fst >> newText.StartsWith) with
        | Some (prefix, se) ->
            // Update the single search-engine-mode
            se
            |> SingleSearchEngineViewModel.create
            |> Some
            |> singleSearchEngineMode.OnNext
            clearTextBoxCommand.OnNext(prefix.Length)

            // Clear the results (as the textbox is empty)
            searchResults.Clear()
            searchResults.NotifyChanges()

        | None ->
            let query =
                newText
                |> String.normalize
                |> Array.map System.Text.Rune.ToLowerInvariant

            let fuzzyMatch = Fusil.fuzzyMatch false true true fusilSlab query

            singleSearchEngineMode.Subscribe(fun singleSe ->
                match singleSe with
                | Some { SearchEngine = :? DynamicSearchEngine as singleSe } ->
                    searchResults.Clear()
                    singleSe |> subscribeToDynamicSearchEngine searchCts.Token newText
                    searchResults.NotifyChanges()
                | _ ->
                    let isSearchEngineActivated seId =
                        match singleSe with
                        | None -> true
                        | Some se -> se.SearchEngine.Id = seId

                    staticSearchResults.Subscribe(fun staticResults ->
                        searchResults.Clear()

                        searchEngines.Dynamics |> Seq.iter (fun se ->
                            if se.Id |> isSearchEngineActivated then
                                se |> subscribeToDynamicSearchEngine searchCts.Token newText
                        )

                        let filteredResults =
                            staticResults |> Array.filter (fun srVm ->
                                if srVm.SearchEngineId |> isSearchEngineActivated |> not then
                                    false
                                else
                                    match srVm.Name |> fuzzyMatch with
                                    | Some fusilResult when fusilResult.Score > 0s ->
                                        srVm.AccentuationMap <- fusilResult.MatchingPositions
                                        true
                                    | _ -> false
                            )

                        filteredResults |> searchResults.AddRange
                        searchResults.Sort(SearchResultViewModel.mapForComparison resultScoreDb)
                        searchResults.NotifyChanges()
                    )
                    |> disposeOnCancelled searchCts.Token
            )
            |> disposeOnCancelled searchCts.Token

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
               Path.Combine(__SOURCE_DIRECTORY__, "../../Starter.ApplicationSearchEngine/bin/Debug/net9.0/")
               Path.Combine(__SOURCE_DIRECTORY__, "../../Starter.WebSearchEngine/bin/Debug/net9.0/") |]
            #else
            Constants.PluginsDirectory |> Directory.GetDirectories
            #endif

        let settingsSearchEngine = SettingsSearchEngine(config.Value, searchEngines.Dict)

        searchEngines
        |> SearchEngines.loadFromDirectories pluginDirectories
        |> SearchEngines.addSearchEngine settingsSearchEngine
        |> ignore

        // TODO: First use of search engines is slow, but RuntimeHelpers.PrepareMethod doesn't work (HELP wanted)
        // Precompile search engine methods
        // dynamicSearchEngines |> Seq.iter SearchEngineLoading.prepareSearchEngine

        // Sync searchEngineFromPrefix with config
        config.Subscribe (fun config ->
            config.SearchEnginePrefixes
            |> Map.toSeq
            |> Seq.choose (fun (k, v) ->
                match searchEngines.Dict.Value.TryGetValue k with
                | false, _ -> None
                | true, se -> Some (v, se)
            )
            |> Seq.toArray
            |> searchEngineFromPrefix.OnNext
        )
        |> ignore

        // Sync config changes with the settings search engine (and the settings page)
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
                    let! results = se.LoadResults() // TODO: Handle errors

                    // SearchResultViewModel instantiation must happen on UI thread in order to create span controls
                    // Also staticSearchResults.OnNext must happen on UI thread
                    Dispatcher.UIThread.Post(fun () ->
                        let newStaticResults =
                            results
                            |> Array.map (SearchResultViewModel.create SearchResultPosition.Normal se)
                            |> Array.append staticSearchResults.Value

                        newStaticResults |> Array.Parallel.sortInPlaceBy (SearchResultViewModel.mapForComparison resultScoreDb)
                        staticSearchResults.OnNext newStaticResults
                        printfn "%s results loaded" se.Name
                    )
                with e -> printfn "%A" e
            }) |> ignore

    member _.HideCommand = hideCommand
    member _.ClearTextBoxCommand = clearTextBoxCommand
    member _.ResetSingleSearchEngineMode() =
        match singleSearchEngineMode.Value with
        | None -> ()
        | Some _ -> singleSearchEngineMode.OnNext None

    member _.ValidateResult(searchResult: SearchResultViewModel | null) =
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
    static member DesignVM = MainWindowViewModel(Configuration.Default, Array.empty |> dict)
    #endif
