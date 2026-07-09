module Starter.ApplicationSearchEngine.Windows.UwpLoader

open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Logger
open Starter.ApplicationSearchEngine.Windows.Uwp
open Starter.ApplicationSearchEngine.Windows.Uwp.AppxManifest
open Starter.SearchEngine

open System
open System.Diagnostics
open System.Security.Principal
open System.Threading.Tasks

open FsToolkit.ErrorHandling

open Windows.ApplicationModel
open Windows.Management.Deployment

let runApp (app: UwpApplication) =
    ProcessStartInfo(
        FileName = $"shell:AppsFolder\\{app.PackageId}",
        UseShellExecute = true
    )
    |> Process.Start
    |> function null -> () | d -> d.Dispose()

let loadApplications () =
    try
        let packageManager = PackageManager()

        WindowsIdentity.GetCurrent().Owner
        |> ValueOption.ofObj
        |> ValueOption.map (fun currentUser ->
            currentUser.Value
            |> packageManager.FindPackagesForUser
            |> Seq.collect (fun package -> AppxManifest(package).GetApplications())
        )
        |> ValueOption.defaultValue Seq.empty

    with e ->
        logger.Error(e, "Failed to load applications from Windows packages.")
        Seq.empty


type UwpAppsLoader() =
    let apps = ResizeArray<ISearchResult>()
    let changedEvent = Event<unit>()
    let mutable watcher: IDisposable voption = ValueNone

    member this.Apps = apps

    member this.LoadApps() =
        Task.Run<unit>(fun () ->
            loadApplications() |> apps.AddRange
            changedEvent.Trigger()
        ) |> ignore

    member this.ObserveFolders() =
        watcher |> ValueOption.iter _.Dispose()
        let catalog = PackageCatalog.OpenForCurrentUser()

        let add =
            Windows.Foundation.TypedEventHandler<_, PackageInstallingEventArgs>(fun _ evt ->
                if evt.IsComplete then
                    let oldCount = apps.Count
                    AppxManifest(evt.Package).GetApplications() |> apps.AddRange
                    if oldCount <> apps.Count then changedEvent.Trigger()
            )

        let remove =
            Windows.Foundation.TypedEventHandler<_, PackageUninstallingEventArgs>(fun _ evt ->
                if evt.IsComplete then
                    apps.RemoveAll(fun app ->
                        match app.Id with
                        | null -> false
                        | id -> id.StartsWith(evt.Package.Id.FullName)
                    )
                    |> function
                        | 0 -> ()
                        | _ -> changedEvent.Trigger()
            )

        let update =
            Windows.Foundation.TypedEventHandler<_, PackageUpdatingEventArgs>(fun _ evt ->
                if evt.IsComplete then
                    apps.RemoveAll(fun app ->
                        match app.Id with
                        | null -> false
                        | id -> id.StartsWith(evt.SourcePackage.Id.FullName)
                    ) |> ignore
                    AppxManifest(evt.TargetPackage).GetApplications() |> apps.AddRange
                    changedEvent.Trigger()
            )

        catalog.add_PackageInstalling(add)
        catalog.add_PackageUninstalling(remove)
        catalog.add_PackageUpdating(update)

        watcher <- ValueSome { new IDisposable with
            member this.Dispose() =
                catalog.remove_PackageInstalling(add)
                catalog.remove_PackageUninstalling(remove)
                catalog.remove_PackageUpdating(update)
        }

    [<CLIEvent>]
    member this.Changed = changedEvent.Publish
