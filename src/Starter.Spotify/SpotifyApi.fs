module Starter.Spotify.SpotifyApi

open System.Net
open System.Net.Http
open FsToolkit.ErrorHandling

type MusicInfo =
    { Name: string
      Artists: string
      AlbumCoverUrl: string }

type SpotifyMusicJson =
    { item:
        {| id: string
           name: string
           href: string
           duration_ms: int64
           artists: {| id: string; name: string |} array
           album:
            {| id: string
               name: string
               images:
                {| url: string
                   height: int
                   width: int |} array |} |} }

let getCurrentMusicInfo (http: HttpClient) (secret: Secret) =
    taskResult {
        use request =
            new HttpRequestMessage(
                HttpMethod.Get,
                "https://api.spotify.com/v1/me/player/currently-playing"
            )

        request.Headers.Add("Authorization", "Bearer " + secret.AccessToken)

        let! response =
            http.SendAsync(request)
            |> Task.catch
            |> Task.map Result.ofChoice
            |> TaskResult.mapError _.Message

        match response.StatusCode with
        | HttpStatusCode.NoContent -> return None
        | HttpStatusCode.OK ->
            let! json = response.Content.TryReadFromJsonAsync<SpotifyMusicJson>()

            let artists =
                json.item.artists |> Array.fold
                    (fun state artist ->
                        match state with
                        | "" -> artist.name
                        | _ -> $"{state}, {artist.name}"
                    )
                    ""

            let albumCover =
                json.item.album.images
                |> Array.maxBy _.width
                |> _.url

            return Some { Name = json.item.name
                          Artists = artists
                          AlbumCoverUrl = albumCover }
        | _ ->
            return!
                match response.ReasonPhrase with
                | null -> Error "Unknown error"
                | s -> Error s
    }
