[<AutoOpen>]
module Starter.Spotify.ObservableObject

open System.ComponentModel

type ObservableObject () =
    let propertyChanged = Event<PropertyChangedEventHandler,PropertyChangedEventArgs>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish

    member this.NotifyPropertyChanged propertyName =
        propertyChanged.Trigger(this,PropertyChangedEventArgs(propertyName))
