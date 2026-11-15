namespace Starter.Spotify

open System.Threading.Tasks
open Starter.SearchEngine

module Log =
    let mutable logger: Serilog.ILogger = null
open Log

module Process =
    open System.Diagnostics

    let run proc args =
        ProcessStartInfo(
            FileName = proc,
            Arguments = args,
            UseShellExecute = true
        )
        |> Process.Start
        |> function null -> () | d -> d.Dispose()

module OAuth =
    open System
    open System.Web

    let private clientId = ""
    let private scope = "user-read-playback-state user-modify-playback-state user-read-currently-playing"
    let private redirectUri = "starter://spotify-callback"
    let private state = Random().GetHexString(16, true)
    let mutable code = None

    /// Requests user authorization
    let login () =
        let query = HttpUtility.ParseQueryString String.Empty
        query.Add("response_type", "code")
        query.Add("client_id", clientId)
        query.Add("scope", scope)
        query.Add("redirect_uri", redirectUri)
        query.Add("state", state)

        let uri = UriBuilder "https://accounts.spotify.com/authorize"
        uri.Query <- query.ToString()

        // Open URL
        Process.run (uri.ToString()) String.Empty

    let codeReceived url =
        match Uri.TryCreate(url, UriKind.Absolute) with
        | false, _ -> ()
        | true, uri ->
            try
                let query = HttpUtility.ParseQueryString uri.Query
                let code' = query.Get "code"
                let state' = query.Get "state"
                if state <> state' then
                    logger.Error("Unexpected state returned by Spotify api.")
                else
                    code <- Some code'
                    printfn "%A" code
            with _ -> ()


type LoginSearchResult() =
    inherit SearchResult()
    override this.Name = "Log in"
    override this.Description = "Open the browser to log you in"
    // TODO: override this.Icon = StarterIconSource.Empty

type SpotifySearchEngine() =
    inherit StaticSearchEngine()

    override this.Id = nameof SpotifySearchEngine
    override this.Name = "Spotify"
    override this.LoadResults() = ValueTask.FromResult [| LoginSearchResult() |]
    override this.SearchResultSelected(selectedSearchResult) =
        match selectedSearchResult with
        | :? LoginSearchResult -> OAuth.login ()
        | _ -> ()


type SpotifySearchEngineFactory(pluginDirectory) =
    inherit SearchEngineFactory(pluginDirectory)

    override this.LoadSearchEngineIds() = [| nameof SpotifySearchEngine |]
    override this.LoadSearchEngine(searchEngineId, pluginConfigDirectory, logger, clipboard) =
        Log.logger <- logger
        SpotifySearchEngine(), null

    override this.DeepLinkingPaths = [| "spotify-callback" |]
    override this.DeepLinkActivated(url) = OAuth.codeReceived url
