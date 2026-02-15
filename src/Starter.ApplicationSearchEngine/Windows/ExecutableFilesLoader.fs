module Starter.ApplicationSearchEngine.Windows.ExeLoader

open System.Diagnostics
open R3
open Starter.SearchEngine
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Windows.IconHelper
open Starter.ApplicationSearchEngine.Logger

open System
open System.IO
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

module FolderConfiguration =
    let Default =
        { Folders =
            [| Environment.GetFolderPath(Environment.SpecialFolder.Programs)
               Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms) |]
          ExcludedFolders =
            [| Environment.GetFolderPath(Environment.SpecialFolder.Startup)
               Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) |]  }

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

        use shellItem = new ShellItem(file)
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

let loadApplications (config: FolderConfiguration) =
    Task.Run(fun () ->
        let appFiles =
            config.Folders
            |> Seq.collect (fun dir -> Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            |> Seq.filter (FolderConfiguration.isFileExcluded config >> not)

        // Load apps according to the config order to prevent duplicate names
        let apps = Dictionary()
        appFiles |> Seq.iter (fun appFile ->
            appFile
            |> getAppFromFile
            |> ValueOption.iter (fun app ->
                match apps.TryAdd(app.Name, app :> ISearchResult) with
                | false -> logger.Verbose $"Duplicate app (this file is ignored): {app.Path}"
                | true -> ()
            )
        )

        apps.Values
    )

let observeApplicationChanges (appList: List<ISearchResult>) (config: FolderConfiguration) =
    let subject = new Subject<unit>()
    let watchers = config.Folders |> Array.map (fun folder ->
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
            |> ValueOption.iter (fun app ->
                appList.Add app
                subject.OnNext()
            )
        )
        watcher.Deleted.Add(fun args ->
            appList.FindIndex(_.Id >> (=) args.FullPath) |> appList.RemoveAt
            subject.OnNext()
        )
        watcher.Renamed.Add(fun args ->
            appList.FindIndex(_.Id >> (=) args.OldFullPath) |> appList.RemoveAt

            args.FullPath
            |> getAppFromFile
            |> ValueOption.iter (fun app ->
                appList.Add app
                subject.OnNext()
            )
        )

        watcher.IncludeSubdirectories <- true
        watcher.EnableRaisingEvents <- true
        watcher
    )

    subject,
    { new IDisposable with
        member _.Dispose() =
            watchers |> Array.iter _.Dispose()
            subject.Dispose() }
