namespace Starter.Features.CustomCollections

open System
open System.Collections
open System.Collections.Generic
open System.Collections.Specialized
open System.ComponentModel

/// An observable list, that fires events only when sorted
type ObservableList<'T>(capacity: int) =
    let list = new List<'T>(capacity)

    let propertyChanged = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()
    let collectionChanged = Event<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>()

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member this.PropertyChanged = propertyChanged.Publish

    interface INotifyCollectionChanged with
        [<CLIEvent>]
        member this.CollectionChanged = collectionChanged.Publish

    interface IList with
        member this.GetEnumerator(): IEnumerator = list.GetEnumerator()
        member this.Add(value) = (list :> IList).Add(value)
        member this.Clear() = (list :> IList).Clear()
        member this.Contains(value) = (list :> IList).Contains(value)
        member this.CopyTo(array, index) = (list :> IList).CopyTo(array, index)
        member this.IndexOf(value) = (list :> IList).IndexOf(value)
        member this.Insert(index, value) = (list :> IList).Insert(index, value)
        member this.Remove(value) = (list :> IList).Remove(value)
        member this.RemoveAt(index) = (list :> IList).RemoveAt(index)
        member this.Count = (list :> IList).Count
        member this.IsReadOnly = (list :> IList).IsReadOnly
        member this.Item
            with get index = (list :> IList)[index]
            and set index value = (list :> IList)[index] <- unbox value

        member this.IsFixedSize = (list :> IList).IsFixedSize
        member this.IsSynchronized = (list :> IList).IsSynchronized
        member this.SyncRoot = (list :> IList).SyncRoot

    #if DEBUG
    member this.List = list
    #endif

    member this.AddRange(range) = list.AddRange(range)
    member this.Clear() = list.Clear()
    member this.Sort(f) =
        list.Sort(Comparison<'T>(fun e1 e2 -> compare (f e1) (f e2)))
        collectionChanged.Trigger(this, NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset))
