namespace Starter.Spotify.ViewModels

open System
open System.ComponentModel
open System.Net.Http
open System.Threading.Tasks

open Starter.Spotify
open Starter.Spotify.Log
open Avalonia.Threading

type ViewModel () =
    let propertyChanged = Event<PropertyChangedEventHandler,PropertyChangedEventArgs>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish

    member this.NotifyPropertyChanged propertyName =
        propertyChanged.Trigger(this,PropertyChangedEventArgs(propertyName))

type PlaybackControllerViewModel(secret: Secret) as this =
    inherit ViewModel()

    let mutable albumCoverUrl: string = unbox null
    let mutable musicName: string = unbox null
    let mutable artists: string = unbox null

    do this.Init()

    member this.AlbumCoverUrl
        with get () = albumCoverUrl
        and set v =
            albumCoverUrl <- v
            this.NotifyPropertyChanged(nameof this.AlbumCoverUrl)

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

    member this.Init() =
        Task.Run<unit>(fun () -> task {
            use client = new HttpClient()

            while true do
                let! musicInfoRes = SpotifyApi.getCurrentMusicInfo client secret

                match musicInfoRes with
                | Ok None -> Dispatcher.UIThread.Post(fun () -> this.Clear())
                | Ok (Some musicInfo) -> Dispatcher.UIThread.Post(fun () -> this.SetMusic musicInfo)
                | Error error ->
                    logger.Error("Failed to get currently playing music: {Error}", error)

                do! Task.Delay(TimeSpan.FromSeconds 3L)
        })
        |> ignore

    member this.Clear() =
        this.MusicName <- "Not playing"
        this.Artists <- "Nobody"
        this.AlbumCoverUrl <- "https://cataas.com/cat/says/No%20music"

    member this.SetMusic musicInfo =
        this.MusicName <- musicInfo.Name
        this.Artists <- musicInfo.Artists
        this.AlbumCoverUrl <- musicInfo.AlbumCoverUrl
