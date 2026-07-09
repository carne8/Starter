namespace Starter.ApplicationSearchEngine.Linux

open System
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open R3
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Linux
open Starter.ApplicationSearchEngine.Linux.IconLoader
open Starter.ApplicationSearchEngine.Logger
open Starter.SearchEngine

type LinuxAppsSearchEngine() =
    let icon = Constants.getIcon ()

    static let defaultDataDirectories = // TODO: Make it respect the hierarchy and prioritize the first matches
        let userShare =
            Path.Combine(
                Environment.GetFolderPath Environment.SpecialFolder.UserProfile,
                ".local/share"
            )

        match Environment.GetEnvironmentVariable "XDG_DATA_DIRS" with
        | null -> Seq.empty
        | xdgDataDirs -> xdgDataDirs.Split ':'
        |> Seq.append (Seq.singleton userShare)
        |> Seq.filter Directory.Exists
        |> Seq.toArray

    static let defaultFolderConfig =
        { Folders = defaultDataDirectories
          ExcludedFolders = Array.empty
          AllowDuplicates = true } // Not applicable for Linux

    let apps = ResizeArray<ISearchResult> 200
    let resultsChanged = DelegateEvent<EventHandler<ISearchResult seq>>()
    let mutable disposables = ResizeArray 3 // Btw: keep a reference of the app watcher and prevent it from being garbage collected

    let useGtkLaunch =
        try
            use proc =
                ProcessStartInfo(
                    FileName = "gtk-launch",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                )
                |> Process.Start
            match proc with
            | null -> false
            | proc -> proc.WaitForExit(TimeSpan.FromSeconds 3L) && proc.ExitCode = 0
        with _ -> false

    do if not useGtkLaunch then
        logger.Information "gtk-launch not available"

    member private this.LoadApps() =
        task {
            let! iconLoader = IconLoader.create()
            let! newApps =
                AppsLoader.loadApplications
                    iconLoader
                    useGtkLaunch
                    defaultFolderConfig

            newApps |> apps.AddRange
            resultsChanged.Trigger [| null; apps |]

            let observable, disposable =
                AppsLoader.observeApplicationChanges
                    iconLoader
                    useGtkLaunch
                    apps
                    defaultFolderConfig

            disposables.Add disposable
            observable.Subscribe(fun () -> resultsChanged.Trigger [| null; apps |]) |> ignore
        }

    interface IStaticSearchEngine with
        member this.LoadResults() =
            if not <| OperatingSystem.IsLinux() then
                logger.Warning "This search engine is not supported on this platform."
            else
                this.LoadApps |> Task.Run<unit> |> ignore

            ValueTask.FromResult Seq.empty

        member _.Id = nameof LinuxAppsSearchEngine
        member _.Name = "Applications"
        member _.ShortName = "Apps"
        member _.Icon = icon
        member this.Activators = [| DefaultSearchEngineActivator(this) |]

        member _.SearchResultSelected(searchResult) =
            match searchResult with
            | :? DesktopApplication as app ->
                ProcessStartInfo(
                    FileName = "setsid",
                    Arguments = app.Exec,
                    #if DEBUG // Hide process logs
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    #endif
                    CreateNoWindow = true
                )
                |> Process.Start
                |> function null -> () | d -> d.Dispose()

                // TODO: DBus Activation -> https://specifications.freedesktop.org/desktop-entry-spec/latest/dbus.html
                // TODO: Check manually into the $PATH -> https://specifications.freedesktop.org/desktop-entry-spec/latest/exec-variables.html
                // TODO: Maybe this https://specifications.freedesktop.org/desktop-entry-spec/latest/extra-actions.html
            | _ -> ()

        [<CLIEvent>]
        member this.ResultsChanged = resultsChanged.Publish
        member this.add_Changed _ = ()
        member this.remove_Changed _ = ()

type Factory(pluginPath) =
    inherit SearchEngineFactory(pluginPath)

    override this.LoadSearchEngineIds() = [| nameof LinuxAppsSearchEngine |]
    override this.LoadSearchEngine(_, _, logger, _) =
        Logger.logger <- logger
        LinuxAppsSearchEngine(), null
