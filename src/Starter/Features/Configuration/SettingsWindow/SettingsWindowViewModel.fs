namespace Starter.Features.Config.UI.SettingsWindow

open System
open Starter.SearchEngine
open Starter.Features.Config

open System.Collections.Generic
open Avalonia.Controls
open FluentAvalonia.UI.Controls
open ReactiveUI
open R3

type MenuItemVM =
    { Id: string
      Icon: IconSource | null
      Name: string
      Control: Control }

    static member create (se: ISearchEngine) =
        { Id = se.Id
          Icon = se.Icon |> StarterIconSource.build
          Name = se.Name
          Control = se.LoadSettingsControl() }

type WindowViewModel(baseConfig, searchEngines: IDictionary<string, ISearchEngine> BehaviorSubject) =
    inherit ReactiveObject()

    let starterSettingsVM = new UI.StarterSettings.ViewModel(baseConfig, searchEngines)
    let starterSettingsMenuItem =
        { Id = "starter-settings"
          Icon = SymbolIconSource(Symbol = Symbol.Settings)
          Name = "Starter settings"
          Control = UI.StarterSettings.StarterSettings(DataContext = starterSettingsVM) }

    let mutable selectedPage = starterSettingsMenuItem
    let menuItems = new BehaviorSubject<_ array>(Array.empty)

    let sub =
        searchEngines.ObserveOnUIThreadDispatcher()
        |> Observable.subscribe (fun d ->
            d
            |> Seq.map (fun kv ->
                menuItems.Value
                |> Array.tryFind (fun i -> i.Id = kv.Key)
                |> Option.defaultWith (fun () -> kv.Value |> MenuItemVM.create)
            )
            |> Seq.sortBy _.Name
            |> Seq.append [ starterSettingsMenuItem ]
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

    member this.Save() =
        if selectedPage.Id = starterSettingsMenuItem.Id then
            starterSettingsVM.Save()
        else
            searchEngines.Value[selectedPage.Id].SaveSettings()
