namespace Starter.ViewModels

open Starter.Features
open Starter.SearchEngine
open Starter.Features.InternalSearchEngines

open System
open System.Reactive.Linq
open System.Threading
open System.Windows.Input
open System.Collections.Generic
open System.Threading.Tasks

open Elmish
open ReactiveUI
open ReactiveElmish
open ReactiveElmish.Avalonia

module private Constants =
    let [<Literal>] ScoresFile = "result.scores"
    let [<Literal>] ScoresMaxAging = 10_000

[<AutoOpen>]
module private Types =
    [<RequireQualifiedAccess>]
    type Msg =
        | ScoresLoaded of ScoresSaver.Scores
        | ConfigChanged of Config.Configuration

        | TextChanged of string
        | ResultLoaded of SearchResultViewModel array
        | Validate of SearchResultViewModel

    type Model =
        { Text: string
          Scores: ScoresSaver.Scores option
          Config: Config.Configuration
          Results: SearchResultViewModel array
          SearchEngines: IDictionary<string, SearchEngineBase>
          SearchCTS: CancellationTokenSource }

module private Cmds =
    let subscribeToConfigChanges (settingsSE: SettingsSearchEngine) =
        Cmd.ofEffect (fun dispatch ->
            settingsSE.Configuration
            |> Observable.subscribe (fun config ->
                config
                |> Msg.ConfigChanged
                |> dispatch

                config
                |> Config.saveConfig
                |> ignore
            )
            |> ignore

        )

    let computeResults model =
        Cmd.ofEffect (fun dispatch ->
            let ct = model.SearchCTS.Token
            for kv in model.SearchEngines do
                let searchEngine = kv.Value
                try
                    let obs = searchEngine.Search(model.Text, ct)
                    let sub = obs |> Observable.subscribe (fun results ->
                        results
                        |> Seq.map (fun result ->
                            SearchResultViewModel(
                                searchEngine.Id,
                                searchEngine.DisplayName,
                                result
                            )
                        )
                        |> Seq.toArray
                        |> Msg.ResultLoaded
                        |> dispatch
                    )

                    ct.Register(fun _ -> sub.Dispose()) |> ignore
                with e -> printfn "%s" e.Message
        )

    let loadScores () =
        Cmd.ofEffect (fun dispatch ->
            task {
                let! scores = Constants.ScoresFile |> ScoresSaver.readFromFile
                scores |> Msg.ScoresLoaded |> dispatch
            } |> ignore
        )

    let private checkScoresMaxAging maxAge (scores: ScoresSaver.Scores) =
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

    let saveScores (scores: ScoresSaver.Scores) =
        Cmd.ofEffect (fun _ ->
            checkScoresMaxAging Constants.ScoresMaxAging scores

            scores
            |> ScoresSaver.writeToFile Constants.ScoresFile
            |> ignore
        )

    let validateResult (model: Model) (result: SearchResultViewModel) =
        Cmd.ofEffect (fun _ ->
            let se = model.SearchEngines[result.SearchEngineId]

            Action(fun () -> se.SearchResultSelected result.Result)
            |> Task.Run
            |> ignore
        )

module private State =
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


    let init config () =
        let settingsSearchEngine = SettingsSearchEngine config

        let searchEngines =
            [| System.IO.Path.Combine(
                __SOURCE_DIRECTORY__,
                "../Plugins/Starter.ApplicationSearchEngine/bin/Debug/net9.0/Starter.ApplicationSearchEngine.dll"
            ) |]
            |> Array.collect SearchEngineLoading.loadSearchEngines
            |> Array.append [| settingsSearchEngine |]
            |> Array.map (fun searchEngine -> searchEngine.Id, searchEngine)
            |> dict

        { Text = "Hello world !"
          Scores = None
          Config = config
          Results = Array.empty
          SearchEngines = searchEngines
          SearchCTS = new CancellationTokenSource() },
        Cmd.batch [
            Cmds.subscribeToConfigChanges settingsSearchEngine
            Cmds.loadScores()
        ]

    let update msg model =
        match msg with
        | Msg.ScoresLoaded scores ->
            { model with Scores = Some scores },
            Cmd.ofMsg (Msg.ResultLoaded Array.empty) // Re-sort results

        | Msg.ConfigChanged newConfig ->
            printfn "Config changed: %A" newConfig
            { model with Config = newConfig }, Cmd.none

        | Msg.TextChanged text ->
            model.SearchCTS.Cancel() // Cancel current query

            let newModel =
                { model with
                    Text = text
                    SearchCTS = new CancellationTokenSource()
                    Results = Array.empty }
            newModel, Cmds.computeResults newModel

        | Msg.ResultLoaded results ->
            let newResults =
                match model.Scores with
                | None -> results |> Array.append model.Results
                | Some scores ->
                    results
                    |> Array.append model.Results
                    |> Array.sortBy (getResultSortIdx scores)

            { model with Results = newResults },
            Cmd.none

        | Msg.Validate result ->
            // Increase app score
            model.Scores |> Option.iter (fun scores ->
                increaseAppScore scores result.Result.Id
            )

            model, Cmd.batch [
                model.Scores
                |> Option.map Cmds.saveScores
                |> Option.defaultValue Cmd.none

                Cmds.validateResult model result
            ]

type MainWindowViewModel() =
    inherit ReactiveElmishViewModel()

    let config =
        match Config.getConfig() with
        | Error _ -> failwith "Error"
        | Ok r ->
            match r with
            | null -> failwith "Error"
            | r -> r

    let local =
        Program.mkAvaloniaProgram
            (State.init config)
            State.update
        |> Program.mkStore

    do local.Observable
        |> Observable.subscribe (printfn "%A")
        |> ignore

    let hideCommand = ReactiveCommand.Create(fun () -> ())

    member _.HideCommand = hideCommand
    member _.ValidateCommand(searchResult: SearchResultViewModel | null) =
        match searchResult with
        | null -> ()
        | searchResult ->
            searchResult
            |> Msg.Validate
            |> local.Dispatch

        (hideCommand :> ICommand).Execute()

    member _.BaseConfig = config
    member _.Config = local.Observable |> Observable.map _.Config

    member this.SearchResults = this.Bind(local, _.Results)
    member _.Text
        with get () = local.Model.Text
        and set v = v |> Msg.TextChanged |> local.Dispatch

    static member DesignVM = new MainWindowViewModel()
