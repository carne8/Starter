module Starter.Features.InternalSearchEngines

open Starter.SearchEngine
open Avalonia.Threading
open FluentAvalonia.UI.Controls
open FsToolkit.ErrorHandling

type SettingsSearchResult =
    { Id: string
      Name: string }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = "Starter settings"
        member this.Icon = StarterIconSource(Symbol.Settings)

type SettingsSearchEngine(searchEngines) =
    inherit StaticSearchEngine("")

    let vm = new Config.UI.SettingsWindow.WindowViewModel(searchEngines)
    let mutable window = Config.UI.SettingsWindow.WindowControl(DataContext = vm)

    static let id = nameof SettingsSearchEngine
    static let results: ISearchResult array =
        [| { Id = "starter-options"
             Name = "Options" }
           { Id = "starter-settings"
             Name = "Settings" } |]

    static member StaticId = id

    member this.Configuration = vm.Configuration

    override this.Name = "Options"
    override this.ShortName = "Options"
    override this.Id = id
    override this.Icon = StarterIconSource(Symbol.Settings)
    override this.LoadResults() = results |> Task.singleton
    override this.SearchResultSelected _ =
        Dispatcher.UIThread.Post(fun () ->
            vm.ResetSelectedPage()
            try
                window.Show()
                window.Activate()
            with _ ->
                window <- Config.UI.SettingsWindow.WindowControl(DataContext = vm)
                window.Show()
        )

    override this.LoadSettingsControl() = null
