namespace Starter.WorkspaceSearchEngine

open FsToolkit.ErrorHandling
open ObservableCollections
open Starter.SearchEngine
open System
open System.Threading.Tasks
open R3

module Logger =
    let mutable logger: Serilog.ILogger = unbox null

/// Represents a workspace from an app like vscode or rider
type Workspace =
    { Id: string
      Name: string
      Path: string
      Open: unit -> unit }

/// Loads workspaces. For instance it can represents a vscode installation
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
        member this.SearchEngineId = "WorkspaceSearchEngine" // TODO

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
            let! executablePath = builder.FindExecutablePath()
            let! dbPath = builder.FindWorkspacesDb()
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
    open Logger

    [<Literal>]
    let private SettingsFilename = "settings.json"

    let getFilePath settingsDirectory = Path.Combine(settingsDirectory, SettingsFilename)

    let ensureFileExists (filePath: string) =
        let fileDir = filePath |> Path.GetDirectoryName
        if fileDir |> Directory.Exists |> not then
            fileDir |> Directory.CreateDirectory |> ignore

        if filePath |> File.Exists |> not then
            filePath |> File.Create |> _.Dispose()

    let saveSettings (filePath: string) (settings: Settings) =
        try
            ensureFileExists filePath
            let json = settings |> JsonSerializer.SerializeToUtf8Bytes
            File.WriteAllBytes(filePath, json)
        with e ->
            logger.Warning(e, "Failed to save settings")
            failwith "Failed to save settings"

    let loadSettings (filePath: string) =
        try
            use stream = File.OpenRead filePath
            JsonSerializer.Deserialize<Settings> stream
        with
        | :? DirectoryNotFoundException ->
            logger.Information("Settings file doesn't exists. Creating it.")
            Settings.defaultSettings |> saveSettings filePath
            Settings.defaultSettings
        | e ->
            logger.Warning(e, "Failed to load settings")
            Settings.defaultSettings |> saveSettings filePath
            Settings.defaultSettings

type SettingsSaver(settings: Settings, settingsFilePath) =
    do
        settings.ShowIfNoActivator.add_CollectionChanged(NotifyCollectionChangedEventHandler(fun args ->
            settings |> Settings.saveSettings settingsFilePath
        ))
