namespace Starter.FileSearchEngine

open System
open System.Diagnostics
open System.Threading.Tasks
open R3
open Starter.SearchEngine

type FileSearchResult =
    { Path: string }

    interface ISearchResult with
        member this.Id = this.Path
        member this.Name = this.Path
        member this.Description = String.Empty
        member this.Icon = StarterIconSource.Empty
        member this.Keywords = null
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = null
        member this.ContextMenuLoader = null

type FileSearchEngine() as this =
    let activators = lazy [| DefaultSearchEngineActivator(this) :> ISearchEngineActivator |]

    interface IDynamicSearchEngine with
        member this.Id = nameof FileSearchEngine
        member this.Name = "Files"
        member this.Icon = StarterIconSource.Empty // TODO: Add icon
        member this.Activators = activators.Value
        member this.BufferResults = true
        member this.ResultsPriority = ResultPriority.Search

        member this.Search(query, cancellationToken, activator) =
            if isNull activator then
                Array.empty, Observable.Empty()
            else
                let results = new Subject<_>()
                let proc =
                    ProcessStartInfo(
                        "locate",
                        query,
                        RedirectStandardOutput = true
                    )
                    |> Process.Start

                Task.Run<unit>(fun () -> task {
                    try
                        while not cancellationToken.IsCancellationRequested &&
                              not proc.StandardOutput.EndOfStream do
                            let! line = proc.StandardOutput.ReadLineAsync()
                            { Path = line }
                            :> ISearchResult
                            |> Array.singleton
                            :> _ seq
                            |> results.OnNext
                    with
                    | :? ObjectDisposedException -> ()
                    | exn -> raise exn
                }) |> ignore

                cancellationToken.Register(fun () ->
                    proc.Dispose()
                    results.Dispose()
                ) |> ignore

                Array.empty, results

        member this.SearchResultSelected(selectedSearchResult) =
            ()
            // ProcessStartInfo(
            //     FileName = "hello",
            //     UseShellExecute = true
            // )
            // |> Process.Start
            // |> function null -> () | d -> d.Dispose()

        member this.add_Changed _ = ()
        member this.remove_Changed _ = ()

type Factory(pluginDirectory) =
    inherit SearchEngineFactory(pluginDirectory)

    override this.LoadSearchEngineIds() = [| nameof FileSearchEngine |]
    override this.LoadSearchEngine(searchEngineId, pluginConfigDirectory, logger, clipboard) =
        FileSearchEngine(),
        null
