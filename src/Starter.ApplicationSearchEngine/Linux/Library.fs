namespace Starter.ApplicationSearchEngine.Linux

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open R3
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Linux
open Starter.SearchEngine

type LinuxAppsSearchEngine(pluginPath, configDir, logger) =
    inherit StaticSearchEngine(pluginPath, configDir, logger)
    static let icon = Constants.icon
    
    static let defaultDataDirectories = // TODO: Make it respect the hierarchy and prioritize the first matches 
        "XDG_DATA_DIRS"
        |> Environment.GetEnvironmentVariable
        |> fun s -> s.Split ':'
        |> Array.append
            [| (Environment.SpecialFolder.UserProfile |> Environment.GetFolderPath,
                ".local/share")
               |> Path.Combine |]
        |> Array.filter Directory.Exists

    static let defaultFolderConfig =
        { Folders = defaultDataDirectories
          ExcludedFolders = Array.empty  }

    let apps = ResizeArray<ISearchResult>(200)
    let results = new Subject<ISearchResult seq>()
    let mutable disposables = ResizeArray(3) // Btw: keep a reference of the app watcher and prevent it from being garbage collected

    do Logger.logger <- logger

    member private this.LoadApps() =
        task {
            let! newApps = defaultFolderConfig |> AppsLoader.loadApplications
            newApps |> apps.AddRange
            apps.ToArray() |> results.OnNext

            let observable, disposable = AppsLoader.observeApplicationChanges apps defaultFolderConfig
            disposables.Add disposable
            observable.Subscribe(fun () -> apps.ToArray() |> results.OnNext) |> ignore
        }

    override this.LoadResults() =
        if not <| OperatingSystem.IsLinux() then
            logger.Warning("This search engine is not supported on this platform.")
        else
            this.LoadApps |> Task.Run<unit> |> ignore

        struct (Seq.empty, results.AsObservable()) |> Task.FromResult

    override _.Id = nameof LinuxAppsSearchEngine
    override _.Name = "Applications"
    override _.ShortName = "Apps"
    override _.Icon = icon

    override _.SearchResultSelected(searchResult) =
        match searchResult with
        | :? DesktopApplication as app ->
            match app.Exec with
            | ValueNone -> logger.Warning $"The app {app.Name} doesn't provide a valid Exec command"
            | ValueSome exec ->
                // TODO: Prevent logs from showing
                // TODO: DBus Activation -> https://specifications.freedesktop.org/desktop-entry-spec/latest/dbus.html
                // TODO: Check manually into the $PATH -> https://specifications.freedesktop.org/desktop-entry-spec/latest/exec-variables.html
                // TODO: Maybe this https://specifications.freedesktop.org/desktop-entry-spec/latest/extra-actions.html
                ProcessStartInfo(
                    FileName = "nohup",
                    WorkingDirectory = (app.WorkingDirectory |> ValueOption.defaultValue null),
                    Arguments = exec,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                )
                |> Process.Start
                |> ignore
        | _ -> ()
    override this.LoadSettingsControl() = null
