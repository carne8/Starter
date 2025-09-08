namespace Starter.ApplicationSearchEngine.Linux

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Linux
open Starter.SearchEngine

type LinuxAppsSearchEngine(pluginPath, configDir, logger) =
    inherit StaticSearchEngine(pluginPath, configDir, logger)
    static let icon = Constants.icon
    static let defaultFolderConfig =
        { Folders =
            [| Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local/share/applications"
               )
               "/usr/share/applications/"
               "/usr/local/share/applications/" |]
          ExcludedFolders = Array.empty  }

    let apps = ResizeArray<ISearchResult>(200)
    let mutable disposables = ResizeArray(2) // Btw: keep a reference of the app watcher and prevent it from being garbage collected

    do Logger.logger <- logger

    override this.LoadResults() =
        if not <| OperatingSystem.IsLinux() then
            logger.Warning("This search engine is not supported on this platform.")
        else
            Task.Run<unit>(fun () ->
                defaultFolderConfig
                |> AppsLoader.loadApplications
                |> Task.map (fun newApps ->
                    newApps |> apps.AddRange
                    apps.ToArray() |> this.ResultsChanged.OnNext
                )
            ) |> ignore

        Array.empty |> Task.FromResult

    override _.Id = nameof LinuxAppsSearchEngine
    override _.Name = "Applications"
    override _.ShortName = "Apps"
    override _.Icon = icon

    override _.SearchResultSelected(searchResult) =
        match searchResult with
        | :? DesktopApplication as app ->
            ProcessStartInfo(
                FileName = "nohup",
                Arguments = app.Exec,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            )
            |> Process.Start
            |> ignore
        | _ -> ()
    override this.LoadSettingsControl() = null
