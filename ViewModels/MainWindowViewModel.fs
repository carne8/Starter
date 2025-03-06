namespace Starter.ViewModels

open Starter.Features
open Starter.SearchEngine

open System.Threading
open System.Windows.Input

open ReactiveUI
open ReactiveElmish
open ReactiveElmish.Avalonia

[<AutoOpen>]
module private Types =
    [<RequireQualifiedAccess>]
    type Msg =
        | TextChanged of string
        | ClearResults
        | ResultLoaded of searchEngineId: string * ISearchResult array
        | Validate of string * ISearchResult

    type Model =
        { Text: string
          Results: SearchResultViewModel array
          SearchEngines: SearchEngine array
          SearchCTS: CancellationTokenSource }

module private Cmds =
    open Elmish
    open System.Threading.Tasks

    let computeResults model =
        Cmd.ofEffect (fun dispatch ->
            Msg.ClearResults |> dispatch
            let ct = model.SearchCTS.Token

            Task.Run<unit>(fun () -> task {
                for se in model.SearchEngines do
                    try
                        let obs = se.Search(model.Text, ct)
                        let sub = obs |> Observable.subscribe (fun sr ->
                            (se.Id, sr |> Seq.toArray)
                            |> Msg.ResultLoaded
                            |> dispatch
                        )

                        ct.Register(fun _ -> sub.Dispose()) |> ignore
                    with e -> printfn "%s" e.Message
            }) |> ignore
        )

    let validateResult (model: Model) (seName: string) sr =
        Cmd.ofEffect (fun _ ->
            let se =
                model.SearchEngines
                |> Array.find (fun x -> x.Id = seName)

            System.Action(fun () -> se.SearchResultSelected sr)
            |> Task.Run
            |> ignore
        )
//     open System.Data.OleDb
//     open System.Threading.Tasks
//
//     let t =
//         task {
//             let query = $"""SELECT TOP 10 System.ItemName, System.ItemPathDisplay FROM SystemIndex WHERE
//                 System.ItemPathDisplay LIKE 'D:\%%'
//                 AND System.ItemName LIKE '%%{model.Text}%%'
//                 ORDER BY System.DateModified DESC
//             """
//                 // AND System.ItemPathDisplay NOT LIKE '%%\.%%'
//             use connection = new OleDbConnection("Provider=Search.CollatorDSO.1;Extended Properties='Application=Windows';")
//             do! connection.OpenAsync()
//
//             use command = new OleDbCommand(query, connection)
//             use! reader = command.ExecuteReaderAsync()
//
//             while! reader.ReadAsync() do
//                   // Path = reader["System.ItemPathDisplay"] }
//                 { Name = reader["System.ItemPathDisplay"] :?> string }
//                 |> Msg.ResultLoaded
//                 |> dispatch
//         } :> Task
//
//     open Elmish

module private State =
    open Elmish

    let init () =
        let searchEngines =
            [| System.IO.Path.Combine(
                __SOURCE_DIRECTORY__,
                "../Plugins/Starter.ApplicationSearchEngine/bin/Debug/net9.0/Starter.ApplicationSearchEngine.dll"
            ) |]
            |> Array.collect SearchEngineLoading.loadSearchEngines

        { Text = "Hello world !"
          Results = Array.empty
          // Results = [|
          //     for _ in 0..100 do
          //       SearchResultViewModel.DesignVM
          // |]
          SearchEngines = searchEngines
          SearchCTS = new CancellationTokenSource() },
        Cmd.none

    let update msg model =
        match msg with
        | Msg.TextChanged text ->
            model.SearchCTS.Cancel() // Cancel current query

            let newModel =
                { model with
                    Text = text
                    SearchCTS = new CancellationTokenSource() }
            newModel, Cmds.computeResults newModel

        | Msg.ClearResults -> { model with Results = Array.empty }, Cmd.none
        | Msg.ResultLoaded (seName, results) ->
            let searchEngine = model.SearchEngines |> Array.find (_.Id >> (=) seName)

            let newResultArray =
                Array.append
                    model.Results
                    (results |> Array.map (fun result ->
                        SearchResultViewModel(
                            searchEngine.Id,
                            searchEngine.DisplayName,
                            result
                        )
                    ))

            { model with Results = newResultArray }, Cmd.none

        | Msg.Validate (seId, sr) -> model, Cmds.validateResult model seId sr

type MainWindowViewModel() =
    inherit ReactiveElmishViewModel()

    let local =
        Program.mkAvaloniaProgram
            State.init
            State.update
        |> Program.mkStore

    let hideCommand = ReactiveCommand.Create(fun () -> ())

    member _.HideCommand = hideCommand
    member _.ValidateCommand(searchResult: SearchResultViewModel | null) =
        match searchResult with
        | null -> ()
        | searchResult ->
            (searchResult.SearchEngineId,
             searchResult.Result)
            |> Msg.Validate
            |> local.Dispatch

        (hideCommand :> ICommand).Execute()

    member this.SearchResults = this.Bind(local, _.Results)
    member _.Text
        with get () = local.Model.Text
        and set v = v |> Msg.TextChanged |> local.Dispatch

    static member DesignVM = new MainWindowViewModel()
