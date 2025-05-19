module Starter.Features.InternalSearchEngines

open Avalonia.Media.Imaging
open FsToolkit.ErrorHandling
open Starter
open Starter.SearchEngine
open Starter.ViewModels

type SettingsSearchResult =
    { Id: string
      Name: string }
    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.LoadIcon() = null

type SettingsSearchEngine(config, searchEngines) =
    inherit StaticSearchEngine("")

    static let id = nameof SettingsSearchEngine
    static let matchingString = "Options"

    let settingsViewModel = new SettingsViewModel(config, searchEngines)
    let mutable window : Views.Settings option = None
    // Create the window only when opening is requested
    // This allows the view model to load the search engines correctly
    // (else, the settings search engine (the current one) doesn't appear in the settings)

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
                window.Value.Show()
            with _ ->
                // Create new window
                window <- Some <| Views.Settings(DataContext = settingsViewModel)
                window.Value.Show()
        )
