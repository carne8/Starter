module Starter.Features.InternalSearchEngines

open Starter.SearchEngine
open Avalonia.Threading
open FluentAvalonia.UI.Controls
open FsToolkit.ErrorHandling

let private settingsIcon = StarterIconSource(Symbol.Settings)
let private logsIcon = StarterIconSource(Symbol.Document)

type private TargetPage =
    | Settings
    | Logs

type private SettingsSearchResult =
    { Id: string
      Name: string
      Description: string
      TargetPage: TargetPage }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = this.Description
        member this.Icon =
            match this.TargetPage with
            | TargetPage.Settings -> settingsIcon
            | TargetPage.Logs -> logsIcon

type SettingsSearchEngine(searchEngines) =
    inherit StaticSearchEngine("")

    let vm = new Config.UI.SettingsWindow.WindowViewModel(searchEngines)
    let mutable window = Config.UI.SettingsWindow.WindowControl(DataContext = vm)

    static let id = nameof SettingsSearchEngine
    static let results: ISearchResult array =
        [| { Id = "starter-options"
             Name = "Options"
             Description = "Starter settings"
             TargetPage = TargetPage.Settings }
           { Id = "starter-settings"
             Name = "Settings"
             Description = "Starter settings"
             TargetPage = TargetPage.Settings }
           { Id = "starter-logs"
             Name = "Logs"
             Description = "Starter logs"
             TargetPage = TargetPage.Logs } |]

    static member StaticId = id

    member this.Configuration = vm.Configuration

    override this.Name = "Options"
    override this.ShortName = "Options"
    override this.Id = id
    override this.Icon = settingsIcon
    override this.LoadResults() = results |> Task.singleton
    override this.SearchResultSelected se =
        match se with
        | :? SettingsSearchResult as se ->
            Dispatcher.UIThread.Post(fun () ->
                match se.TargetPage with
                | TargetPage.Settings -> vm.SelectSettingsPage()
                | TargetPage.Logs -> vm.SelectLogsPage()

                try
                    window.Show()
                    window.Activate()
                with _ ->
                    window <- Config.UI.SettingsWindow.WindowControl(DataContext = vm)
                    window.Show()
            )
        | _ -> ()

    override this.LoadSettingsControl() = null
