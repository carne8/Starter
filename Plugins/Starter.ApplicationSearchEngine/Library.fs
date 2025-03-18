namespace Starter.ApplicationSearchEngine

open System
open System.Diagnostics
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic

open FSharp.Control.Reactive
open FsToolkit.ErrorHandling
open Vanara.PInvoke
open Vanara.Windows.Shell

open Starter.SearchEngine
open Avalonia.Media.Imaging
open IconHelper

[<RequireQualifiedAccess>]
type ExecutionPath =
    | ExeFile of string
    | PackageId of string

type Application =
    { Id: string
      Name: string
      ExecutionPath: ExecutionPath
      LoadIcon: unit -> Task<Bitmap> }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.LoadIcon() = this.LoadIcon()

type AppIndexer() =
    let mutable applications = Array.empty<Application>
    let indexingFinished = TaskCompletionSource()

    let getAppIcon (targetPathOpt: string option) (app: ShellItem) =
        // Icon for the Appx apps
        let packageIconOpt =
            app
            |> WindowsPackage.ofShellItem
            |> Option.bind WindowsPackage.getIcon

        let getShellIcon () =
            app.Images
               .GetImage(SIZE(35, 35),  ShellItemGetImageOptions.IconOnly) // TODO: Pass the size of the icon from the host program
               .ToAvaloniaBitmap()

        match packageIconOpt, targetPathOpt with
        | Some iconPath, _ -> new Bitmap(iconPath) // Found an icon associated with package
        | None, Some filePath when filePath.ToLowerInvariant().EndsWith ".exe" -> // Take the .exe icon
            IconHelper.getFileIcon (Avalonia.PixelSize(35, 35)) filePath
            |> Option.defaultWith getShellIcon // Let the shell load the icon
        | _ ->
            getShellIcon() // Let the shell load the icon

    let getApps () =
        use appsFolder = new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_AppsFolder)

        appsFolder |> Seq.choose (fun app ->
            option {
                let! name = app.Name |> Option.require (String.IsNullOrEmpty >> not)
                let packageIdOpt = app |> ShellItem.Property.get "System.AppUserModel.ID"
                let targetPathOpt = app |> ShellItem.Property.get "System.Link.TargetParsingPath"

                let icon = app |> getAppIcon targetPathOpt // TODO: Load icons only when needed

                let! executionPath =
                    match packageIdOpt, targetPathOpt with
                    | Some pkgId, _ -> ExecutionPath.PackageId pkgId |> Some
                    | _, Some linkPath -> ExecutionPath.ExeFile linkPath |> Some
                    | None, None -> None

                app.Dispose()
                return
                    { Id = app.ParsingName
                      Name = name
                      ExecutionPath = executionPath
                      LoadIcon = fun () -> Task.singleton icon }
            }
        )

    do
        Task.Run<unit>(fun () -> task {
            try
                let apps =
                    getApps()
                    |> Seq.sortBy _.Name
                    |> Seq.toArray

                applications <- apps
                indexingFinished.SetResult()
            with e ->
                indexingFinished.SetException e
        }) |> ignore

    member _.FindApp(query: string, ct: CancellationToken) = task {
        do! indexingFinished.Task.WaitAsync ct
        return applications |> Array.filter _.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
    }

type ApplicationSearchEngine(pluginPath) =
    inherit SearchEngineBase(pluginPath)
    let indexer = AppIndexer()

    override _.Id = nameof ApplicationSearchEngine
    override _.DisplayName = "Application"

    override _.Search(query, ct) =
        { new IObservable<ISearchResult array> with
            member _.Subscribe (observer: IObserver<ISearchResult array>) =
                Task.Run<unit>(
                    fun () -> task {
                        try
                            let! apps = indexer.FindApp(query, ct)
                            apps
                            |> unbox<ISearchResult array>
                            |> observer.OnNext
                        with exn ->
                            exn |> observer.OnError
                    },
                    ct
                ) |> ignore

                { new IDisposable with member _.Dispose() = () }
                // Disposing is already handled by the cancellation token
        }

    override _.SearchResultSelected(searchResult) =
        match searchResult with
        | :? Application as sr ->
            let execStr =
                match sr.ExecutionPath with
                | ExecutionPath.ExeFile path -> path
                | ExecutionPath.PackageId pkgId -> $"shell:AppsFolder\\{pkgId}"

            ProcessStartInfo(
                FileName = execStr,
                UseShellExecute = true
            )
            |> Process.Start
            |> ignore
        | _ -> ()
