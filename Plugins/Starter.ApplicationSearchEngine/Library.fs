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
type private FileNames =
    static member AppScores = "app.scores"

[<RequireQualifiedAccess>]
type ExecutionPath =
    | ExeFile of string
    | PackageId of string

type Application =
    { Id: string
      Name: string
      Score: (int * DateTimeOffset) option
      ExecutionPath: ExecutionPath
      LoadIcon: unit -> Task<Bitmap> }

    interface ISearchResult with
        member this.Name = this.Name
        member this.LoadIcon() = this.LoadIcon()

/// Indexes apps and cache their icon
/// App score based on https://github.com/ajeetdsouza/zoxide/wiki/Algorithm
type AppIndexer(scoresFilePath) =
    static let MaxAge = 10_000.

    let applications = List<Application>()
    let mutable indexingFinished = TaskCompletionSource()

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

    let getApps (savedScores: IDictionary<string, _>) =
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

                let score =
                    match savedScores.TryGetValue app.ParsingName with
                    | false, _ -> None
                    | true, score -> Some score

                app.Dispose()
                return
                    { Id = app.ParsingName
                      Name = name
                      Score = score
                      ExecutionPath = executionPath
                      LoadIcon = fun () -> Task.singleton icon }
            }
        )

    do
        Task.Run<unit>(fun () -> task {
            try
                let! savedScores = ScoresSaver.readFromFile scoresFilePath
                let apps = getApps savedScores |> Seq.sortBy _.Name

                applications.AddRange apps
                indexingFinished.SetResult()
            with e ->
                indexingFinished.SetException e
        }) |> ignore

    member _.FindApp(query: string) = task {
        do! indexingFinished.Task
        return
            applications
            |> Seq.choose (fun app ->
                match app.Name.Contains(query, StringComparison.OrdinalIgnoreCase) with
                | true -> Some app
                | false -> None
            )
            |> Seq.sortBy (fun app ->
                match app.Score with
                | None -> 0., app.Name.ToLowerInvariant(), TimeSpan.MaxValue
                | Some (score, lastAccessDate) ->
                    let d = DateTimeOffset.Now - lastAccessDate
                    let s = float -score

                    let f =
                        if d.TotalHours < 1 then s * 4.
                        elif d.TotalDays < 1 then s * 2.
                        elif d.TotalDays < 7 then s / 2.
                        else s / 4.
                    f, app.Name.ToLowerInvariant(), d
            )
    }

    member this.CheckMaxAging() =
        let totalScore =
            applications
            |> Seq.sumBy (
                _.Score
                >> Option.map fst
                >> Option.defaultValue 0
            )
            |> float

        if totalScore > MaxAge then
            let k = (0.9 * MaxAge) / totalScore

            for idx in 0..applications.Count-1 do
                let app = applications[idx]

                match app.Score with
                | None -> ()
                | Some (score, d) ->
                    let newScore =
                        match float score * k |> Math.Round |> int with
                        | 0 -> None
                        | newScore -> Some (newScore, d)

                    applications.RemoveAt idx
                    applications.Insert(idx, { app with Score = newScore })

    member this.IncreaseAppScore(app: Application) = task {
        let appIdx = applications.IndexOf app
        let newScore =
            app.Score
            |> Option.map (fun (s, _) -> s+1, DateTimeOffset.Now)
            |> Option.defaultValue (1, DateTimeOffset.Now)

        let newApp = { app with Score = Some newScore }

        // Apply changes
        applications.RemoveAt appIdx
        applications.Insert(appIdx, newApp)
        this.CheckMaxAging()

        // Save new scores
        do! applications
            |> Seq.choose (fun app ->
                match app.Score with
                | None -> None
                | Some s -> Some (app.Id, s)
            )
            |> dict
            |> ScoresSaver.writeToFile scoresFilePath
    }

type ApplicationSearchEngine(pluginPath) =
    inherit SearchEngine(pluginPath)
    let indexer = Path.Combine(pluginPath, FileNames.AppScores) |> AppIndexer

    override _.Id = nameof ApplicationSearchEngine
    override _.DisplayName = "Application"

    override _.Search(query, _ct) =
        { new IObservable<ISearchResult seq> with
            member _.Subscribe (observer: IObserver<ISearchResult seq>) =
                let cts = new CancellationTokenSource()

                Task.Run<unit>(
                    fun () -> task {
                        try
                            let! apps = indexer.FindApp query
                            apps
                            :?> ISearchResult seq
                            |> observer.OnNext
                        with exn ->
                            exn |> observer.OnError
                    },
                    cts.Token
                ) |> ignore

                { new IDisposable with
                    member _.Dispose() =
                        cts.Cancel()
                        cts.Dispose() }
        }

    override _.SearchResultSelected(searchResult) =
        match searchResult with
        | :? Application as sr ->
            indexer.IncreaseAppScore sr |> ignore

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
