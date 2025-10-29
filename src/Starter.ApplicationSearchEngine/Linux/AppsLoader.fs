module Starter.ApplicationSearchEngine.Linux.AppsLoader

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open R3
open Starter.ApplicationSearchEngine
open Starter.SearchEngine

let loadApplications (config: FolderConfiguration) : Task<ISearchResult seq> =
    Task.Run<ISearchResult seq>(fun () ->
        config.Folders
        |> Seq.choose (fun folder ->
            Path.Combine(folder, "applications")
            |> Some
            |> Option.filter (FolderConfiguration.isFileExcluded config >> not)
            |> Option.filter Path.Exists
        )
        |> Seq.collect (fun folder -> Directory.EnumerateFiles(folder, "*.desktop", SearchOption.AllDirectories))
        |> Seq.distinctBy (fun desktopFile ->
            // Take only the first occurrence of each Desktop File ID
            // https://specifications.freedesktop.org/desktop-entry-spec/latest/file-naming.html#desktop-file-id 
            let i = desktopFile.IndexOf "applications"
            desktopFile.Remove(0, i + "applications".Length)
        )
        |> Seq.map (XDGDesktopFileParser.loadDesktopEntries config)
        |> Task.WhenAll
        |> Task.map Seq.concat
    )

let observeApplicationChanges (appList: ResizeArray<ISearchResult>) (config: FolderConfiguration) =
    let subject = new Subject<unit>()
    let semaphore = new SemaphoreSlim(1, 1)

    let replaceInList desktopFile =
        desktopFile
        |> XDGDesktopFileParser.loadDesktopEntries config
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
