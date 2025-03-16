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


type SettingsSearchEngine(getConfig: unit -> Configuration) =
    inherit SearchEngineBase("")

    static let id = System.Guid.NewGuid() |> string
    static let matchingStrings = [| "Options"; "Settings" |]

    let mutable window = Views.Settings()

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
            let vm = new ViewModels.SettingsViewModel(getConfig())
            try
                window.DataContext <- vm
                window.Show()
            with _ ->
                // Dispose old view model
                window.DataContext
                :?> ViewModels.SettingsViewModel
                :> System.IDisposable
                |> _.Dispose()

                // Create new window
                window <- Views.Settings(DataContext = vm)
                window.Show()
        )
