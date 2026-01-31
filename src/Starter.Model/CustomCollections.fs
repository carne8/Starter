namespace Starter.Features.CustomCollections

open System.Collections.Generic
open System.Collections.Specialized
open System.ComponentModel

#nowarn 3261 // Disable nullness warning

/// An observable list, that fires event only when requested
type ObservableList<'T>(capacity: int) =
    inherit List<'T>(capacity)

    let collectionChanged = Event<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>()
    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    interface INotifyCollectionChanged with
        [<CLIEvent>]
        member this.CollectionChanged = collectionChanged.Publish

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish

    member this.NotifyChanged() =
        collectionChanged.Trigger(this, NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))
        propertyChanged.Trigger(this, PropertyChangedEventArgs(nameof this.Count))
