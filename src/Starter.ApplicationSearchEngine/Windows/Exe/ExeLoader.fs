module Starter.ApplicationSearchEngine.Windows.ExeLoader

open Avalonia.Threading
open Starter.SearchEngine
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Logger
open Starter.Shared

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent

open FsToolkit.ErrorHandling
open Starter.Shared.IconHelper
open Vanara.PInvoke
open Vanara.Windows.Shell

type ExeApplication =
    { Id: string
      Name: string
      Path: string
      Keywords: string array
      mutable AlternativeDescription: bool
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = if this.AlternativeDescription then this.Path else "Application"
        member this.Keywords = this.Keywords
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
        member this.ContextMenuLoader = new ContextMenu.ContextMenuLoader(this.Path)

let runApp (app: ExeApplication) =
    ProcessStartInfo(
        FileName = app.Path,
        UseShellExecute = true
    )
    |> Process.Start
    |> function null -> () | d -> d.Dispose()

let private getAppFromFile (file: string) =
    voption {
        let! ext = file |> Path.GetExtension
        let ext = ext.ToLowerInvariant()
        do! match ext with
            | ".exe" | ".lnk" | ".url" -> ValueSome ()
            | _ -> ValueNone

        use! shellItem =
            try new ShellItem(file) |> ValueSome
            with _ -> ValueNone

        let! name = shellItem.GetDisplayName(ShellItemDisplayString.NormalDisplay)
        let icon =
            match ext = ".url" with
            | false ->
                Dispatcher.UIThread.Invoke(
                    (fun () -> file |> IconHelper.getFileIcon Constants.iconPixelSize),
                    DispatcherPriority.Background
                )
            | true -> file |> IconHelper.getUrlFileIcon
            |> ValueOption.defaultWith (fun () ->
                shellItem
                    .Images
                    .GetImage(SIZE(Constants.IconSize, Constants.IconSize), ShellItemGetImageOptions.IconOnly)
                    .ToAvaloniaBitmap()
            )

        return
            { Id = file
              Name = name
              Path = file
              Keywords = [| ext |]
              AlternativeDescription = false
              Icon = StarterIconSource(icon, icon) }
    }

let loadApplications (ct: CancellationToken) (config: FolderConfiguration) =
    let loadAppsOfFolder folder =
        task {
            if ct.IsCancellationRequested then return seq {} else

            let files =
                Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                |> Seq.filter (FolderConfiguration.isFileExcluded config >> not)
                |> Seq.toArray

            let apps = ConcurrentBag()

            do! Parallel.ForEachAsync(
                files,
                ParallelOptions(CancellationToken = ct),
                Func<_, _, ValueTask>(fun appFile _ ->
                    appFile
                    |> getAppFromFile
                    |> ValueOption.iter apps.Add

                    ValueTask.CompletedTask
                )
            )

            return apps :> _ seq
        }

    task {
        let! apps =
            config.Folders
            |> Array.map loadAppsOfFolder
            |> Task.WhenAll

        let appsDict = Dictionary()
        let appsOutput = ResizeArray<ISearchResult>()

        // Load apps according to the config order to prevent duplicate names
        apps |> Array.iter (Seq.iter (fun app ->
            if config.AllowDuplicates then
                if not <| appsDict.TryAdd(app.Name, app) then
                    logger.Verbose $"Duplicate app: {app.Path}"
                    appsDict[app.Name].AlternativeDescription <- true
                    app.AlternativeDescription <- true

                appsOutput.Add app
            else
                match appsDict.TryAdd(app.Name, app) with
                | false -> logger.Verbose $"Duplicate app (file is ignored): {app.Path}"
                | true -> appsOutput.Add app
        ))

        return appsOutput
    }

let observeFolder added removed (folder: string) =
    let watcher = new FileSystemWatcher(folder)

    watcher.Filters.Add("*.exe")
    watcher.Filters.Add("*.lnk")
    watcher.Filters.Add("*.url")
    watcher.NotifyFilter <-
        NotifyFilters.CreationTime
        ||| NotifyFilters.DirectoryName
        ||| NotifyFilters.FileName
        ||| NotifyFilters.LastWrite

    watcher.Created.Add(fun args ->
        args.FullPath
        |> getAppFromFile
        |> ValueOption.iter added
    )
    watcher.Deleted.Add(fun args -> args.FullPath |> removed)
    watcher.Renamed.Add(fun args ->
        args.OldFullPath |> removed
        args.FullPath
        |> getAppFromFile
        |> ValueOption.iter added
    )

    watcher.IncludeSubdirectories <- true
    watcher.EnableRaisingEvents <- true
    watcher :> IDisposable

type ExeAppsLoader() =
    let apps  = ResizeArray<ISearchResult>()
    let changedEvent = Event<unit>()
    let mutable watchers = Array.empty<IDisposable>

    let mutable cts = new CancellationTokenSource()

    member this.Apps = apps

    member this.LoadApps(folderConfig: FolderConfiguration) =
        cts.Cancel()
        cts <- new CancellationTokenSource()
        let ct = cts.Token
        apps.Clear()

        Task.Run<unit>(fun () -> task {
            let! newApps = loadApplications ct folderConfig
            if not ct.IsCancellationRequested then
                apps.AddRange newApps
                changedEvent.Trigger()
        })
        |> ignore

    member this.ObserveFolders(folderConfig: FolderConfiguration) =
        watchers |> Array.iter _.Dispose()
        watchers <- folderConfig.Folders |> Array.map (
            observeFolder
                (fun newApp ->
                    if newApp.Path
                       |> FolderConfiguration.isFileExcluded folderConfig
                       |> not then
                        apps.Add newApp
                        changedEvent.Trigger()
                )
                (fun appPathToRemove ->
                    let removedCount = apps.RemoveAll(fun app -> app.Id = appPathToRemove)
                    if removedCount <> 0 then changedEvent.Trigger()
                )
        )

    [<CLIEvent>]
    member this.Changed = changedEvent.Publish
