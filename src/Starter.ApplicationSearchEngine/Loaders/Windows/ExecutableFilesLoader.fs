module Starter.ApplicationSearchEngine.Loaders.Windows.Exe

open System.Diagnostics
open R3
open Starter.SearchEngine
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Windows.IconHelper

open System
open System.IO
open System.Threading.Tasks
open System.Collections.Generic

open FsToolkit.ErrorHandling
open Vanara.Windows.Shell

type ExeApplication =
    { Id: string
      Name: string
      Path: string
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = "Application"
        member this.Icon = this.Icon

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
    |> ignore

let private getAppFromFile (file: string) =
    option {
        do! match file |> Path.GetExtension |> _.ToLowerInvariant() with
            | ".exe" | ".lnk" -> Some ()
            | _ -> None

        use shellItem = new ShellItem(file)
        let name = shellItem.GetDisplayName(ShellItemDisplayString.NormalDisplay)
        let! icon =
            file
            |> IconHelper.getFileIcon Constants.iconPixelSize
            |> Option.defaultWith (fun () ->
                shellItem
                    .Images
                    .GetImage(Vanara.PInvoke.SIZE(Constants.IconSize, Constants.IconSize), ShellItemGetImageOptions.IconOnly)
                    .ToAvaloniaBitmap()
            )

        return
            { Id = file
              Name = name
              Path = file
              Icon = StarterIconSource(icon, icon) } :> ISearchResult
    }

let loadApplications (config: FolderConfiguration) : Task<ISearchResult seq> =
    Task.Run<ISearchResult seq>(fun () ->
        config.Folders
        |> Seq.collect (fun dir -> Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
        |> Seq.filter (FolderConfiguration.isFileExcluded config >> not)
        |> Seq.choose getAppFromFile
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
            |> Option.iter appList.Add
            subject.OnNext()
        )
        watcher.Deleted.Add(fun args ->
            appList.FindIndex(_.Id >> (=) args.FullPath) |> appList.RemoveAt
            subject.OnNext()
        )
        watcher.Renamed.Add(fun args ->
            appList.FindIndex(_.Id >> (=) args.OldFullPath) |> appList.RemoveAt

            args.FullPath
            |> getAppFromFile
            |> Option.iter appList.Add
            subject.OnNext()
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
