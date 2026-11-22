module Starter.Spotify.Core

open System
open System.Threading.Tasks
open Avalonia.Controls.Templates
open Starter.SearchEngine
open FsToolkit.ErrorHandling

module SecretManager =
    open System.IO

    let [<Literal>] private configFileName = "spotify.credentials"
    let private getFilePath configDir = Path.Combine(configDir, configFileName)

    let writeSecret configDir secret =
        task {
            Directory.CreateDirectory configDir |> ignore // Ensure directory exists

            let filePath = getFilePath configDir
            if File.Exists filePath then File.Delete filePath

            use file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write)
            use text = new StreamWriter(file)
            do! text.WriteLineAsync(secret.AccessToken)
            do! text.WriteLineAsync(secret.RefreshToken)
            do! text.WriteLineAsync(secret.ExpirationDate.ToUnixTimeSeconds() |> string)
        }

    let readSecret configDir =
        taskOption {
            let filePath = getFilePath configDir
            if not <| File.Exists filePath then do! None

            let! lines = File.ReadAllLinesAsync filePath
            if lines.Length < 3 then do! None

            try
                let! expirationDate =
                    lines[2]
                    |> Int64.TryParse
                    |> Option.ofPair
                    |> Option.map DateTimeOffset.FromUnixTimeSeconds

                return { AccessToken = lines[0]
                         RefreshToken = lines[1]
                         ExpirationDate = expirationDate }
            with _ ->
                return! None
        }


type LoginSearchResult() =
    inherit SearchResult()
    override this.Name = "Log in to Spotify"
    override this.Description = "Open the browser to log you in"
    override this.Keywords = [| "spotify"; "music" |]
    // TODO: override this.Icon = StarterIconSource.Empty

type SpotifySearchResult(secret) =
    inherit ControlSearchResult()

    static let dataTemplate =
        FuncDataTemplate<ViewModels.PlaybackControllerViewModel>(fun vm _ ->
            Views.PlaybackController()
        )

    let dataContext = ViewModels.PlaybackControllerViewModel(secret)

    override this.ControlDataTemplate = dataTemplate
    override this.ControlDataContext = dataContext
    override this.Name = $"Playing: {secret}"
    override this.Keywords = [| "playing"; "spotify"; "music"; "now" |]
    // TODO: override this.Icon = StarterIconSource.Empty

[<RequireQualifiedAccess>]
type State =
    | LoadingSecrets
    | LoginRequired
    | LoggingIn
    | LoggedIn of Secret

type SpotifySearchEngine(configDir) as this =
    inherit StaticSearchEngine()

    let mutable state = State.LoadingSecrets

    let loginResults = [| LoginSearchResult() :> SearchResult |]
    let results secret = [| SpotifySearchResult(secret) :> SearchResult |]

    do
        OAuth.loggedIn.Add(fun secret ->
            Task.Run<unit>(fun () -> SecretManager.writeSecret configDir secret) |> ignore
            state <- State.LoggedIn secret
            this.NotifyResultsChanged()
        )

        // Login / Get tokens
        Task.Run<unit>(fun () -> task {
            match! SecretManager.readSecret configDir with
            | None -> state <- State.LoginRequired

            | Some secret when DateTimeOffset.Now > secret.ExpirationDate ->  // Outdated tokens
                match! OAuth.refreshAccessToken secret with
                | None -> state <- State.LoginRequired
                | Some refreshedSecret ->
                    state <- State.LoggedIn refreshedSecret
                    Task.Run<unit>(fun () -> SecretManager.writeSecret configDir refreshedSecret) |> ignore

            | Some secret -> // Valid tokens
                state <- State.LoggedIn secret

            this.NotifyResultsChanged()
        })
        |> ignore

    member this.NotifyResultsChanged() =
        match state with
        | State.LoadingSecrets -> this.OnResultsChanged Seq.empty
        | State.LoginRequired
        | State.LoggingIn -> this.OnResultsChanged loginResults
        | State.LoggedIn secrets -> this.OnResultsChanged(results secrets)


    override this.Id = nameof SpotifySearchEngine
    override this.Name = "Spotify"
    override this.LoadResults() = ValueTask.FromResult Seq.empty
    override this.SearchResultSelected(selectedSearchResult) =
        match selectedSearchResult with
        | :? LoginSearchResult -> OAuth.login ()
        | _ -> ()


type SpotifySearchEngineFactory(pluginDirectory) =
    inherit SearchEngineFactory(pluginDirectory)

    override this.LoadSearchEngineIds() = [| nameof SpotifySearchEngine |]
    override this.LoadSearchEngine(searchEngineId, configDir, logger, clipboard) =
        Log.logger <- logger
        SpotifySearchEngine(configDir), null

    override this.DeepLinkingPaths = [| "spotify-callback" |]
    override this.DeepLinkActivated(url) = OAuth.codeReceived url
