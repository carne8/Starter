namespace Starter.Spotify

open System
open System.Collections.Generic
open System.Net.Http.Json
open System.Threading.Tasks
open Starter.SearchEngine
open FsToolkit.ErrorHandling

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

module SecretManager =
    open System.IO

    let [<Literal>] private configFileName = "spotify.credentials"

    type Secret =
        { AccessToken: string
          RefreshToken: string
          ExpirationDate: DateTimeOffset }

    let private getFilePath configDir = Path.Combine(configDir, configFileName)

    let writeSecret configDir secret =
        task {
            Directory.CreateDirectory configDir |> ignore // Ensure directory exists

            let filePath = getFilePath configDir
            use file = File.Open(filePath, FileMode.OpenOrCreate, FileAccess.Write)
            file.SetLength 0

            use text = new StreamWriter(file)
            do! text.WriteLineAsync(secret.AccessToken)
            do! text.WriteLineAsync(secret.RefreshToken)
            do! text.WriteLineAsync(secret.ExpirationDate.ToUnixTimeSeconds() |> string)
        }

    let readFile configDir =
        taskOption {
            let filePath = getFilePath configDir
            if not <| File.Exists filePath then do! None

            let! lines = File.ReadAllLinesAsync filePath
            if lines.Length < 3 then do! None

            let! expirationDate =
                lines[3]
                |> Int64.TryParse
                |> Option.ofPair
                |> Option.map DateTimeOffset.FromUnixTimeSeconds

            return { AccessToken = lines[0]
                     RefreshToken = lines[1]
                     ExpirationDate = expirationDate }
        }


module OAuth =
    open System.Web
    open System.Net.Http
    open System.Text

    type SpotifyAccessTokenResponse =
        { access_token: string
          refresh_token: string
          expires_in: int64 }

    let private clientId = ""
    let private clientSecret = ""
    let private scope = "user-read-playback-state user-modify-playback-state user-read-currently-playing"
    let private redirectUri = "starter://spotify-callback"
    let private state = Random().GetHexString(16, true)

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

    let requestAccessToken configDir code =
        task {
            use client = new HttpClient()

            let request =
                new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token")

            let auth =
                $"{clientId}:{clientSecret}"
                |> ASCIIEncoding.ASCII.GetBytes
                |> Convert.ToBase64String
            request.Headers.Add("Authorization", "Basic " + auth)

            request.Content <-
                new FormUrlEncodedContent([
                      KeyValuePair("code", code)
                      KeyValuePair("redirect_uri", redirectUri)
                      KeyValuePair("grant_type", "authorization_code")
                ])

            let! response = client.SendAsync(request)

            if not response.IsSuccessStatusCode then
                let! responseText = response.Content.ReadAsStringAsync()

                logger.Error(
                    "Failed to get access token: {ErrorCode} - {Error}",
                    response.StatusCode,
                    responseText
                )
            else
                let! json = response.Content.ReadFromJsonAsync<SpotifyAccessTokenResponse>()
                let secrets: SecretManager.Secret =
                    { AccessToken = json.access_token
                      RefreshToken = json.refresh_token
                      ExpirationDate = DateTimeOffset.Now + (TimeSpan.FromSeconds json.expires_in) }

                do! SecretManager.writeSecret configDir secrets
        }

    let codeReceived configDir url =
        match Uri.TryCreate(url, UriKind.Absolute) with
        | false, _ -> ()
        | true, uri ->
            try
                let query = HttpUtility.ParseQueryString uri.Query
                let code = query.Get "code"
                let state' = query.Get "state"
                if state <> state' then
                    logger.Error("Unexpected state returned by Spotify api.")
                else
                    requestAccessToken configDir code |> ignore
            with e ->
                logger.Error(e, "Failed to parse Spotify's response.")


type LoginSearchResult() =
    inherit SearchResult()
    override this.Name = "Log in"
    override this.Description = "Open the browser to log you in"
    // TODO: override this.Icon = StarterIconSource.Empty

type SpotifySearchEngine(configDir) =
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

    let mutable configDir = null

    override this.LoadSearchEngineIds() = [| nameof SpotifySearchEngine |]
    override this.LoadSearchEngine(searchEngineId, pluginConfigDirectory, logger, clipboard) =
        configDir <- pluginConfigDirectory
        Log.logger <- logger
        SpotifySearchEngine(pluginConfigDirectory), null

    override this.DeepLinkingPaths = [| "spotify-callback" |]
    override this.DeepLinkActivated(url) = OAuth.codeReceived configDir url
