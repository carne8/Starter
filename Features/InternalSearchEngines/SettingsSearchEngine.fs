module Starter.Features.InternalSearchEngines

open Starter
open Starter.Features.Config
open Starter.SearchEngine

type SettingsSearchResult =
    { Name: string }
    interface ISearchResult with
        member this.Id = ""
        member this.Name = this.Name
        member this.LoadIcon() = task { return null }


type SettingsSearchEngine(baseConfig: Configuration) =
    inherit SearchEngineBase("")

    static let id = System.Guid.NewGuid() |> string
    static let matchingStrings = [| "Options"; "Settings" |]

    let settingsViewModel = new ViewModels.SettingsViewModel(baseConfig)
    let mutable window = Views.Settings(DataContext = settingsViewModel)

    member _.Configuration = settingsViewModel.Configuration

    override this.DisplayName = "Settings"
    override this.Id = id
    override this.Search(query, _) =
        { new System.IObservable<_> with
            member _.Subscribe(obs) =
                matchingStrings
                |> Seq.choose (fun s ->
                    match s.ToLower().Contains(query) with
                    | false -> None
                    | true -> Some ({ Name = s } :> ISearchResult)
                )
                |> obs.OnNext

                { new System.IDisposable with
                    member _.Dispose() = () }
        }

    override this.SearchResultSelected _ =
        Avalonia.Threading.Dispatcher.UIThread.Post(fun _ ->
            try
                window.Show()
            with _ ->
                // Create new window
                window <- Views.Settings(DataContext = (window.DataContext :?> ViewModels.SettingsViewModel))
                window.Show()
        )
