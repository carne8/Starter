namespace Starter.ApplicationSearchEngine.Windows

open System
open Avalonia.Controls
open Avalonia.Markup.Xaml

open System.Collections.ObjectModel
open System.IO

open R3
open Starter.ApplicationSearchEngine

type FolderViewModel(path: string, list: ObservableCollection<FolderViewModel>, changed: Subject<unit>) =
    let mutable path = path

    member this.Path
        with get () = path
        and set v =
            path <- v
            changed.OnNext()
    member this.Remove() = list.Remove this

type SettingsViewModel(configDir) =
    let configPath = Path.Combine(configDir, Config.ConfigFilename)
    let baseConfig = Config.load configPath

    let config = new BehaviorSubject<FolderConfiguration>(baseConfig)

    let mutable allowDuplicates = baseConfig.AllowDuplicates
    let folders = ObservableCollection()
    let excludedFolders = ObservableCollection()
    let foldersChanged = new Subject<unit>()

    do
        folders.CollectionChanged.Add(fun _ -> foldersChanged.OnNext())
        excludedFolders.CollectionChanged.Add(fun _ -> foldersChanged.OnNext())

        baseConfig.Folders |> Seq.iter (fun folder ->
            FolderViewModel(folder, folders, foldersChanged)
            |> folders.Add
        )
        baseConfig.ExcludedFolders |> Seq.iter (fun folder ->
            FolderViewModel(folder, excludedFolders, foldersChanged)
            |> excludedFolders.Add
        )

        config.Skip(1).Subscribe(Config.save configPath) |> ignore

        foldersChanged
            .Debounce(TimeSpan.FromMilliseconds 600L)
            .Subscribe(fun () ->
                { config.Value with
                    Folders = folders |> Seq.map _.Path |> Seq.toArray
                    ExcludedFolders = excludedFolders |> Seq.map _.Path |> Seq.toArray }
                |> config.OnNext
            ) |> ignore

    member this.Config = config
    member this.AllowDuplicates
        with get () = allowDuplicates
        and set v =
            allowDuplicates <- v
            { config.Value with AllowDuplicates = v } |> config.OnNext

    member this.Folders = folders
    member this.ExcludedFolders = excludedFolders

    member this.AddFolder() =
        FolderViewModel(
            String.Empty,
            folders,
            foldersChanged
        ) |> folders.Add
    member this.AddExcludedFolder() =
        FolderViewModel(
            String.Empty,
            excludedFolders,
            foldersChanged
        ) |> excludedFolders.Add


type Settings() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
