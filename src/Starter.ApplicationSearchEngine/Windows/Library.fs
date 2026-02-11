namespace Starter.ApplicationSearchEngine.Windows

open System
open System.Threading.Tasks
open R3
open Starter.ApplicationSearchEngine
open Starter.SearchEngine

type WindowsAppsSearchEngine(pluginPath, configDir, logger) =
    inherit StaticSearchEngine(pluginPath, configDir, logger)
    static let icon = Constants.icon

    let apps = ResizeArray<ISearchResult>(100)
    let mutable disposables = ResizeArray(2) // Btw: keep a reference of the UWP watcher and prevent it from being garbage collected
    let resultsObservable = new Subject<ISearchResult seq>()

    do Logger.logger <- logger

    override this.LoadResults() =
        if not <| OperatingSystem.IsWindows() then
            logger.Warning("This search engine is not supported on this platform.")
        else
            Parallel.Invoke(
                (fun () ->
                    task {
                        let! uwpApps = UwpLoader.loadApplications logger

                        let observable, disposable = UwpLoader.observeApplicationChanges apps
                        disposables.Add disposable
                        observable.Subscribe(fun () -> apps.ToArray() |> resultsObservable.OnNext) |> ignore

                        apps.AddRange uwpApps
                        apps.ToArray() |> resultsObservable.OnNext
                    } |> ignore
                ),
                (fun () ->
                    task {
                        let exeFolderConfig = ExeLoader.FolderConfiguration.Default
                        let! exeApps =
                            ExeLoader.FolderConfiguration.Default
                            |> ExeLoader.loadApplications

                        let observable, disposable = ExeLoader.observeApplicationChanges apps exeFolderConfig
                        disposables.Add disposable
                        observable.Subscribe(fun () -> apps.ToArray() |> resultsObservable.OnNext) |> ignore

                        apps.AddRange exeApps
                        apps.ToArray() |> resultsObservable.OnNext
                    } |> ignore
                )
            )

        struct (Seq.empty, resultsObservable.AsObservable()) |> Task.FromResult

    override _.Id = nameof WindowsAppsSearchEngine
    override _.Name = "Applications"
    override _.ShortName = "Apps"
    override _.Icon = icon

    override _.SearchResultSelected(searchResult) =
        match searchResult with
        | :? ExeLoader.ExeApplication as app -> ExeLoader.runApp app
        | :? UwpLoader.UwpApplication as app -> UwpLoader.runApp app
        | _ -> ()
    override this.LoadSettingsControl() = null
