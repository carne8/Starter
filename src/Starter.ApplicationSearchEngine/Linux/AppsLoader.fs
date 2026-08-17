module Starter.ApplicationSearchEngine.Linux.AppsLoader

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent

open FsToolkit.ErrorHandling
open R3
open Starter.SearchEngine
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Logger

let loadApplication (iconLoader: IconLoader.IconLoader) entry =
    task {
        let! icon = iconLoader.LoadIcon entry

        return
            { Id = $"application:{entry.DesktopFilePath}:{entry.Name}"
              DesktopFile = entry.DesktopFilePath
              Name = entry.Name
              Icon = icon
              Description = entry.Comment |> ValueOption.defaultValue "Applications" // TODO: I18n
              Keywords = entry.AdditionalSearchKeywords
              Exec =
                entry
                |> XDGDesktopFileParser.parseExec
                |> ValueOption.defaultWith (fun () ->
                    logger.Warning $"{entry.DesktopFilePath} does not provide a valid Exec string"
                    String.Empty
                )
              WorkingDirectory = entry.WorkingDirectory }
    }


let loadApplications iconLoader (config: FolderConfiguration) : Task<ISearchResult seq> =
    Task.Run<ISearchResult seq>(fun () -> task {
        let sw = Diagnostics.Stopwatch()
        sw.Start()

        // Find .desktop files
        let desktopFiles =
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

        let apps = ConcurrentBag()

        do! Parallel.ForEachAsync(desktopFiles, Func<_, _, _>(fun desktopFile _ct ->
            task {
                let! desktopEntries = XDGDesktopFileParser.loadDesktopEntries desktopFile
                for entry in desktopEntries do
                    let! app = loadApplication iconLoader entry

                    app
                    :> ISearchResult
                    |> apps.Add
            } |> ValueTask
        ))

        sw.Stop()
        logger.Debug $"Loaded apps: {sw.ElapsedMilliseconds}ms"

        return apps :> ISearchResult seq
    })

let observeApplicationChanges iconLoader (appList: ResizeArray<ISearchResult>) (config: FolderConfiguration) =
    let subject = new Subject<unit>()
    let semaphore = new SemaphoreSlim(1, 1)

    let replaceInList desktopFile =
        desktopFile
        |> XDGDesktopFileParser.loadDesktopEntries
        |> Task.bind (fun newEntries ->
            task {
                do! semaphore.WaitAsync()

                // Remove old apps
                appList.RemoveAll(fun e ->
                    match e.Id with
                    | null -> false
                    | id -> id.Contains desktopFile
                ) |> ignore

                // Add new apps
                for entry in newEntries do
                    let! app = loadApplication iconLoader entry
                    appList.Add app

                subject.OnNext()
                semaphore.Release() |> ignore
            }
        )
        |> ignore

    let removeFromList (desktopFile: string) =
        task {
            do! semaphore.WaitAsync()
            appList.RemoveAll(fun e ->
                match e.Id with
                | null -> false
                | id -> id.Contains desktopFile
            )
            |> function
                | 0 -> ()
                | _ -> subject.OnNext()
            semaphore.Release() |> ignore
        }
        |> ignore

    let watchers = config.Folders |> Array.map (fun folder ->
        let watcher = new FileSystemWatcher(folder)
        watcher.Filters.Add "*.desktop"
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
