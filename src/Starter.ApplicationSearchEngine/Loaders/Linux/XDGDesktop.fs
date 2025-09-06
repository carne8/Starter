module Starter.ApplicationSearchEngine.Loaders.Linux.XDGDesktop

open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine
open Starter.SearchEngine



module FolderConfiguration =
    let Default =
        { Folders =
            [| Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local/share/applications"
               )
               "/usr/share/applications/"
               "/usr/local/share/applications/" |]
          ExcludedFolders = Array.empty  }

let runApp (app: DesktopApplication) =
    let struct (fileName, args) =
        match app.Exec.IndexOf ' ' with
        | -1 -> struct (app.Exec, String.Empty)
        | i -> struct (app.Exec[..i-1], app.Exec[i..])

    ProcessStartInfo(
        FileName = fileName,
        Arguments = args,
        UseShellExecute = true
    )
    |> Process.Start
    |> ignore

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
                            |> DesktopFileParser.loadDesktopEntries
                            |> Task.map (Seq.iter (fun app ->
                                apps.TryAdd(path |> Path.GetFileName, app) |> ignore
                            ))
                            |> ValueTask
                    )
                )

            return apps.Values :> ISearchResult seq
        }
    )

// let observeApplicationChanges (appList: List<ISearchResult>) (config: FolderConfiguration) =
//     let subject = new Subject<unit>()
//     let watchers = config.Folders |> Array.map (fun folder ->
//         let watcher = new FileSystemWatcher(folder)
//         watcher.Filters.Add("*.exe")
//         watcher.Filters.Add("*.lnk")
//         watcher.NotifyFilter <-
//             NotifyFilters.CreationTime
//             ||| NotifyFilters.DirectoryName
//             ||| NotifyFilters.FileName
//             ||| NotifyFilters.LastWrite
//
//         watcher.Created.Add(fun args ->
//             args.FullPath
//             |> getAppFromFile
//             |> Option.iter appList.Add
//             subject.OnNext()
//         )
//         watcher.Deleted.Add(fun args ->
//             appList.FindIndex(_.Id >> (=) args.FullPath) |> appList.RemoveAt
//             subject.OnNext()
//         )
//         watcher.Renamed.Add(fun args ->
//             appList.FindIndex(_.Id >> (=) args.OldFullPath) |> appList.RemoveAt
//
//             args.FullPath
//             |> getAppFromFile
//             |> Option.iter appList.Add
//             subject.OnNext()
//         )
//
//         watcher.IncludeSubdirectories <- true
//         watcher.EnableRaisingEvents <- true
//         watcher
//     )
//
//     subject,
//     { new IDisposable with
//         member _.Dispose() =
//             watchers |> Array.iter _.Dispose()
//             subject.Dispose() }
