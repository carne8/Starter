namespace Starter.WorkspaceSearchEngine.Views

open Avalonia.Controls
open Avalonia.Markup.Xaml
open FluentAvalonia.UI.Controls
open ReactiveUI
open Starter.SearchEngine
open Starter.WorkspaceSearchEngine

[<AutoOpen>]
module private Helpers =
    type StarterIconSource with
        /// Transform StarterIconSource in IconSource
        static member buildIconSource lightMode (iconSource: StarterIconSource) =
            match iconSource.Geometry with
            | null -> FAImageIconSource(Source = iconSource.GetImage lightMode) :> FAIconSource
            | geo -> FAPathIconSource(Data = geo)

type WorkspaceSourceViewModel(source: WorkspaceSource, settings: Settings, changed: unit -> unit) =
    inherit ReactiveObject()

    member this.WorkspaceSource = source
    member this.Icon = source.Icon |> StarterIconSource.buildIconSource true // TODO: Set light/dark mode

    member this.SetShowIfNoActivatorValue(show) = source.ShowIfNoActivator <- show
    member this.Show
        with get () = source.ShowIfNoActivator
        and set v =
            source.ShowIfNoActivator <- v
            settings.ShowIfNoActivator[source.Id] <- v
            changed ()
            this.RaisePropertyChanged(nameof this.Show)

type SettingsViewModel(workspaceSources: WorkspaceSource array, settingsFilePath, settings: Settings) =
    let onWorkspaceSourceChanged () = settings |> Settings.saveSettings settingsFilePath

    let workspaceSourceViewModels =
        workspaceSources
        |> Array.map (fun source -> WorkspaceSourceViewModel(source, settings, onWorkspaceSourceChanged))

    // --- Bindings ---
    member this.WorkspaceSources = workspaceSourceViewModels

type SettingsView() as this =
    inherit UserControl()

    do this.InitializeComponent()

    member this.InitializeComponent() =
        AvaloniaXamlLoader.Load this
