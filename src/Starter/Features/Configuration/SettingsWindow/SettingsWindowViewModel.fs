namespace Starter.Features.Config.UI.SettingsWindow

open System
open System.Collections.Generic
open Starter.Features
open Starter.SearchEngine
open Starter.Features.Config

open Avalonia.Controls
open FluentAvalonia.UI.Controls
open FluentIcons.Common
open FluentIcons.Avalonia.Fluent

open ReactiveUI
open R3

type MenuItemVM =
    { Id: string
      Icon: IconSource | null
      Name: string
      Control: Control }

    static member create control (se: SearchEngine) =
        { Id = se.Id
          Icon = se.Icon |> StarterIconSource.buildIconSource
          Name = se.Name
          Control = control }

type WindowViewModel(baseConfig, searchEngines: Dictionary<string, SearchEngine> BehaviorSubject) =
    inherit ReactiveObject()

    let starterSettingsVM = new UI.StarterSettings.ViewModel(baseConfig, searchEngines)
    let starterSettingsMenuItem =
        { Id = "starter-settings"
          Icon = FluentIconSource(Icon = Icon.Settings, IconSize = IconSize.Size16)
          Name = "Starter settings"
          Control = UI.StarterSettings.StarterSettings(DataContext = starterSettingsVM) }

    let logsVM = LoggingView.LogsViewModel()
    let logsMenuItem =
        { Id = "starter-logs"
          Icon = FluentIconSource(Icon = Icon.DocumentText, IconSize = IconSize.Size16)
          Name = "Logs"
          Control = LoggingView.LogsView(DataContext = logsVM) }

    let mutable selectedPage = starterSettingsMenuItem
    let menuItems = new BehaviorSubject<_ array>(Array.empty)

    let sub =
        searchEngines.ObserveOnUIThreadDispatcher()
        |> Observable.subscribe (fun d ->
            d
            |> Seq.choose (fun kv ->
                menuItems.Value
                |> Array.tryFind (fun i -> i.Id = kv.Key)
                |> Option.map Some
                |> Option.defaultWith (fun () ->
                    match kv.Value.LoadSettingsControl() with
                    | null -> None
                    | control -> kv.Value |> MenuItemVM.create control |> Some
                )
            )
            |> Seq.sortBy _.Name
            |> Seq.append [ starterSettingsMenuItem; logsMenuItem ]
            |> Seq.toArray
            |> menuItems.OnNext
        )

    interface IDisposable with
        member _.Dispose() = sub.Dispose()

    member this.Configuration = starterSettingsVM.Configuration

    member this.MenuItems = menuItems
    member this.SelectedPage
        with get () = selectedPage
        and set v = this.RaiseAndSetIfChanged(&selectedPage, v) |> ignore

    member this.SelectSettingsPage() = this.SelectedPage <- starterSettingsMenuItem
    member this.SelectLogsPage() = this.SelectedPage <- logsMenuItem
