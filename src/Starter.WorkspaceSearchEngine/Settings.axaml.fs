namespace Starter.WorkspaceSearchEngine.Views

open System.Collections.Generic
open Avalonia.Controls
open Avalonia.Markup.Xaml
open FluentAvalonia.UI.Controls
open ObservableCollections
open ReactiveUI
open Starter.SearchEngine
open Starter.WorkspaceSearchEngine
open Starter.WorkspaceSearchEngine.Logger

[<AutoOpen>]
module private Helpers =
    type StarterIconSource with
        /// Transform StarterIconSource in IconSource
        static member buildIconSource lightMode (iconSource: StarterIconSource) =
            match iconSource.Geometry with
            | null -> ImageIconSource(Source = iconSource.GetImage lightMode) :> IconSource
            | geo -> PathIconSource(Data = geo)

type WorkspaceSourceViewModel(source: WorkspaceSource, settings: Settings) =
    inherit ReactiveObject()

    let mutable show = source.ShowIfNoActivator

    member this.WorkspaceSource = source
    member this.Icon = source.Icon |> StarterIconSource.buildIconSource true // TODO: Set light/dark mode

    member this.SetShowIfNoActivatorValue(show') = show <- show'
    member this.Show
        with get () = show
        and set v =
            this.RaiseAndSetIfChanged(&show, v) |> ignore
            settings.ShowIfNoActivator[source.Id] <- v

type SettingsViewModel(workspaceSources: WorkspaceSource array, settings: Settings) =
    let workspaceSourceViewModels =
        workspaceSources
        |> Array.map (fun source -> WorkspaceSourceViewModel(source, settings))

    let onSettingsShowIfNoActivatorChanges =
        NotifyCollectionChangedEventHandler<KeyValuePair<_, _>>(fun args ->
            let workspaceSourceId = args.NewItem.Key
            let workspaceSourceVm =
                workspaceSourceViewModels
                |> Array.tryFind (fun vm -> vm.WorkspaceSource.Id = workspaceSourceId)

            match workspaceSourceVm with
            | None -> logger.Error($"A workspace source is present in the settings but not in the view model list: {workspaceSourceId}")
            | Some workspaceSourceVm ->
                let x = workspaceSourceVm.SuppressChangeNotifications()
                workspaceSourceVm.SetShowIfNoActivatorValue args.NewItem.Value
                x.Dispose()
        )

    do settings.ShowIfNoActivator.add_CollectionChanged(onSettingsShowIfNoActivatorChanges)

    override this.Finalize() =
        settings.ShowIfNoActivator.remove_CollectionChanged(onSettingsShowIfNoActivatorChanges)

    // --- Bindings ---
    member this.WorkspaceSources = workspaceSourceViewModels

type SettingsView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
