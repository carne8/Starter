module Starter.ApplicationSearchEngine.Windows.ExeLoader

open System.Diagnostics
open Starter.SearchEngine
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Windows.Exe.IconHelper
open Starter.ApplicationSearchEngine.Logger

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic

open FsToolkit.ErrorHandling
open Vanara.PInvoke
open Vanara.Windows.Shell

type ExeApplication =
    { Id: string
      Name: string
      Path: string
      Keywords: string array
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = "Application"
        member this.Keywords = this.Keywords
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty

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
            | false -> file |> IconHelper.getFileIcon Constants.iconPixelSize
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
              Icon = StarterIconSource(icon, icon) }
    }

let loadApplications (ct: CancellationToken) (config: FolderConfiguration) =
    Task.Run(fun () ->
        let appFiles =
            config.Folders
            |> Seq.collect (fun dir -> Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            |> Seq.filter (FolderConfiguration.isFileExcluded config >> not)

        // Load apps according to the config order to prevent duplicate names
        let apps = Dictionary()
        let rec loop (enumerator: IEnumerator<_>) =
            if enumerator.MoveNext() && not ct.IsCancellationRequested then
                enumerator.Current
                |> getAppFromFile
                |> ValueOption.iter (fun app ->
                    match apps.TryAdd(app.Name, app :> ISearchResult) with
                    | false -> logger.Verbose $"Duplicate app (this file is ignored): {app.Path}"
                    | true -> ()
                )

                loop enumerator

        appFiles.GetEnumerator() |> loop
        apps.Values
    )

let observeFolder added removed (folder: string) =
    let watcher = new FileSystemWatcher(folder)

    watcher.Filters.Add("*.exe")
    watcher.Filters.Add("*.lnk")
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

        Task.Run<unit>(fun () -> task {
            let ct = cts.Token
            apps.Clear()
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
                    apps.RemoveAll(fun app -> app.Id = appPathToRemove) |> ignore
                    changedEvent.Trigger()
                )
        )

    [<CLIEvent>]
    member this.Changed = changedEvent.Publish
