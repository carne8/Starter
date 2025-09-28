module Starter.ApplicationSearchEngine.Linux.AppsLoader

open System
open System.Collections.Concurrent
open System.IO
open System.Threading
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open R3
open Starter.ApplicationSearchEngine
open Starter.SearchEngine

let loadApplications (config: FolderConfiguration) : Task<ISearchResult seq> =
    Task.Run<ISearchResult seq>(fun () ->
        task {
            let apps = ConcurrentDictionary<_, _>()

            for folder in config.Folders do
                let files = Directory.EnumerateFiles(folder, "*.desktop")

                do! Parallel.ForEachAsync(
                    files,
                    Func<_, _, _>(fun path _ ->
                        match path |> FolderConfiguration.isFileExcluded config with
                        | true -> ValueTask.CompletedTask
                        | false ->
                            path
                            |> XDGDesktopFileParser.loadDesktopEntries
                            |> Task.map (Seq.iter (fun app ->
                                apps.TryAdd(path |> Path.GetFileName, app) |> ignore
                            ))
                            |> ValueTask
                    )
                )

            return apps.Values :> ISearchResult seq
        }
    )

let observeApplicationChanges (appList: ResizeArray<ISearchResult>) (config: FolderConfiguration) =
    let subject = new Subject<unit>()
    let semaphore = new SemaphoreSlim(1, 1)

    let replaceInList desktopFile =
        desktopFile
        |> XDGDesktopFileParser.loadDesktopEntries
        |> Task.bind (fun newEntries ->
            task {
                do! semaphore.WaitAsync()
                appList.RemoveAll(fun e -> e.Id.Contains desktopFile) |> ignore
                appList.AddRange newEntries
                subject.OnNext()
                semaphore.Release() |> ignore
            }
        )
        |> ignore

    let removeFromList (desktopFile: string) =
        task {
            do! semaphore.WaitAsync()
            appList.RemoveAll(fun e -> e.Id.Contains desktopFile) |> ignore
            subject.OnNext()
            semaphore.Release() |> ignore
        }
        |> ignore

    let watchers = config.Folders |> Array.map (fun folder ->
        let watcher = new FileSystemWatcher(folder)
        watcher.Filters.Add("*.desktop")
        watcher.NotifyFilter <-
            NotifyFilters.CreationTime
            ||| NotifyFilters.DirectoryName
            ||| NotifyFilters.FileName
            ||| NotifyFilters.LastWrite

        watcher.Created.Add(fun args -> replaceInList args.FullPath)
        watcher.Renamed.Add(fun args -> replaceInList args.FullPath)
        watcher.Deleted.Add(fun args -> removeFromList args.FullPath)

        watcher.EnableRaisingEvents <- true
        watcher
    )

    subject,
    { new IDisposable with
        member _.Dispose() =
            watchers |> Array.iter _.Dispose()
            subject.Dispose() }
