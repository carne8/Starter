namespace Starter.Spotify.SpotifyClient

open System.Net.Http
open System.Threading.Tasks
open Starter.Spotify
open Starter.Spotify.Log

type CurrentMusic() =
    inherit ObservableObject()

    let mutable musicName = "No name"
    let mutable artists = "Nobody"
    let mutable albumCover : string | null = null
    let mutable loading = true

    member this.MusicName
        with get () = musicName
        and set v =
            musicName <- v
            this.NotifyPropertyChanged(nameof this.MusicName)

    member this.Artists
        with get () = artists
        and set v =
            artists <- v
            this.NotifyPropertyChanged(nameof this.Artists)

    member this.AlbumCover
        with get () = albumCover
        and set v =
            albumCover <- v
            this.NotifyPropertyChanged(nameof this.AlbumCover)

    member this.Loading
        with get () = loading
        and set v =
            loading <- v
            this.NotifyPropertyChanged(nameof this.Loading)

    member this.SetNoMusic() =
        this.MusicName <- "No name"
        this.Artists <- "Nobody"
        this.AlbumCover <- "https://cataas.com/cat/says/No%20music"
        this.Loading <- false

    member this.SetMusic(musicInfo: SpotifyApi.MusicInfo) =
        this.MusicName <- musicInfo.Name
        this.Artists <- musicInfo.Artists
        this.AlbumCover <- musicInfo.AlbumCoverUrl
        this.Loading <- false

    member this.SetLoading() = this.Loading <- true


[<AutoOpen>]
module private Helpers =
    let inline runQuery (semaphore: System.Threading.SemaphoreSlim) f =
        Task.Run<unit>(fun () -> task {
            do! semaphore.WaitAsync()
            do! f ()
            semaphore.Release() |> ignore
        }) |> ignore

type SpotifyClient(auth: OAuth.AuthManager) =
    let httpClient = new HttpClient()

    let lock = new System.Threading.SemaphoreSlim(1)
    let currentMusic = CurrentMusic()

    member this.QuerySpotify() =
        runQuery lock (fun () -> task {
            currentMusic.SetLoading()
            match! SpotifyApi.getCurrentMusicInfo httpClient auth with
            | Error err -> logger.Error("Failed to query Spotify: {Error}", err)
            | Ok None -> currentMusic.SetNoMusic()
            | Ok (Some music) -> currentMusic.SetMusic(music)
        })

    member this.CurrentMusic = currentMusic
