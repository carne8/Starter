namespace Starter.ViewModels

open ReactiveUI
open ReactiveElmish
open ReactiveElmish.Avalonia
open Starter.SearchEngine
open System.Threading
open System.Windows.Input

[<AutoOpen>]
module private Types =
    [<RequireQualifiedAccess>]
    type Msg =
        | TextChanged of string
        | ClearResults
        | ResultLoaded of searchEngineName: string * ISearchResult array
        | Validate of string * ISearchResult

    type Model =
        { Text: string
          Results: SearchResultViewModel array
          SearchEngines: ISearchEngine list
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
                        let obs = se.Search(ct, model.Text)
                        let sub = obs |> Observable.subscribe (fun sr ->
                            (se.Name, sr |> Seq.toArray)
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
                |> List.find (fun x -> x.Name = seName)

            Task.Run(System.Action(fun () -> se.SearchResultSelected sr)) |> ignore
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

module SearchEngineLoader =
    open System
    open System.IO
    open System.Reflection
    open System.Runtime.Loader

    type SearchEngineLoadContext(dllPath) =
        inherit AssemblyLoadContext()

        let resolver = AssemblyDependencyResolver dllPath

        let isSharedAssembly (assemblyName: AssemblyName) =
            match assemblyName.Name with
            | null -> false
            | assemblyName ->
                Constants.SharedAssemblies |> Seq.contains assemblyName

        override this.Load(assemblyName: AssemblyName): Assembly | null =
            match assemblyName |> isSharedAssembly with
            | true -> Assembly.Load assemblyName
            | false ->
                let assemblyPath = resolver.ResolveAssemblyToPath assemblyName
                match assemblyPath with
                | null -> null
                | assemblyPath -> this.LoadFromAssemblyPath assemblyPath

        override this.LoadUnmanagedDll(unmanagedDllName) =
            let libraryPath = resolver.ResolveUnmanagedDllToPath unmanagedDllName
            match libraryPath with
            | null -> IntPtr.Zero
            | libraryPath -> this.LoadUnmanagedDllFromPath libraryPath

    let private loadSearchEngineAssembly relativePath =
        let root =
            AppContext.BaseDirectory
            |> Path.GetDirectoryName
            |> Path.GetDirectoryName
            |> Path.GetDirectoryName
            |> Path.GetDirectoryName
        let path = Path.Combine(root, relativePath) |> Path.GetFullPath

        let loadContext = SearchEngineLoadContext path
        let u =
            path
            |> AssemblyName.GetAssemblyName
            |> loadContext.LoadFromAssemblyName

        u

    let private loadAssemblySearchEngines (assembly: Assembly) =
        let interfaceType = typeof<ISearchEngine>

        assembly.GetTypes()
        |> Array.choose (fun type' ->
            if interfaceType.IsAssignableFrom type' then
                Activator.CreateInstance(type')
                :?> ISearchEngine
                |> Some
            else
                None
        )

    let loadSearchEngines libPath =
        libPath
        |> loadSearchEngineAssembly
        |> loadAssemblySearchEngines

module private State =
    open Elmish

    let init () =
        let searchEngines =
            [| "Plugins/Starter.ApplicationSearchEngine/bin/Debug/net9.0/Starter.ApplicationSearchEngine.dll" |]
            |> Array.collect SearchEngineLoader.loadSearchEngines
            |> Array.toList

        { Text = "Hello"
          Results = Array.empty
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
            let newResultList =
                Array.append
                    model.Results
                    (results |> Array.map (fun r -> SearchResultViewModel(seName, r)))

            { model with Results = newResultList }, Cmd.none

        | Msg.Validate (se, sr) -> model, Cmds.validateResult model se sr

type MainWindowViewModel() =
    inherit ReactiveElmishViewModel()

    let local =
        Program.mkAvaloniaProgram
            State.init
            State.update
        |> Program.mkStore

    let hideCommand = ReactiveCommand.Create(fun () -> ())
    let focusDownCommand = ReactiveCommand.Create(fun () -> ())
    let focusUpCommand = ReactiveCommand.Create(fun () -> ())

    member _.HideCommand = hideCommand
    member _.FocusDownCommand = focusDownCommand
    member _.FocusUpCommand = focusUpCommand
    member _.ValidateCommand(searchResult: SearchResultViewModel | null) =
        match searchResult with
        | null -> ()
        | searchResult ->
            (searchResult.SearchEngineName,
             searchResult.Result)
            |> Msg.Validate
            |> local.Dispatch

        (hideCommand :> ICommand).Execute()

    member this.SearchResults = this.Bind(local, _.Results)
    member _.Text
        with get () = local.Model.Text
        and set v = v |> Msg.TextChanged |> local.Dispatch
