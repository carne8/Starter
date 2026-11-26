module Starter.Spotify.OAuth

open System
open System.Collections.Generic
open System.Net.Http
open System.Threading
open System.Threading.Tasks
open System.Web
open System.Text
open System.Security.Cryptography

open FsToolkit.ErrorHandling
open Starter.Spotify.Log

let private loggedInEvent = Event<Secret>()

let loggedIn = loggedInEvent.Publish

type SpotifyAccessTokenResponse =
    { access_token: string
      refresh_token: string
      expires_in: int64 }

let private clientId = ""
let private scope = "user-read-playback-state user-modify-playback-state user-read-currently-playing"
let private redirectUri = "starter://spotify-callback"
let private state = Random().GetHexString(16, true)

let mutable currentCodeVerifier : string = unbox null
let generateCodeVerifier () =
    currentCodeVerifier <-
        RandomNumberGenerator.GetString(
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789",
            64
        )
    currentCodeVerifier

let createCodeChallenge =
    generateCodeVerifier
    >> ASCIIEncoding.ASCII.GetBytes
    >> SHA256.HashData // TODO: Make it use async (but it needs a stream)
    >> Convert.ToBase64String
    >> _.Replace("=", "").Replace('+', '-').Replace('/', '_')


/// Requests user authorization
let login () =
    let query = HttpUtility.ParseQueryString String.Empty
    query.Add("response_type", "code")
    query.Add("client_id", clientId)
    query.Add("scope", scope)
    query.Add("redirect_uri", redirectUri)
    query.Add("state", state)
    query.Add("code_challenge_method", "S256")
    query.Add("code_challenge", createCodeChallenge ())

    let uri = UriBuilder "https://accounts.spotify.com/authorize"
    match query.ToString() with
    | null -> logger.Error("Failed to redirect user to Spotify connection page: Failed to create url.")
    | query ->
        uri.Query <- query

        // Open URL
        Process.run (uri.ToString()) String.Empty

let requestAccessToken code =
    taskOption {
        use client = new HttpClient()

        let request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "https://accounts.spotify.com/api/token"
            )

        request.Content <-
            new FormUrlEncodedContent([
                KeyValuePair("grant_type", "authorization_code")
                KeyValuePair("code", code)
                KeyValuePair("redirect_uri", redirectUri)
                KeyValuePair("client_id", clientId)
                KeyValuePair("code_verifier", currentCodeVerifier)
            ])

        try
            let! response = client.SendAsync(request)

            if not response.IsSuccessStatusCode then
                let! responseText = response.Content.ReadAsStringAsync()

                logger.Error(
                    "Failed to get access token: {ErrorCode} - {Error}",
                    response.StatusCode,
                    responseText
                )
                return! None
            else
                let! json =
                    response.Content.TryReadFromJsonAsync<SpotifyAccessTokenResponse>()
                    |> Task.map (
                        Result.teeError (fun err -> logger.Error(err))
                        >> Result.toOption
                    )
                return
                    { AccessToken = json.access_token
                      RefreshToken = json.refresh_token
                      ExpirationDate = DateTimeOffset.Now + (TimeSpan.FromSeconds json.expires_in) }
        with exn ->
            logger.Error(exn, "Failed to request access token.")
            return! None
    }

let refreshAccessToken (secret: Secret) =
    taskOption {
        use client = new HttpClient()

        use request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "https://accounts.spotify.com/api/token"
            )

        request.Content <-
            new FormUrlEncodedContent([
                KeyValuePair("grant_type", "refresh_token")
                KeyValuePair("refresh_token", secret.RefreshToken)
                KeyValuePair("client_id", clientId)
            ])

        try
            let! response = client.SendAsync(request)

            if not response.IsSuccessStatusCode then
                let! responseText = response.Content.ReadAsStringAsync()

                logger.Error(
                    "Failed to refresh token: {ErrorCode} - {Error}",
                    response.StatusCode,
                    responseText
                )
                return! None
            else
                let! json =
                    response.Content.TryReadFromJsonAsync<SpotifyAccessTokenResponse>()
                    |> Task.map (
                        Result.teeError (fun err -> logger.Error(err))
                        >> Result.toOption
                    )

                return
                    { AccessToken = json.access_token
                      RefreshToken =
                          match String.IsNullOrEmpty json.refresh_token with
                          | true -> secret.RefreshToken
                          | false -> json.refresh_token
                      ExpirationDate = DateTimeOffset.Now + (TimeSpan.FromSeconds json.expires_in) }
        with exn ->
            logger.Error(exn, "Failed to refresh token.")
            return! None
    }

let codeReceived url =
    match Uri.TryCreate(url, UriKind.Absolute) with
    | false, _
    | true, null -> ()
    | true, uri ->
        voption {
            let uri = unbox<Uri> uri // Prevents nullable warnings

            let! code, state' =
                try
                    let query = HttpUtility.ParseQueryString uri.Query
                    match query.Get "code", query.Get "state" with
                    | null, _
                    | _, null -> ValueNone
                    | o -> ValueSome o
                with e ->
                    logger.Error(e, "Failed to parse Spotify's response.")
                    ValueNone

            if state <> state' then
                logger.Error("Unexpected state returned by Spotify api.")
                do! ValueNone

            Task.Run<unit>(fun () ->
                code
                |> requestAccessToken
                |> Task.map (Option.iter loggedInEvent.Trigger)
            ) |> ignore
        } |> ignore

type AuthManager(
    secret: Secret option,
    tryGetSavedSecret: unit -> Task<Secret option>,
    saveSecret: Secret -> Task<unit>
    )
    =
    let currentSecretLock = obj()
    let mutable currentSecret = secret

    member this.RequestUserToLogin() =
        let tcs = TaskCompletionSource<Secret>()

        // Cancel after 3 minutes
        let cts = new CancellationTokenSource(TimeSpan.FromMinutes 3L)
        cts.Token.Register(fun () -> tcs.TrySetCanceled() |> ignore) |> ignore

#nowarn 40
        let rec sub: IDisposable = loggedIn.Subscribe(fun newSecret ->
            if tcs.TrySetResult(newSecret) then
                lock currentSecretLock (fun () -> currentSecret <- Some newSecret)
            else
                logger.Error("Error while logging in.")

            // TODO: Save token
            // Task.Run<unit>(fun () -> SecretManager.writeSecret configDir secret) |> ignore
            sub.Dispose()
        )
#warnon 40

        login ()
        tcs.Task |> Task.bind (fun newSecret ->
            task {
                do! saveSecret newSecret
                return newSecret
            }
        )

    member this.HasValidAccessToken =
        lock currentSecretLock (fun () ->
            currentSecret
            |> Option.filter (fun s -> s.ExpirationDate > DateTimeOffset.Now)
            |> Option.isSome
        )

    member this.AccessToken =
        task {
            match lock currentSecretLock (fun () -> currentSecret) with
            | None ->
                let! savedSecret = tryGetSavedSecret ()
                lock currentSecretLock (fun () -> currentSecret <- savedSecret)
                return! this.ReuseToken(savedSecret)

            | Some secret -> return! this.ReuseToken(Some secret)
        }

    member this.ReuseToken(s) =
        match s with
        | Some s when s.ExpirationDate > DateTimeOffset.Now ->
            s.AccessToken
            |> Some
            |> Task.singleton

        | Some s ->
            task {
                let! newSecret =
                    task {
                        match! refreshAccessToken s with
                        | None -> return! this.RequestUserToLogin()
                        | Some newSecret ->
                            lock currentSecretLock (fun () -> currentSecret <- Some newSecret)
                            do! saveSecret newSecret
                            return newSecret
                    }

                return Some newSecret.AccessToken
            }

        | None ->
            task {
                let! newSecret = this.RequestUserToLogin()
                return Some newSecret.AccessToken
            }
