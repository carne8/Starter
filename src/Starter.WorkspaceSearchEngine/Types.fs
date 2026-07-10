namespace Starter.WorkspaceSearchEngine

open System
open System.Threading.Tasks

open Starter.SearchEngine
open ObservableCollections
open FsToolkit.ErrorHandling
open R3

module Logger =
    let mutable logger: Serilog.ILogger = unbox null
open Logger

module Constants =
    let [<Literal>] searchEngineId = "WorkspaceSearchEngine"

/// Represents a workspace from an app like vscode or rider
type Workspace =
    { Id: string
      Name: string
      Path: string
      Open: unit -> unit }

/// Loads workspaces. For instance, it can represent a vscode installation
type WorkspaceSource =
    { Id: string
      Name: string
      ShortName: string
      Icon: StarterIconSource
      LoadWorkspaces: unit -> Task<Workspace seq>
      WorkspacesChanged: Observable<unit>
      mutable ShowIfNoActivator: bool
      /// Needed for the FileSystemWatcher to not be garbage collected
      Watcher: IDisposable }

    interface ISearchEngineActivator with
        member this.Id = this.Id
        member this.Icon = this.Icon
        member this.Name = this.Name
        member this.ShortName = this.ShortName
        member this.SearchEngineId = Constants.searchEngineId

type WorkspaceSourceBuilder =
    { Id: string
      Name: string
      ShortName: string
      LoadIcon: string -> StarterIconSource
      FindExecutablePath: unit -> string option
      FindWorkspacesDb: unit -> string option
      LoadWorkspaces: string -> string -> Task<Workspace seq>
      GetChangesObservable: string -> Observable<unit> * IDisposable }

    static member build showIfNoActivator pluginPath (builder: WorkspaceSourceBuilder) =
        option {
            let! executablePath =
                builder.FindExecutablePath() |> Option.teeNone (fun () ->
                    logger.Debug $"Executable not found: {builder.Name}"
                )
            let! dbPath =
                builder.FindWorkspacesDb() |> Option.teeNone (fun () ->
                    logger.Debug $"DB path not found while executable exists: {builder.Name}"
                )

            let workspacesChanged, watcher = builder.GetChangesObservable dbPath

            return
                { Id = builder.Id
                  Name = builder.Name
                  ShortName = builder.ShortName
                  Icon = pluginPath |> builder.LoadIcon
                  LoadWorkspaces = fun () -> builder.LoadWorkspaces dbPath executablePath
                  WorkspacesChanged = workspacesChanged
                  ShowIfNoActivator = showIfNoActivator
                  Watcher = watcher }
        }


type SearchResult =
    { Id: string
      Name: string
      Path: string
      Source: WorkspaceSource
      Open: unit -> unit }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = this.Path
        member this.Keywords = Array.empty
        member this.Icon = this.Source.Icon
        member this.ShowIfNoActivator = this.Source.ShowIfNoActivator
        member this.ActivatorFilter = [| this.Source |]
        member this.GetContextMenu() = null

    static member fromWorkspace (workspaceSource: WorkspaceSource) (workspace: Workspace) =
        { Id = $"{workspaceSource.Id}:{workspace.Id}"
          Name = workspace.Name
          Path = workspace.Path
          Source = workspaceSource
          Open = workspace.Open } :> ISearchResult

type Settings =
    { ShowIfNoActivator: ObservableDictionary<string, bool> }
    static member defaultSettings = { ShowIfNoActivator = ObservableDictionary() }

module Settings =
    open System.IO
    open System.Text.Json

    [<Literal>]
    let private SettingsFilename = "settings.json"

    let getFilePath settingsDirectory = Path.Combine(settingsDirectory, SettingsFilename)

    let ensureConfigFileExists (filePath: string) =
        let fileDir = filePath |> Path.GetDirectoryName
        if fileDir |> Directory.Exists |> not then
            fileDir |> Directory.CreateDirectory |> ignore

        if filePath |> File.Exists |> not then
            logger.Information "Config file does not exist. Creating it."
            use file = File.Create filePath

            Settings.defaultSettings
            |> JsonSerializer.SerializeToUtf8Bytes
            |> file.Write

    let saveSettings (filePath: string) (settings: Settings) =
        try
            ensureConfigFileExists filePath
            let json = settings |> JsonSerializer.SerializeToUtf8Bytes
            File.WriteAllBytes(filePath, json)
        with e ->
            logger.Error(e, "Failed to save config")

    let loadSettings (filePath: string) =
        try
            ensureConfigFileExists filePath
            use stream = File.OpenRead filePath
            JsonSerializer.Deserialize<Settings> stream
        with
        | e ->
            logger.Error(e, "Failed to load config")
            Settings.defaultSettings |> saveSettings filePath
            Settings.defaultSettings

type SettingsSaver(settings: Settings, settingsFilePath) =
    do
        settings.ShowIfNoActivator.add_CollectionChanged(NotifyCollectionChangedEventHandler(fun _args ->
            settings |> Settings.saveSettings settingsFilePath
        ))
