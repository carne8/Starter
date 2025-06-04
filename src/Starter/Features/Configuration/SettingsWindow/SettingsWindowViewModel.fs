namespace Starter.Features.Config.UI.SettingsWindow

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

    let mutable selectedPage: MenuItemVM = starterSettingsMenuItem

    // Search engine settings pages
    let menuItems =
        searchEngines.ObserveOnUIThreadDispatcher()
        |> Observable.map (fun d ->
            d
            |> Seq.map (_.Value >> MenuItemVM.create)
            |> Seq.sortBy _.Name
            |> Seq.append [ starterSettingsMenuItem ]
            |> Seq.toArray
        )

    member this.MenuItems = menuItems
    member this.SelectedPage
        with get () = selectedPage
        and set v = this.RaiseAndSetIfChanged(&selectedPage, v) |> ignore

    member this.Save() = ()
