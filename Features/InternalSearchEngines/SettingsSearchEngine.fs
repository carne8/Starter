module Starter.Features.InternalSearchEngines

open FsToolkit.ErrorHandling
open Starter
open Starter.Features.Config
open Starter.SearchEngine

type SettingsSearchResult =
    { Id: string
      Name: string }
    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.LoadIcon() = null


type SettingsSearchEngine(baseConfig: Configuration) =
    inherit StaticSearchEngine("")

    static let id = System.Guid.NewGuid() |> string
    static let matchingString = "Options"

    let settingsViewModel = new ViewModels.SettingsViewModel(baseConfig)
    let mutable window =
        Avalonia.Threading.Dispatcher.UIThread.Invoke(fun _ ->
            Views.Settings(DataContext = settingsViewModel)
        )

    member _.Configuration = settingsViewModel.Configuration

    override this.DisplayName = "Settings"
    override this.Id = id
    override this.LoadResults() =
        { Id = "starter-options"; Name = matchingString }
        :> ISearchResult
        |> Array.singleton
        |> Task.singleton

    override this.SearchResultSelected _ =
        Avalonia.Threading.Dispatcher.UIThread.Post(fun _ ->
            try
                window.Show()
            with _ ->
                // Create new window
                let previousDataContext =
                    match window.DataContext with
                    | null -> failwith "Should not happen"
                    | dc -> dc :?> ViewModels.SettingsViewModel

                window <- Views.Settings(DataContext = previousDataContext)
                window.Show()
        )
