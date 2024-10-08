﻿namespace FSharp.Control.Reactive

open System
open System.Collections.Generic
open System.Threading
open System.Reactive
open System.Reactive.Linq
open System.Reactive.Concurrency
open System.Reactive.Disposables


[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Builders =

    /// An Observable computation builder.
    type ObservableBuilder() =
        member __.Bind(m: IObservable<_>, f: _ -> IObservable<_>) = m.SelectMany(f)
        member __.Combine(comp1, comp2) = Observable.Concat(comp1, comp2)
        member __.Delay(f: _ -> IObservable<_>) = Observable.Defer(fun _ -> f())
        member __.Zero() = Observable.Empty(Scheduler.CurrentThread :> IScheduler)
        member __.For(sequence, body) = Observable.For(sequence, Func<_,_> body)
        member __.TryWith(m: IObservable<_>, h: Exception -> IObservable<_>) = Observable.Catch(m, h)
        member __.TryFinally(m, compensation) = Observable.Finally(m, Action compensation)
        member __.Using(res: #IDisposable, body) = Observable.Using((fun () -> res), Func<_,_> body)
        member __.While(guard, m: IObservable<_>) = Observable.While(Func<_> guard, m)
        member __.Yield(x) = Observable.Return(x, Scheduler.CurrentThread)
        member __.YieldFrom m : IObservable<_> = m
        [<Obsolete("Use Yield. Return will be removed in an upcoming version of FSharp.Control.Reactive.")>]
        member inline __.Return(x) = __.Yield(x)
        [<Obsolete("Use Yield. Return will be removed in an upcoming version of FSharp.Control.Reactive.")>]
        member inline __.ReturnFrom m = __.YieldFrom m

    let observe = ObservableBuilder()

    /// A reactive query builder.
    /// See http://mnajder.blogspot.com/2011/09/when-reactive-framework-meets-f-30.html
    type RxQueryBuilder() =
        member __.For (s:IObservable<_>, body : _ -> IObservable<_>) = s.SelectMany(body)
        [<CustomOperation("select", AllowIntoPattern=true)>]
        member __.Select (s:IObservable<_>, [<ProjectionParameter>] selector : _ -> _) = s.Select(selector)
        [<CustomOperation("where", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member __.Where (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool ) = s.Where(predicate)
        [<CustomOperation("takeWhile", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member __.TakeWhile (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool ) = s.TakeWhile(predicate)
        [<CustomOperation("take", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member __.Take (s:IObservable<_>, count: int) = s.Take(count)
        [<CustomOperation("skipWhile", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member __.SkipWhile (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool ) = s.SkipWhile(predicate)
        [<CustomOperation("skip", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member __.Skip (s:IObservable<_>, count: int) = s.Skip(count)
        member __.Zero () = Observable.Empty(Scheduler.CurrentThread :> IScheduler)
        member __.Yield (value) = Observable.Return(value)
        [<CustomOperation("count")>]
        member __.Count (s:IObservable<_>) = Observable.Count(s)
        [<CustomOperation("all")>]
        member __.All (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool ) = s.All(new Func<_,bool>(predicate))
        [<CustomOperation("contains")>]
        member __.Contains (s:IObservable<_>, key) = s.Contains(key)
        [<CustomOperation("distinct", MaintainsVariableSpace=true, AllowIntoPattern=true)>]
        member __.Distinct (s:IObservable<_>) = s.Distinct()
        [<CustomOperation("exactlyOne")>]
        member __.ExactlyOne (s:IObservable<_>) = s.SingleAsync()
        [<CustomOperation("exactlyOneOrDefault")>]
        member __.ExactlyOneOrDefault (s:IObservable<_>) = s.SingleOrDefaultAsync()
        [<CustomOperation("find")>]
        member __.Find (s:IObservable<_>, [<ProjectionParameter>] predicate : _ -> bool) = s.FirstAsync(new Func<_,bool>(predicate))
        [<CustomOperation("head")>]
        member __.Head (s:IObservable<_>) = s.FirstAsync()
        [<CustomOperation("headOrDefault")>]
        member __.HeadOrDefault (s:IObservable<_>) = s.FirstOrDefaultAsync()
        [<CustomOperation("last")>]
        member __.Last (s:IObservable<_>) = s.LastAsync()
        [<CustomOperation("lastOrDefault")>]
        member __.LastOrDefault (s:IObservable<_>) = s.LastOrDefaultAsync()
        [<CustomOperation("maxBy")>]
        member __.MaxBy (s:IObservable<'a>,  [<ProjectionParameter>] valueSelector : 'a -> 'b) = s.MaxBy(new Func<'a,'b>(valueSelector))
        [<CustomOperation("minBy")>]
        member __.MinBy (s:IObservable<'a>,  [<ProjectionParameter>] valueSelector : 'a -> 'b) = s.MinBy(new Func<'a,'b>(valueSelector))
        [<CustomOperation("nth")>]
        member __.Nth (s:IObservable<'a>,  index ) = s.ElementAt(index)
        [<CustomOperation("sumBy")>]
        member inline __.SumBy (s:IObservable<_>,[<ProjectionParameter>] valueSelector : _ -> _) = s.Select(valueSelector).Aggregate(Unchecked.defaultof<_>, new Func<_,_,_>( fun a b -> a + b))
        [<CustomOperation("groupBy", AllowIntoPattern=true)>]
        member __.GroupBy (s:IObservable<_>,[<ProjectionParameter>] keySelector : _ -> _) = s.GroupBy(new Func<_,_>(keySelector))
        [<CustomOperation("groupValBy", AllowIntoPattern=true)>]
        member __.GroupValBy (s:IObservable<_>,[<ProjectionParameter>] resultSelector : _ -> _,[<ProjectionParameter>] keySelector : _ -> _) = s.GroupBy(new Func<_,_>(keySelector),new Func<_,_>(resultSelector))
        [<CustomOperation("join", IsLikeJoin=true)>]
        member __.Join (s1:IObservable<_>,s2:IObservable<_>, [<ProjectionParameter>] s1KeySelector : _ -> _,[<ProjectionParameter>] s2KeySelector : _ -> _,[<ProjectionParameter>] resultSelector : _ -> _) = s1.Join(s2,new Func<_,_>(s1KeySelector),new Func<_,_>(s2KeySelector),new Func<_,_,_>(resultSelector))
        [<CustomOperation("groupJoin", AllowIntoPattern=true)>]
        member __.GroupJoin (s1:IObservable<_>,s2:IObservable<_>, [<ProjectionParameter>] leftDurationSelector : _ -> _,[<ProjectionParameter>] rightDurationSelector : _ -> _,[<ProjectionParameter>] resultSelector : _ -> _) = s1.GroupJoin(s2,new Func<_,_>(leftDurationSelector),new Func<_,_>(rightDurationSelector),new Func<_,_,_>(resultSelector))
        [<CustomOperation("zip", IsLikeZip=true)>]
        member __.Zip (s1:IObservable<_>,s2:IObservable<_>,[<ProjectionParameter>] resultSelector : _ -> _) = s1.Zip(s2,new Func<_,_,_>(resultSelector))
        //[<CustomOperation("forkJoin", IsLikeZip=true)>]
        //member __.ForkJoin (s1:IObservable<_>,s2:IObservable<_>,[<ProjectionParameter>] resultSelector : _ -> _) = s1.ForkJoin(s2,new Func<_,_,_>(resultSelector))
        [<CustomOperation("iter")>]
        member __.Iter(s:IObservable<_>, [<ProjectionParameter>] selector : _ -> _) = s.Do(selector)

    let rxquery = RxQueryBuilder()


/// The Reactive module provides operators for working with IObservable<_> in F#.
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Observable =

    type Observer with
        /// Creates an observer from the specified onNext function.
        static member Create(onNext) =
            Observer.Create(Action<_> onNext)

        /// Creates an observer from the specified onNext and onError functions.
        static member Create(onNext, onError) =
            Observer.Create(Action<_> onNext, Action<_> onError)

        /// Creates an observer from the specified onNext and onCompleted functions.
        static member Create(onNext, onCompleted) =
            Observer.Create(Action<_> onNext, Action onCompleted)

        /// Creates an observer from the specified onNext, onError, and onCompleted functions.
        static member Create(onNext, onError, onCompleted) =
            Observer.Create(Action<_> onNext, Action<_> onError, Action onCompleted)

        /// Creates an observer that ignores the incoming emits from 'OnNext', 'OnError', and 'OnCompleted'.
        static member Empty =
            (fun () -> Observer.Create(Action<_> ignore, Action<_> ignore, Action (fun () -> ()))) ()

    type Observable with
        /// Creates an observable sequence from the specified Subscribe method implementation.
        static member Create (subscribe: IObserver<'T> -> unit -> unit) =
            let subscribe o =
                let m = subscribe o
                Action(m)
            Observable.Create(subscribe)

        /// Creates an observable sequence from the specified asynchronous Subscribe method implementation.
        static member CreateAsync (subscribe: IObserver<'T> -> Async<IDisposable>) =
            Observable.Create(subscribe >> Async.StartAsTask)

    type IObservable<'T> with
        /// Subscribes to the Observable with just a next-function.
        member this.Subscribe(onNext: 'T -> unit) =
            this.Subscribe(Action<_> onNext)

        /// Subscribes to the Observable with a next and an error-function.
        member this.Subscribe(onNext: 'T -> unit, onError: exn -> unit) =
            this.Subscribe(Action<_> onNext, Action<exn> onError)

        /// Subscribes to the Observable with a next and a completion callback.
        member this.Subscribe(onNext: 'T -> unit, onCompleted: unit -> unit) =
            this.Subscribe(Action<_> onNext, Action onCompleted)

        /// Subscribes to the Observable with all 3 callbacks.
        member this.Subscribe(onNext, onError, onCompleted) =
            this.Subscribe(Action<_> onNext, Action<_> onError, Action onCompleted)


(***************************************************************
 * F# combinator wrappers for Rx extensions
 ***************************************************************)


    /// Applies an accumulator function over an observable sequence, returning the
    /// result of the aggregation as a single element in the result sequence
    let aggregate accumulator source =
        Observable.Aggregate(source, Func<_,_,_> accumulator )

    /// Determines whether all elements of an observable satisfy a predicate
    let all pred source =
        Observable.All(source, Func<_,_> pred )


    /// Returns the observable sequence that reacts first
    let amb second first = Observable.Amb(first, second)


    /// Propagates the observable sequence that reacts first
    let ambSeq (source:seq<IObservable<'T>>) = Observable.Amb( source )


    /// Propagates the observable sequence that reacts first
    let ambArray (source:IObservable<'T>[]) = Observable.Amb( source  )


    /// Determines whether an observable sequence contains any elements
    let any  (source:IObservable<'Source>) : IObservable<bool> =
        Observable.Any(source)


    /// Hides the identy of an observable sequence
    let asObservable source : IObservable<'Source>=
        Observable.AsObservable( source )


    /// Binds an observable to generate a subsequent observable.
    let bind (f: 'T -> IObservable<'TNext>) (m: IObservable<'T>) = m.SelectMany(Func<_,_> f)


    /// Lifts the values of f and m and applies f to m, returning an IObservable of the result.
    let apply f m = f |> bind (fun f' -> m |> bind (fun m' -> Observable.Return(f' m')))


    /// Matches when both observable sequences have an available value
    let both second first = Observable.And(first, second)

    // #region Buffers


    let buffer (bufferClosingSelector:IObservable<'BufferClosing>) source =
        Observable.Buffer(source, bufferClosingSelector)


    /// Projects each element of an observable sequence into
    /// consequtive non-overlapping buffers based on a sequence of boundary markers
    let bufferBounded (boundaries:IObservable<'BufferClosing>) source : IObservable<IList<'T>>=
        Observable.Buffer(source, boundaries)


    /// Projects each element of an observable sequence into
    /// consequtive non-overlapping buffers produced based on count information
    let bufferCount (count:int) source =
        Observable.Buffer(source, count)


    /// Projects each element of an observable sequence into zero or more buffers
    /// which are produced based on element count information
    let bufferCountSkip (count:int) (skip:int) source =
        Observable.Buffer(source,count, skip)


    /// Projects each element of an observable sequence into
    /// consequtive non-overlapping buffers produced based on timing information
    let bufferSpan (timeSpan:TimeSpan) source =
        Observable.Buffer(source, timeSpan)


    /// Projects each element of an observable sequence into consecutive non-overlapping buffers
    /// which are produced based on timing information, using the specified scheduler to run timers.
    let bufferSpanOn (scheduler:IScheduler) timeSpan source =
        Observable.Buffer (source, timeSpan, scheduler)


    /// Projects each element of an observable sequence into a buffer that goes
    /// sent out when either it's full or a specific amount of time has elapsed
    /// Analogy - A boat that departs when it's full or at its scheduled time to leave
    let bufferSpanCount (timeSpan:TimeSpan) (count:int) source =
        Observable.Buffer(source, timeSpan, count)


    /// Projects each element of an observable sequence into a buffer that's sent out
    /// when either it's full or a given amount of time has elapsed, using the specified scheduler to run timers.
    /// Analogy - A ferry leaves the dock when all the seats are taken, or at the scheduled time or departure,
    /// whichever event occurs first.
    let bufferSpanCountOn (scheduler:IScheduler) (timeSpan:TimeSpan) (count:int) source =
        Observable.Buffer(source, timeSpan, count, scheduler)


    /// Projects each element of an observable sequence into zero of more buffers.
    /// bufferOpenings - observable sequence whose elements denote the opening of each produced buffer
    /// bufferClosing - observable sequence whose elements denote the closing of each produced buffer
    let bufferFork  ( bufferOpenings:IObservable<'BufferOpening>)
                    ( bufferClosingSelector: 'BufferOpening ->IObservable<'T> ) source =
        Observable.Buffer( source, bufferOpenings,Func<_,_> bufferClosingSelector)


    /// Projects each element of an observable sequence into
    /// zero or more buffers produced based on timing information
    let bufferSpanShift (timeSpan:TimeSpan) (timeShift:TimeSpan) source =
        Observable.Buffer(source, timeSpan, timeShift)


    /// Projects each element of an observable sequence into
    /// zero or more buffers which are produced based on timing information,
    /// using the specified scheduler to run timers.
    let bufferSpanShiftOn (scheduler:IScheduler) (timeSpan:TimeSpan) (timeShift:TimeSpan) source =
        Observable.Buffer(source, timeSpan, timeShift, scheduler)

    // #endregion


    /// Converts the elements of the sequence to the specified type
    let cast<'CastType> (source) =
        Observable.Cast<'CastType>(source)


    /// Uses selector to determine which source in sources to return,
    /// choosing an empty sequence if no match is found
    let case selector sources =
        Observable.Case( Func<_> selector, sources )


    /// Uses selector to determine which source in sources to return,
    /// choosing defaulSource if no match is found
    let caseDefault selector (defaulSource:IObservable<'Result>) (sources:IDictionary<'Value,IObservable<'Result>>) =
        Observable.Case( Func<'Value> selector, sources, defaulSource )


    /// Uses selector to determine which source in sources to return,
    /// choosing an empty sequence on the specified scheduler if no match is found.
    let caseOn (scheduler:IScheduler) selector sources =
        Observable.Case (selector, sources, scheduler)

    /// Continues an observable sequence that is terminated
    /// by an exception with the next observable sequence.
    let catch (second: IObservable<'T>) first =
        Observable.Catch(first, second)


    /// Continues an observable sequence that is terminated by an exception
    /// with an optional as result type.
    let catchOption (source : IObservable<_>) =
        let some = source.Select(Func<_, _> Some)
        let none _ = Observable.Return None
        Observable.Catch(some, none)


    /// Continues an observable sequence that is terminated by an exception of
    /// the specified type with the observable sequence produced by the handler,
    /// wrapped in a 'Result' type.
    let catchResult handler (source : IObservable<_>)  =
        let normal = source.Select(Func<_, _> Result.Ok)
        let error ex = handler ex |> fun (o : IObservable<_>) -> o.Select(Func<_, _> Result.Error)
        Observable.Catch(normal, error)


    /// Continues an observable sequence that is terminated by an exception of
    /// the specified type with the observable sequence produced by the handler.
    let catchWith handler source =
        Observable.Catch( source,Func<_,_> handler )


    /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
    let catchSeq (sources:seq<IObservable<'T>>) =
        Observable.Catch(sources)


    /// Continues an observable sequence that is terminated by an exception with the next observable sequence.
    let catchArray (sources:IObservable<'T>[]) =
        Observable.Catch(sources)


    /// Produces an enumerable sequence of consequtive (possibly empty) chunks of the source observable
    let chunkify<'Source> source : seq<IList<'Source>> =
        Observable.Chunkify<'Source>( source )


    /// Concatenates the observable sequences obtained by applying the map for each element in the given enumerable
    let collect   ( map )( source:seq<'Source> ) : IObservable<'Result> =
        Observable.For( source, Func<'Source, IObservable<'Result>> map )



    /// Produces an enumerable sequence that returns elements collected/aggregated from the source sequence between consecutive iterations.
    /// merge - Merges a sequence element with the current collector
    /// newCollector - Factory to create a new collector object.
    let collectMerge newCollector merge source =
        Observable.Collect( source, Func<_> newCollector,Func<_,_,_> merge )


    /// Produces an enumerable sequence that returns elements collected/aggregated from the source sequence between consecutive iterations.
    /// merge - Merges a sequence element with the current collector
    /// getNewCollector - Factory to replace the current collector by a new collector
    /// getInitialCollector - Factory to create the initial collector object.
    let collectMergeInit getInitialCollector merge getNewCollector source =
        Observable.Collect( source           , Func<_> getInitialCollector  ,
                            Func<_,_,_> merge, Func<_,_> getNewCollector    )

    // #region CombineLatest Functions


    /// Merges the specified observable sequences into one observable sequence
    /// whenever either of the observable sequences produces an element.
    let combineLatest ( source1 : IObservable<'T1> ) ( source2 : IObservable<'T2> ) =
        Observable.CombineLatest(source1, source2, fun t1 t2 -> (t1, t2) )

    /// Merges the specified observable sequences into one observable sequence by
    /// emmiting a list with the latest source elements of whenever any of the
    /// observable sequences produces an element.
    let combineLatestSeq (source :seq<IObservable<'T>> ) : IObservable<IList<'T>> =
        Observable.CombineLatest( source )


    /// Merges the specified observable sequences into one observable sequence by  applying the map
    /// whenever any of the observable sequences produces an element.
    let combineLatestArray (source :IObservable<'T>[] )  =
        Observable.CombineLatest( source )


    /// Merges the specified observable sequences into one observable sequence by  applying the map
    /// whenever any of the observable sequences produces an element.
    let combineLatestSeqMap ( map : IList<'T>-> 'Result ) ( source :seq<IObservable<'T>> )  =
        Observable.CombineLatest( source, Func<IList<'T>,'Result> map )


    /// Concatenates the second observable sequence to the first observable sequence
    /// upn the successful termination of the first
    let concat (second: IObservable<'T>) (first: IObservable<'T>) =
        Observable.Concat(first, second)


    /// Concatenates all observable sequences within the sequence as long as
    /// the previous observable sequence terminated successfully
    let concatSeq (sources:seq<IObservable<'T>>) : IObservable<'T> =
        Observable.Concat(sources)


    /// Concatenates all of the specified  observable sequences as long as
    /// the previous observable sequence terminated successfully
    let concatArray (sources:IObservable<'T>[]) =
        Observable.Concat(sources)


    /// Concatenates all of the inner observable sequences as long as
    /// the previous observable sequence terminated successfully
    let concatInner (sources: IObservable<IObservable<'T>>) =
        Observable.Concat( sources )


    /// Concatenates all task results as long as
    /// the previous taskterminated successfully
    let concatTasks(sources: IObservable<Tasks.Task<'T>>) =
        Observable.Concat( sources )


    /// Connects the observable wrapper to its source. All subscribed
    /// observers will recieve values from the underlying observable
    /// sequence as long as the connection is established.
    /// ( publish an Observable to get a ConnectableObservable )
    let connect ( source:Subjects.IConnectableObservable<_> ) =
        source.Connect()


    /// Determines whether an observable sequence contains a specified
    /// element by using the default equality comparer.
    let contains value source =
        Observable.Contains( source, value )


    /// Determines whether an observable sequence contains a
    /// specified element by using a specified EqualityComparer
    let containsCompare comparer value source =
        Observable.Contains( source, value, comparer )


    /// Counts the elements
    let count source =
        Observable.Count(source)


    /// Returns an observable sequence containing an int that represents how many elements
    /// in the specified observable sequence satisfy a condition.
    let countSatisfy predicate source =
        Observable.Count( source, Func<_,_> predicate )


    /// Returns the elements of the specified sequence or the type parameter's default value
    /// in a singleton sequence if the sequence is empty.
    let defaultIfEmpty    ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.DefaultIfEmpty( source )


    /// Returns the elements of the specified sequence or the specified value in a singleton sequence if the sequence is empty.
    let defaultIfEmptyIs (defaultValue:'Source )( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.DefaultIfEmpty( source, defaultValue )


    /// Returns an observable sequence that invokes the specified factory function whenever a new observer subscribes.
    let defer ( observableFactory: unit -> IObservable<'Result> ): IObservable<'Result> =
        Observable.Defer(Func<IObservable<'Result>> observableFactory )


    /// Time shifts the observable sequence by the specified relative time duration.
    /// The relative time intervals between the values are preserved.
    let delay ( dueTime:TimeSpan ) ( source:IObservable<'Source> ): IObservable<'Source>=
        Observable.Delay(source, dueTime)

    /// Time shifts the observable sequence by the specified relative time duration,
    /// using the specified scheduler to run timers.
    /// The relative time intervals between the values are preserved.
    let delayOn (scheduler:IScheduler) (dueTime:TimeSpan) source =
        Observable.Delay(source, dueTime, scheduler)

    /// Time shifts the observable sequence to start propagating notifications at the specified absolute time.
    /// The relative time intervals between the values are preserved.
    let delayUntil ( source:IObservable<'Source> ) ( dueTime:DateTimeOffset ) : IObservable<'Source> =
        Observable.Delay(source, dueTime )

    /// Time shifts the observable sequence to start propagating notifications at the specified absolute time,
    /// using the specified scheduler to run timers.
    /// The relative time intervals between the values are preserved.
    let delayUntilOn (scheduler:IScheduler) (dueTime:DateTimeOffset) source =
        Observable.Delay(source, dueTime, scheduler)

    /// Time shifts the observable sequence based on a delay selector function for each element.
    let delayMap ( delayDurationSelector:'Source -> IObservable<'TDelay> )  ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.Delay( source, Func<'Source,IObservable<'TDelay>> delayDurationSelector)


    /// Time shifts the observable sequence based on a subscription delay and a delay selector function for each element.
    let delayMapFilter  ( delayDurationSelector         : 'Source -> IObservable<'TDelay>)
                        ( subscriptionDelay             : IObservable<'TDelay>)
                        ( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.Delay(source, subscriptionDelay, Func<'Source, IObservable<'TDelay>> delayDurationSelector)


    /// Time shifts the observable sequence by delaying the subscription with the specified relative time duration.
    let delaySubscription ( dueTime:TimeSpan) ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.DelaySubscription( source, dueTime )


    /// Time shifts the observable sequence by delaying the subscription with the specified relative time duration,
    /// using the specified scheduler to run timers.
    let delaySubscriptionOn (scheduler:IScheduler) (dueTime:TimeSpan) source =
        Observable.DelaySubscription(source, dueTime, scheduler)

    /// Time shifts the observable sequence by delaying the subscription to the specified absolute time.
    let delaySubscriptionUntil ( dueTime:DateTimeOffset) ( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.DelaySubscription( source, dueTime )


    /// Time shifts the observable sequence by delaying the subscription to the specified absolute time,
    /// using the specified scheduler to run timers.
    let delaySubscriptionUntilOn (scheduler:IScheduler) (dueTime:DateTimeOffset) source =
        Observable.DelaySubscription(source, dueTime, scheduler)


    /// Dematerializes the explicit notification values of an observable sequence as implicit notifications.
    let dematerialize source =
        Observable.Dematerialize(source)


    /// Returns an observable sequence that only contains distinct elements
    let distinct ( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.Distinct( source )


    /// Returns an observable sequence that contains only distinct elements according to the keySelector.
    let distinctKey ( keySelector:'Source -> 'Key )( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.Distinct( source, Func<'Source,'Key> keySelector)


    /// Returns an observable sequence that contains only distinct elements according to the comparer.
    let distinctCompare ( comparer:IEqualityComparer<'Source> )( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.Distinct( source, comparer )


    /// Returns an observable sequence that contains only distinct elements according to the keySelector and the comparer.
    let distinctKeyCompare ( keySelector:'Source -> 'Key )( comparer:IEqualityComparer<'Key>)( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.Distinct( source, Func<'Source,'Key> keySelector, comparer )


    /// Returns an observable sequence that only contains distinct contiguous elements
    let distinctUntilChanged ( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.DistinctUntilChanged(source)


    /// Returns an observable sequence that contains only distinct contiguous elements according to the keySelector.
    let distinctUntilChangedKey ( keySelector:'Source -> 'Key )( source:IObservable<'Source> )  : IObservable<'Source> =
        Observable.DistinctUntilChanged( source, Func<'Source,'Key> keySelector )


    /// Returns an observable sequence that contains only distinct contiguous elements according to the comparer.
    let distinctUntilChangedCompare ( comparer:IEqualityComparer<'Source> )( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.DistinctUntilChanged( source, comparer )


    /// Returns an observable sequence that contains only distinct contiguous elements according to the keySelector and the comparer.
    let distinctUntilChangedKeyCompare  ( keySelector:'Source -> 'Key )( comparer:IEqualityComparer<'Key> )( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.DistinctUntilChanged( source, Func<'Source,'Key> keySelector, comparer )


    /// Returns the element at a specified index in a sequence.
    let elementAt  ( index:int ) ( source:IObservable<'Source>): IObservable<'Source> =
        Observable.ElementAt( source, index )


    /// Returns the element at a specified index in a sequence or a default value if the index is out of range
    let elementAtOrDefault ( index:int )( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.ElementAt( source, index )

    /// Ignores all elements in an observable sequence leaving only the completed/error notifications
    let ignoreElements source =
        Observable.IgnoreElements (source)

    /// Returns an empty observable
    let empty<'T> = Observable.Empty<'T>()


    /// Returns an empty Observable sequence
    let emptyWitness<'T>(witness:'T) :IObservable<'T> =
        Observable.Empty( witness )


    /// Returns an empty sequence, using the specified scheduler to send out the single OnCompleted message.
    let emptyCompleted (scheduler:IScheduler) =
        Observable.Empty( scheduler )


    /// Returns an empty sequence, using the specified scheduler to send out the single OnCompleted message.
    let emptyCompletedWitness scheduler witness =
        Observable.Empty( scheduler, witness )


    /// Determines whether two sequences are equal by comparing the elements pairwise.
    let equals ( first:IObservable<'Source>  )( second:IObservable<'Source> ) : IObservable<bool> =
        Observable.SequenceEqual( first, second )


    /// Determines whether two sequences are equal by comparing the elements pairwise using a specified equality comparer.
    let equalsComparer ( comparer:IEqualityComparer<'Source>)  ( first:IObservable<'Source>  )( second:IObservable<'Source> ): IObservable<bool> =
        Observable.SequenceEqual( first, second, comparer )


    /// Determines whether an observable and enumerable sequence are equal by comparing the elements pairwise.
    let equalsSeq ( first:IObservable<'Source>  )( second:seq<'Source>) : IObservable<bool> =
        Observable.SequenceEqual( first, second )


    /// Determines whether an observable and enumerable sequence are equal by comparing the elements pairwise using a specified equality comparer.
    let equalsSeqComparer ( comparer:IEqualityComparer<'Source> ) ( first:IObservable<'Source>  )( second:seq<'Source> ) : IObservable<bool> =
        Observable.SequenceEqual( first, second, comparer )

    /// Immediately raises OnError on subscription
    [<Obsolete("Use Observable.throw instead")>]
    let error e = Observable.Throw e


    /// Determines whether an observable sequence contains a specified value
    /// which satisfies the given predicate
    let exists predicate source =
        Observable.Any(source, Func<_,_> predicate)


    /// Filters the observable elements of a sequence based on a predicate
    let filter  predicate (source: IObservable<'T>) =
        Observable.Where( source, Func<_,_> predicate )


    /// Filters the observable elements of a sequence based on a predicate by
    /// incorporating the element's index
    let filteri predicate (source: IObservable<'T>)  =
        Observable.Where( source, Func<_,_,_> (fun i x -> predicate x i) )


    /// Invokes a specified action after the source observable sequence
    /// terminates gracefully or exceptionally
    let finallyDo finallyAction source =
        Observable.Finally( source, Action finallyAction )


    /// Returns the first element of an observable sequence
    let first (source:IObservable<'T>)  =
        source.FirstAsync()


    /// Returns the first element of an observable sequence
    /// if it satisfies the predicate
    let firstIf predicate (source:IObservable<'T>) =
        source.FirstAsync( Func<_,_> predicate )


    /// Projects each element of an observable sequence to an observable sequence
    /// and merges the resulting observable sequences into one observable sequenc
    let flatmap map source =
        Observable.SelectMany(source, Func<'S,IObservable<'R>> map )


    /// Projects each element of an observable sequence to an observable sequence by incorporating the
    /// element's index and merges the resulting observable sequences into one observable sequence.
//    let flatmapi map source =
//        Observable.SelectMany(source,Func<'Source,int,seq<'Result>> map )
//

    /// Projects each element of the source observable sequence to the other observable sequence
    /// and merges the resulting observable sequences into one observable sequence.
    let flatmapOther  ( other:IObservable<'Other> ) ( source:IObservable<'Source> ): IObservable<'Other> =
        Observable.SelectMany( source, other )


    /// Projects each element of an observable sequence to an enumerable sequence and concatenates
    /// the resulting enumerable sequences into one observable sequence.
    let flatmapSeq map source =
        Observable.SelectMany(source, Func<'Source,seq<'Result>> map)


    /// Projects each element of an observable sequence to an enumerable sequence by incorporating the
    /// element's index and concatenates the resulting enumerable sequences into one observable sequence.
//    let flatmapSeqi map source =
//        Observable.SelectMany(source, Func<'Source,int,seq<'Result>> map)
//
//

    /// Projects each element of an observable sequence to a task and merges all of the task results into one observable sequence.
    let flatmapTask  ( map ) ( source:IObservable<'Source> ) : IObservable<'Result> =
        Observable.SelectMany( source, Func<'Source,Threading.Tasks.Task<'Result>> map )

    ///Turns an F# async workflow into an observable
    let ofAsync asyncOperation =
        Observable.FromAsync
            (fun (token : Threading.CancellationToken) -> Async.StartAsTask(asyncOperation, cancellationToken = token))

    ///Helper function for turning async workflows into observables
    let liftAsync asyncOperation =
        asyncOperation >> ofAsync

    /// Projects each element of an observable sequence to a async workflow and merges all of the async worksflow results into one observable sequence.
    let flatmapAsync asyncOperation (source : IObservable<'Source>) =
        source.SelectMany(fun item -> liftAsync asyncOperation item)


    /// Projects each element of an observable sequence to a task by incorporating the element's index
    /// and merges all of the task results into one observable sequence.
//    let flatmapTaski  ( map ) ( source:IObservable<'Source> ) : IObservable<'Result> =
//        Observable.SelectMany( source, Func<'Source,int,Threading.Tasks.Task<'Result>> map )


    /// Applies an accumulator function over an observable sequence, returning the
    /// result of the fold as a single element in the result sequence
    /// init is the initial accumulator value
    let fold accumulator init source =
        Observable.Aggregate(source, init, Func<_,_,_> accumulator)


    /// Applies an accumulator function over an observable sequence, returning the
    /// result of the fold as a single element in the result sequence
    /// init is the initial accumulator value, map is performed after the fold
    let foldMap accumulator init map source =
        Observable.Aggregate(source, init,Func<_,_,_> accumulator,Func<_,_>  map )


    /// Converts an Action-based .NET event to an observable sequence. Each event invocation is surfaced through an OnNext message in the resulting sequence.
    /// For conversion of events conforming to the standard .NET event pattern, use any of the FromEventPattern overloads instead.
    let fromEvent ( addHandler )( removeHandler ) : IObservable<unit> =
        Observable.FromEvent( Action<'Delegate> addHandler, Action<'Delegate> removeHandler )


    /// Converts an Action-based .NET event to an observable sequence. Each event invocation is surfaced through an OnNext message in the resulting sequence.
    /// For conversion of events conforming to the standard .NET event pattern, use any of the FromEventPattern overloads instead.
    let fromEventOn scheduler addHandler removeHandler =
        Observable.FromEvent(Action<_> addHandler, Action<_> removeHandler, scheduler)


    /// Converts an generic Action-based .NET event to an observable sequence. Each event invocation is surfaced through an OnNext message in the resulting sequence.
    /// For conversion of events conforming to the standard .NET event pattern, use any of the FromEventPattern overloads instead.
    let fromEventGeneric addHandler removeHandler : IObservable<'TEventArgs> =
        Observable.FromEvent(Action<'TEventArgs -> unit> addHandler, Action<'TEventArgs -> unit> removeHandler)


    /// Converts an generic Action-based .NET event to an observable sequence. Each event invocation is surfaced through an OnNext message in the resulting sequence.
    /// For conversion of events conforming to the standard .NET event pattern, use any of the FromEventPattern overloads instead.
    let fromEventGenericOn scheduler addHandler removeHandler =
        Observable.FromEvent(Action<#EventArgs -> unit> addHandler, Action<#EventArgs -> unit> removeHandler, scheduler)


    /// Converts a .NET event to an observable sequence, using a conversion function to obtain the event delegate.
    /// Each event invocation is surfaced through an OnNext message in the resulting sequence.
    /// For conversion of events conforming to the standard .NET event pattern, use any of the FromEventPattern functions instead.
    let fromEventConversion conversion addHandler removeHandler =
        Observable.FromEvent(
            conversion = Func<Action<#EventArgs>, unit> (fun action -> conversion (fun args -> action.Invoke(args))),
            addHandler = Action<_> addHandler,
            removeHandler = Action<_> removeHandler
        )



    /// Converts a .NET event to an observable sequence, using a conversion function to obtain the event delegate, using a specified scheduler to run timers.
    /// Each event invocation is surfaced through an OnNext message in the resulting sequence.
    /// For conversion of events conforming to the standard .NET event pattern, use any of the FromEventPattern functions instead.
    let fromEventConversionOn scheduler conversion addHandler removeHandler =
        Observable.FromEventPattern
            (Func<EventHandler<'TEventArgs>, 'TDelegate> conversion, Action<'TDelegate> addHandler, Action<'TDelegate> removeHandler, scheduler)


    /// Converts a .NET event to an observable sequence, using a supplied event delegate type.
    /// Each event invocation is surfaced through an OnNext message in the resulting sequence.
    let fromEventHandler addHandler removeHandler =
        Observable.FromEventPattern<#EventArgs> (
                 Action<EventHandler<_>> addHandler,
                 Action<EventHandler<_>> removeHandler)


    /// Converts a .NET event to an observable sequence, using a supplied event delegate type on a specified scheduler.
    /// Each event invocation is surfaced through an OnNext message in the resulting sequence.
    let fromEventHandlerOn (scheduler:IScheduler) addHandler removeHandler =
        Observable.FromEventPattern<#EventArgs> (
            Action<EventHandler<_>> addHandler,
            Action<EventHandler<_>> removeHandler,
            scheduler)


    /// Generates an observable from an IEvent<_> as an EventPattern.
    let fromEventPattern eventName (target:obj) =
        Observable.FromEventPattern( target, eventName )



    /// Generates an observable sequence by running a state-driven loop producing the sequence's elements.
    let generate initialState condition iterator resultMap =
        Observable.Generate(                            initialState,
                                Func<'State,bool>      condition   ,
                                Func<'State,'State>   iterator    ,
                                Func<'State,'Result>  resultMap   )

    /// Generates an observable sequence by running a state-driven loop producing the sequence's elements.
    let generateOn (scheduler:IScheduler) initialState condition iterator resultMap =
        Observable.Generate(
            initialState,
            Func<'State, bool> condition,
            Func<'State, 'State> iterator,
            Func<'State, 'TResult> resultMap,
            scheduler)


    /// Generates an observable sequence by running a state-driven and temporal loop producing the sequence's elements.
    let generateTimed( initialState:'State   )
                     ( condition             )
                     ( iterate               )
                     ( resultMap             )
                     ( timeSelector          ) : IObservable<'Result> =
        Observable.Generate(                                initialState   ,
                                Func<'State,bool>          condition       ,
                                Func<'State,'State>       iterate          ,
                                Func<'State,'Result>      resultMap        ,
                                Func<'State,DateTimeOffset>timeSelector    )


    /// Generates an observable sequence by running a state-driven and temporal loop producing the sequence's elements,
    /// using a specified scheduler to run timers and to send out observer messages.
    let generateTimedOn( scheduler             )
                       ( initialState:'State   )
                       ( condition             )
                       ( iterate               )
                       ( resultMap             )
                       ( timeSelector          ) : IObservable<'Result> =
        Observable.Generate(                                initialState   ,
                                Func<'State,bool>          condition       ,
                                Func<'State,'State>       iterate          ,
                                Func<'State,'Result>      resultMap        ,
                                Func<'State,DateTimeOffset>timeSelector    ,
                                scheduler)

    /// Generates an observable sequence by running a state-driven and temporal loop producing the sequence's elements.
    let generateTimeSpan ( initialState:'State )( condition )( iterate )( resultMap )( genTime ) : IObservable<'Result> =
        Observable.Generate( initialState, Func<_,_> condition, Func<_,_> iterate, Func<'State,'Result> resultMap, Func<'State,TimeSpan> genTime )


     /// Generates an observable sequence by running a state-driven and temporal loop producing the sequence's elements,
     /// using a specified scheduler to run timers and to send out observer messages.
    let generateTimeSpanOn scheduler ( initialState:'State )( condition )( iterate )( resultMap )( genTime ) : IObservable<'Result> =
        Observable.Generate( initialState, Func<_,_> condition, Func<_,_> iterate, Func<'State,'Result> resultMap, Func<'State,TimeSpan> genTime, scheduler )

    /// Returns an enumerator that enumerates all values of the observable sequence.
    let getEnumerator ( source ) : IEnumerator<_> =
        Observable.GetEnumerator( source )


    /// Groups the elements of an observable sequence according to a specified key selector function.
    let groupBy ( keySelector  )
                ( source      : IObservable<'Source>   ) :  IObservable<IGroupedObservable<'Key,'Source>> =
        Observable.GroupBy( source,Func<'Source,'Key>  keySelector )


    /// Groups the elements of an observable sequence with the specified initial
    /// capacity according to a specified key selector function.
//    let groupByCapacity ( keySelector )
//                        ( capacity:int )
//                        ( source:IObservable<'Source> ) : IObservable<IGroupedObservable<'Key,'Source>> =
//        Observable.GroupBy( source, Func<'Source,'Key> keySelector, capacity)


    /// Groups the elements of an observable sequence according to a specified key selector function and comparer.
    let groupByCompare ( keySelector  )
                        ( comparer    : IEqualityComparer<_> )
                        ( source      : IObservable<_>    ) : IObservable<IGroupedObservable<_,_>> =
        Observable.GroupBy( source, Func<_,_> keySelector, comparer )


    /// Groups the elements of an observable sequence and selects the resulting elements by using a specified function.
    let groupByElement  ( keySelector       )
                        ( elementSelector   )
                        ( source            ) : IObservable<IGroupedObservable<'Key,'Element>> =
        Observable.GroupBy( source, Func<'Source,'Key>  keySelector, Func<'Source,'Element> elementSelector )


    /// Groups the elements of an observable sequence with the specified initial capacity
    /// according to a specified key selector function and comparer.
//    let groupByCapacityCompare
//                ( keySelector                           )
//                ( capacity  : int                       )
//                ( comparer  : IEqualityComparer<'Key>   )
//                ( source    : IObservable<'Source>      )    : IObservable<IGroupedObservable<'Key,'Source>> =
//        Observable.GroupBy( source, Func<'Source,'Key> keySelector, capacity, comparer )
//

    /// Groups the elements of an observable sequence with the specified initial capacity
    /// and selects the resulting elements by using a specified function.
    let groupByCapacityElement
                ( keySelector           )
                ( capacity       : int  )
                ( elementSelector       )
                ( source         : IObservable<'Source> ): IObservable<IGroupedObservable<'Key,'Element>> =
        Observable.GroupBy( source, Func<'Source,'Key> keySelector, Func<'Source,'Element>  elementSelector )


    /// Groups the elements of an observable sequence according to a specified key selector function
    /// and comparer and selects the resulting elements by using a specified function.
    let groupByCompareElement
                ( keySelector      )
                ( comparer       : IEqualityComparer<'Key>       )
                ( elementSelector  )
                ( source         : IObservable<'Source>           ): IObservable<IGroupedObservable<'Key,'Element>> =
        Observable.GroupBy( source,Func<'Source,'Key>  keySelector, Func<'Source,'Element> elementSelector )



    /// Groups the elements of an observable sequence with the specified initial capacity according to a
    /// specified key selector function and comparer and selects the resulting elements by using a specified function.
//    let groupByCapacityCompareElement
//                ( keySelector      )
//                ( capacity        : int                      )
//                ( comparer        : IEqualityComparer<'Key> )
//                ( elementSelector  )
//                ( source          : IObservable<'Source>    ) : IObservable<IGroupedObservable<'Key,'Element>> =
//        Observable.GroupBy( source, Func<'Source,'Key>    keySelector, Func<'Source,'Element> elementSelector, capacity, comparer )
//

    ///  Groups the elements of an observable sequence according to a specified key selector function.
    ///  A duration selector function is used to control the lifetime of groups. When a group expires,
    ///  it receives an OnCompleted notification. When a new element with the same
    ///  key value as a reclaimed group occurs, the group will be reborn with a new lifetime request.
    let groupByUntil( keySelector )
                    ( durationSelector )
                    ( source:IObservable<'Source> ) : IObservable<IGroupedObservable<'Key,'Source>> =
        Observable.GroupByUntil( source, Func<'Source,'Key>keySelector,Func<IGroupedObservable<'Key,'Source>,IObservable<'TDuration>> durationSelector )


    /// Groups the elements of an observable sequence with the specified initial capacity according to a specified key selector function.
    /// A duration selector function is used to control the lifetime of groups. When a group
    /// expires, it receives an OnCompleted notification. When a new element with the same
    /// key value as a reclaimed group occurs, the group will be reborn with a new lifetime request.
//    let groupByCapacityUntil
//                    ( keySelector     )
//                    ( capacity        : int )
//                    ( durationSelector )
//                    ( source          : IObservable<'Source> ): IObservable<IGroupedObservable<'Key,'Source>> =
//        Observable.GroupByUntil( source, Func<'Source,'Key> keySelector,Func<IGroupedObservable<'Key,'Source>,IObservable<'TDuration>>  durationSelector, capacity)
//

    /// Groups the elements of an observable sequence according to a specified key selector function and comparer.
    /// A duration selector function is used to control the lifetime of groups. When a group expires,
    /// it receives an OnCompleted notification. When a new element with the same
    /// key value as a reclaimed group occurs, the group will be reborn with a new lifetime request.
    let groupByComparerUntil
                    ( keySelector)
                    ( comparer: IEqualityComparer<'Key> )
                    ( durationSelector )
                    ( source:IObservable<'Source> ) : IObservable<IGroupedObservable<'Key,'Source>> =
        Observable.GroupByUntil( source, Func<'Source,'Key>  keySelector, Func<IGroupedObservable<'Key,'Source>,IObservable<'TDuration>> durationSelector, comparer )


    /// Groups the elements of an observable sequence according to a specified key selector function
    /// and selects the resulting elements by using a specified function.
    /// A duration selector function is used to control the lifetime of groups. When a group expires,
    /// it receives an OnCompleted notification. When a new element with the same
    /// key value as a reclaimed group occurs, the group will be reborn with a new lifetime request.
    let groupByElementUntil
                    ( keySelector )
                    ( elementSelector )
                    ( durationSelector)
                    ( source:IObservable<'Source> ): IObservable<IGroupedObservable<'Key,'Element>> =
        Observable.GroupByUntil( source, Func<'Source,'Key> keySelector, Func<'Source,'Element>elementSelector, Func<IGroupedObservable<'Key,'Element>,IObservable<'TDuration>>  durationSelector )


    /// Groups the elements of an observable sequence with the specified initial capacity according to a specified key selector function and comparer.
    /// A duration selector function is used to control the lifetime of groups. When a group expires, it receives an OnCompleted notification. When a new element with the same
    /// key value as a reclaimed group occurs, the group will be reborn with a new lifetime request.
//    let groupByCapacityComparerUntil
//                        ( keySelector      )
//                        ( durationSelector )
//                        ( capacity : int   )
//                        ( comparer : IEqualityComparer<'Key> )
//                        ( source   : IObservable<'Source>    ) : IObservable<IGroupedObservable<'Key,'Source>> =
//        Observable.GroupByUntil(    source,
//                                    Func<'Source,'Key> keySelector,
//                                    Func<IGroupedObservable<'Key,'Source>,IObservable<'TDuration>> durationSelector,
//                                    capacity,
//                                    comparer                    )


    /// Groups the elements of an observable sequence with the specified initial capacity according to a specified key
    /// selector function and selects the resulting elements by using a specified function.
    /// A duration selector function is used to control the lifetime of groups. When a group
    /// expires, it receives an OnCompleted notification. When a new element with the same
    /// key value as a reclaimed group occurs, the group will be reborn with a new lifetime request.
//    let groupByCapacityElementUntil
//                        ( keySelector      )
//                        ( capacity        : int )
//                        ( elementSelector   )
//                        ( durationSelector )
//                        ( source          : IObservable<'Source> ) : IObservable<IGroupedObservable<'Key,'Element>> =
//        Observable.GroupByUntil( source, Func<'Source,'Key>keySelector, Func<'Source,'Element>elementSelector, Func<IGroupedObservable<'Key,'Element>,IObservable<'TDuration>> durationSelector, capacity )


    /// Groups the elements of an observable sequence according to a specified key selector function and
    /// comparer and selects the resulting elements by using a specified function.
    /// A duration selector function is used to control the lifetime of groups. When a group expires,
    /// it receives an OnCompleted notification. When a new element with the same
    /// key value as a reclaimed group occurs, the group will be reborn with a new lifetime request.
    let groupByComparerElementUntil
                    ( keySelector )
                    ( comparer:Collections.Generic.IEqualityComparer<'Key>)
                    ( elementSelector )
                    ( durationSelector )
                    ( source:IObservable<'Source> ) : IObservable<IGroupedObservable<'Key,'Element>> =
        Observable.GroupByUntil( source, Func<'Source,'Key> keySelector, Func<'Source,'Element>elementSelector, Func<IGroupedObservable<'Key,'Element>,IObservable<'TDuration>>durationSelector, comparer )


    /// Groups the elements of an observable sequence with the specified initial capacity according to a specified
    /// key selector function and comparer and selects the resulting elements by using a specified function.
    /// A duration selector function is used to control the lifetime of groups. When a group expires, it receives
    /// an OnCompleted notification. When a new element with the same
    /// key value as a reclaimed group occurs, the group will be reborn with a new lifetime request.
//    let groupByCapacityComparerElementUntil
//                    ( keySelector )
//                    ( capacity:int )
//                    ( comparer:IEqualityComparer<'Key>)
//                    ( elementSelector )
//                    ( durationSelector )
//                    ( source:IObservable<'Source> ) : IObservable<IGroupedObservable<'Key,'Element>> =
//        Observable.GroupByUntil( source, Func<'Source,'Key>keySelector, Func<'Source,'Element>elementSelector, Func<IGroupedObservable<'Key,'Element>,IObservable<'TDuration>>durationSelector, capacity, comparer )


    /// Correlates the elements of two sequences based on overlapping
    /// durations and groups the results
    let groupJoin   ( left        : IObservable<'Left>       )
                    ( right       : IObservable<'Right>      )
                    ( leftselect  : 'Left -> IObservable<'a> )
                    ( rightselect : 'Right-> IObservable<'b> )
                    ( resultselect: 'Left -> IObservable<'Right>->'Result )  =
        Observable.GroupJoin(   left, right,
                                Func<'Left , IObservable<'a>>            leftselect  ,
                                Func<'Right, IObservable<'b>>            rightselect ,
                                Func<'Left , IObservable<'Right>,'Result>resultselect)


    /// Creates an observable that calls the specified function (each time)
    /// after an observer is attached to the observable. This is useful to
    /// make sure that events triggered by the function are handled.
    let guard f (source:IObservable<'Args>) =
        Observable.Create (fun observer ->
            let disposable = source.Subscribe observer in f ()
            disposable
        )


    /// Takes the first element of the observable sequence
    let head obs = Observable.FirstAsync(obs)

    /// Returns an observable sequence that produces a value after each period
    let interval period =
        Observable.Interval( period )

    /// Returns an observable sequence that produces a value on the specified scheduler after each period
    let intervalOn (scheduler : IScheduler) period =
        Observable.Interval( period, scheduler )

    /// IsEmpty returns an Observable that emits true if and only if the
    /// source Observable completes without emitting any items.
    let isEmpty source =
        Observable.IsEmpty source


    /// Invokes an action for each element in the observable sequence, and propagates all observer
    /// messages through the result sequence. This method can be used for debugging, logging, etc. of query
    /// behavior by intercepting the message stream to run arbitrary actions for messages on the pipeline.
    let iter ( onNext ) ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.Do( source, Action<'Source> onNext )


    /// Invokes an action for each element in the observable sequence and invokes an action
    /// upon graceful termination of the observable sequence. This method can be used for debugging,
    ///  logging, etc. of query behavior by intercepting the message stream to run arbitrary
    /// actions for messages on the pipeline.
    let iterEnd ( onNext )( onCompleted ) ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.Do( source, Action<'Source> onNext, Action onCompleted )


    /// Invokes an action for each element in the observable sequence and invokes an action upon
    /// exceptional termination of the observable sequence. This method can be used for debugging,
    /// logging, etc. of query behavior by intercepting the message stream to run arbitrary
    /// actions for messages on the pipeline.
    let iterError ( onNext)( onError ) ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.Do( source, Action<'Source> onNext, Action<exn> onError )


    /// Invokes an action for each element in the observable sequence and invokes an action
    /// upon graceful or exceptional termination of the observable sequence.
    /// This method can be used for debugging, logging, etc. of query behavior by intercepting
    /// the message stream to run arbitrary actions for messages on the pipeline.
    let iterErrorEnd ( onNext )( onError ) ( onCompleted ) ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.Do( source, Action<'Source> onNext, Action<exn> onError, Action onCompleted )


    /// Invokes the observer's methods for each message in the source sequence.
    /// This method can be used for debugging, logging, etc. of query behavior by intercepting
    /// the message stream to run arbitrary actions for messages on the pipeline.
    let iterObserver ( observer:IObserver<'Source> ) ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.Do( source,observer )


    /// Correlates the elements of two sequences based on overlapping durations.
    let join right f left =
        Observable.Join( left, right, Func<_, _> Observable.Return, Func<_, _> Observable.Return, Func<_, _, _> f )


    /// Correlates the elements of two sequences based on overlapping durations.
    let joinMap right fLeft fRight fResult left =
        Observable.Join( left, right, Func<_, _> fLeft, Func<_, _> fRight, Func<_, _, _> fResult )


    /// Joins together the results from several patterns
    let joinWhen (plans:seq<Joins.Plan<'T>>): IObservable<'T> =
        Observable.When( plans )


    /// Returns the last element of an observable sequence.
    let last ( source:IObservable<'Source>) : IObservable<'Source> =
        Observable.LastAsync( source )


    /// Returns the last element of an observable sequence that satisfies the condition in the predicate
    let lastIf  ( predicate ) ( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.LastAsync( source, Func<'Source,bool> predicate )


    /// Returns an enumerable sequence whose enumeration returns the latest observed element in the source observable sequence.
    /// Enumerators on the resulting sequence will never produce the same element repeatedly,
    /// and will block until the next element becomes available.
    let latest source =
        Observable.Latest( source )


    /// Returns an observable sequence containing a int64 that represents
    /// the total number of elements in an observable sequence
    let longCount source =
        Observable.LongCount(source)


    /// Returns an observable sequence containing an int that represents how many elements
    /// in the specified observable sequence satisfy a condition.
    let longCountSatisfy predicate source =
        Observable.LongCount(source, Func<_,_> predicate)


    /// Maps the given observable with the given function
    let map f source = Observable.Select(source, Func<_,_>(f))


    /// Maps the given observable with the given function and the
    /// index of the element
    let mapi (f:int -> 'Source -> 'Result) (source:IObservable<'Source>) =
        Observable.Select (source, Func<_,_,_> (fun i x -> f x i))


    /// Maps two observables to the specified function.
    let map2 f a b = apply (apply f a) b


    /// Combines 'map' and 'fold'. Builds an observable whose emits are the result of applying the given function to each of the emits of the source observable.
    /// The function is also used to accumulate a final value.
    let mapFold (f : 'TState -> 'T -> 'TResult * 'TState) (init : 'TState) source =
        Observable.Aggregate(source, ([], init),
            Func<_, _, _> (fun (ys, state) x ->
                let y, state = f state x
                y :: ys, state))


    /// Maps every emission to a constant value.
    let mapTo x (source : IObservable<'T>) =
        Observable.Select (source, Func<_, _> (fun _ -> x))


    /// Maps every emission to a constant lazy value.
    let mapToLazy (xLazy : Lazy<'T>) (source : IObservable<'T>) =
        Observable.Select (source, Func<_, _> (fun _ -> xLazy.Force ()))


    /// Materializes the implicit notifications of an observable sequence as
    /// explicit notification values
    let materialize source =
        Observable.Materialize( source )





    /// Merges the two observables
    let merge (second: IObservable<'T>) (first: IObservable<'T>) = Observable.Merge(first, second)


    /// Merges the two observables, using a specified scheduler for enumeration of and subscription to the sources.
    let mergeOn (scheduler:IScheduler) (second:IObservable<'T>) (first:IObservable<'T>) =
        Observable.Merge(first, second, scheduler)


    /// Merges all the observable sequences into a single observable sequence.
    let mergeArray (sources:IObservable<'T>[]) =
        Observable.Merge(sources)


    /// Merges all the observable sequences into a single observable sequence,
    /// using a specified scheduler for enumeration of and subscription to the sources.
    let mergeArrayOn (scheduler:IScheduler) (sources:IObservable<'T>[]) =
        Observable.Merge(scheduler, sources)


    /// Merges elements from all inner observable sequences
    /// into a single  observable sequence.
    let mergeInner (sources:IObservable<IObservable<'T>>) =
        Observable.Merge(sources)


    /// Merges elements from all inner observable sequences
    /// into a single  observable sequence limiting the number of concurrent
    /// subscriptions to inner sequences
    let mergeInnerMax (maxConcurrent:int) (sources:IObservable<IObservable<'T>>) =
        Observable.Merge(sources, maxConcurrent)


    /// Merges an enumerable sequence of observable sequences into a single observable sequence.
    let mergeSeq (sources:seq<IObservable<'T>>) =
        Observable.Merge(sources)


    /// Merges an enumerable sequence of observable sequences into a single observable sequence,
    /// using a specified scheduler for enumeration of and subscription to the sources.
    let mergeSeqOn (scheduler:IScheduler) (sources:seq<IObservable<'T>>) =
        Observable.Merge(sources, scheduler)


    /// Merges an enumerable sequence of observable sequences into an observable sequence,
    ///  limiting the number of concurrent subscriptions to inner sequences.
    let mergeSeqMax (maxConcurrent:int)(sources:seq<IObservable<'T>>) =
        Observable.Merge(sources, maxConcurrent)


    /// Merges an enumerable sequence of observable sequences into an observable sequence,
    ///  limiting the number of concurrent subscriptions to inner sequences,
    /// using a specified scheduler for enumeration of and subscription to the sources.
    let mergeSeqMaxOn (scheduler:IScheduler) (maxConcurrent:int) (sources:seq<IObservable<'T>>) =
        Observable.Merge(sources, maxConcurrent, scheduler)


    /// Merge results from all source tasks into a single observable sequence
    let mergeTasks (sources:IObservable<Tasks.Task<'T>>) =
        Observable.Merge(sources)


    /// Returns the maximum element in an observable sequence.
    let maxOf (source:IObservable<'T>) =
        Observable.Max( source )


    /// Returns an enumerable sequence whose sequence whose enumeration returns the
    /// most recently observed element in the source observable sequence, using
    /// the specified
    let mostRecent initialVal source =
        Observable.MostRecent( source, initialVal )


    /// Multicasts the source sequence notifications through the specified subject to
    /// the resulting connectable observable. Upon connection of the connectable
    /// observable, the subject is subscribed to the source exactly one, and messages
    /// are forwarded to the observers registered with the connectable observable.
    /// For specializations with fixed subject types, see Publish, PublishLast, and Replay.
    let multicast subject source =
        Observable.Multicast(source, subject)


    /// Multicasts the source sequence notifications through an instantiated subject into
    /// all uses of the sequence within a selector function. Each subscription to the
    /// resulting sequence causes a separate multicast invocation, exposing the sequence
    /// resulting from the selector function's invocation. For specializations with fixed
    /// subject types, see Publish, PublishLast, and Replay.
    let multicastMap subjectSelector selector source  =
        Observable.Multicast(source, Func<_> subjectSelector, Func<_,_> selector)


    /// Returns a non-terminating observable sequence, which can
    /// be used to denote an infinite duration (e.g. when using reactive joins).
    let infinite() =
        Observable.Never()


    /// Returns a non-terminating observable sequence, which can be
    /// used to denote an infinite duration (e.g. when using reactive joins).
    let neverWitness( witness ) =
        Observable.Never( witness )


    /// Returns an observable sequence whose enumeration blocks until the next
    /// element in the source observable sequence becomes available.
    /// Enumerators  on the resulting sequence will block until the next
    /// element becomes available.
    let next source =
        Observable.Next( source )


    /// Returns the sequence as an observable
    let ofSeq<'Item>(source:'Item seq) : IObservable<'Item> =
        Observable.ToObservable source


    /// Returns the sequence as an observable, using the specified scheduler to run the enumeration loop
    let ofSeqOn<'Item>(scheduler:Concurrency.IScheduler) (items:'Item seq) : IObservable<'Item> =
        Observable.ToObservable (items, scheduler)


    /// Wraps the source sequence in order to run its observer callbacks on the specified scheduler.
    let observeOn (scheduler:Concurrency.IScheduler) source =
        Observable.ObserveOn( source, scheduler )


    /// Wraps the source sequence in order to run its observer callbacks
    /// on the specified synchronization context.
    let observeOnContext (context:SynchronizationContext) source =
        Observable.ObserveOn( source, context )


    /// Filters the elements of an observable sequence based on the specified type
    let ofType source =
        Observable.OfType( source )


    /// Concatenates the second observable sequence to the first observable sequence
    /// upon successful or exceptional termination of the first.
    let onErrorConcat ( second:IObservable<'Source> ) ( first:IObservable<'Source> ) : IObservable<'Source> =
        Observable.OnErrorResumeNext( first, second )


    /// Concatenates all of the specified observable sequences, even if the previous observable sequence terminated exceptionally.
    let onErrorConcatArray ( sources:IObservable<'Source> [] ) : IObservable<'Source> =
        Observable.OnErrorResumeNext( sources )


    /// Concatenates all observable sequences in the given enumerable sequence, even if the
    /// previous observable sequence terminated exceptionally.
    let onErrorConcatSeq ( sources:seq<IObservable<'Source>> ) : IObservable<'Source> =
        Observable.OnErrorResumeNext( sources )


    /// Iterates through the observable and performs the given side-effect
    let perform f source =
        let inner x = f x
        Observable.Do(source, inner)

    /// Logs the incoming emits with a given prefix to a specified target.
    let logTo prefix f source =
        let onNext = Action<_> (fun x -> f (sprintf "%s - OnNext(%A)" prefix x))
        let onError = Action<exn> (fun ex ->
            f (sprintf "%s - OnError:" prefix)
            f (sprintf "\t %A" ex))
        let onCompleted = Action (fun () -> f (sprintf "%s - OnCompleted()" prefix))
        Observable.Do(source, onNext, onError, onCompleted)

    /// Logs the incoming emits with a given prefix to the console.
    let log prefix source =
        logTo prefix Console.WriteLine source


    /// Invokes the finally action after source observable sequence terminates normally or by an exception.
    let performFinally f source = Observable.Finally(source, Action f)


    /// Returns a connectable observable sequence (IConnectableObsevable) that shares
    /// a single subscription to the underlying sequence. This operator is a
    /// specialization of Multicast using a regular Subject
    let publish source =
        Observable.Publish( source )


    /// Returns a connectable observable sequence (IConnectableObsevable) that shares
    /// a single subscription to the underlying sequence and starts with the value
    /// initial. This operator is a specialization of Multicast using a regular Subject
    let publishInitial (initial:'Source) (source:IObservable<'Source>) =
        Observable.Publish( source, initial )


    /// Returns an observable sequence that is the result of invoking
    /// the selector on a connectable observable sequence that shares a
    /// a single subscription to the underlying sequence. This operator is a
    /// specialization of Multicast using a regular Subject
    let publishMap ( map:IObservable<'Source> -> IObservable<'Result> )
                   ( source  :IObservable<'Source>            ) =
        Observable.Publish( source, Func<IObservable<'Source>,IObservable<'Result>> map )


    /// Returns an observable sequence that is the result of
    /// the map on a connectable observable sequence that shares a
    /// a single subscription to the underlying sequence. This operator is a
    /// specialization of Multicast using a regular Subject
    let publishInitialMap  ( initial : 'Source  )
                           ( map: IObservable<'Source> -> IObservable<'Result> )
                           ( source  : IObservable<'Source> ) =
        Observable.Publish( source, Func<IObservable<'Source>,IObservable<'Result>> map, initial )


    /// Returns an observable sequence that is the result of invoking
    /// the selector on a connectable observable sequence containing
    /// only the last notification This operator is a
    /// specialization of Multicast using a regular Subject
    let publishLast source =
        Observable.PublishLast( source )


    /// Returns an observable sequence that is the result of invoking
    /// the selector on a connectable observable sequence that shares a
    /// a single subscription to the underlying sequence. This operator is a
    /// specialization of Multicast using a regular Subject
    let publishLastMap ( map: IObservable<'Source> -> IObservable<'Result> ) source  =
        Observable.PublishLast( source , Func<IObservable<'Source>,IObservable<'Result>> map )


    /// Creates a range as an observable
    let range start count = Observable.Range(start, count)


    /// Creates a range as an observable, using the specified scheduler to send out observer messages.
    let rangeOn scheduler start count = Observable.Range(start, count, scheduler)


    /// Reduces the observable
    let reduce f source = Observable.Aggregate(source, Func<_,_,_> f)


    /// Returns an observable that remains connected to the source as long
    /// as there is at least one subscription to the observable sequence
    /// ( publish an Observable to get a ConnectableObservable )
    let refCount ( source )=
        Observable.RefCount ( source )


    /// Repeats the observable sequence indefinitely.
    let repeat<'Source> ( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.Repeat<'Source>( source )


    /// Repeats the observable sequence a specified number of times.
    let repeatCount<'Result> ( repeatCount:int ) ( value:'Result ) : IObservable<'Result> =
        Observable.Repeat( value, repeatCount )


    /// Generates an observable sequence that repeats the given element infinitely.
    let repeatValue ( value:'Result ) : IObservable<'Result> =
        Observable.Repeat<'Result>( value )


    /// Repeats the given observable sequence as long as the specified condition holds, where the
    /// condition is evaluated after each repeated source is completed.
    let repeatWhile ( condition)( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.DoWhile( source, Func<bool> condition)


    /// Returns a connectable observable sequence that shares a single subscription to the
    /// underlying sequence replaying all notifications.
    let replay ( source:IObservable<'Source>) : Subjects.IConnectableObservable<'Source> =
        Observable.Replay( source )

    /// Returns a connectable observable sequence that shares a single subscription to the
    /// underlying sequence replaying all notifications.
    let replayOn ( sch:IScheduler ) source =
        Observable.Replay( source, sch )

    /// Returns a connectable observable sequence that shares a single subscription to the underlying sequence
    /// replaying notifications subject to a maximum element count for the replay buffer.
    let replayBuffer ( bufferSize:int )( source:IObservable<'Source>)  : Subjects.IConnectableObservable<'Source> =
            Observable.Replay( source, bufferSize )

    /// Returns a connectable observable sequence that shares a single subscription to the underlying sequence
    /// replaying notifications subject to a maximum element count for the replay buffer and using the specified
    /// scheduler to do the buffering on.
    let replayBufferOn ( sch:IScheduler )( bufferSize:int )( source:IObservable<'Source>)  : Subjects.IConnectableObservable<'Source> =
            Observable.Replay( source, bufferSize, sch )

    /// Returns an observable sequence that is the result of invoking the selector on a connectable observable
    /// sequence that shares a single subscription to the underlying sequence replaying all notifications.
    let replayMap ( map )( source:IObservable<'Source>)  : IObservable<'Result> =
            Observable.Replay( source, Func<IObservable<'Source>,IObservable<'Result>> map )


    /// Returns a connectable observable sequence that shares a single subscription to the underlying sequence
    /// replaying notifications subject to a maximum time length for the replay buffer.
    let replayWindow  ( window:TimeSpan ) ( source:IObservable<'Source>): Subjects.IConnectableObservable<'Source> =
            Observable.Replay( source, window )

    /// Returns a connectable observable sequence that shares a single subscription to the underlying sequence
    /// replaying notifications subject to a maximum time length for the replay buffer.
    let replayWindowOn  (scheduler:Concurrency.IScheduler) ( window:TimeSpan ) ( source:IObservable<'Source>): Subjects.IConnectableObservable<'Source> =
            Observable.Replay( source, window, scheduler )

    /// Returns a connectable observable sequence that shares a single subscription to the underlying sequence
    //  replaying notifications subject to a maximum time length and element count for the replay buffer.
    let replayBufferWindow  ( bufferSize:int )( window:TimeSpan )( source:IObservable<'Source>) : Subjects.IConnectableObservable<'Source> =
            Observable.Replay( source, bufferSize, window )

    /// Returns a connectable observable sequence that shares a single subscription to the underlying sequence
    //  replaying notifications subject to a maximum time length and element count for the replay buffer.
    let replayBufferWindowOn (scheduler:Concurrency.IScheduler) ( bufferSize:int )( window:TimeSpan )( source:IObservable<'Source>) : Subjects.IConnectableObservable<'Source> =
            Observable.Replay( source, bufferSize, window, scheduler )

    /// Returns an observable sequence that is the result of apply a map to a connectable observable sequence that
    /// shares a single subscription to the underlying sequence replaying notifications subject to
    /// a maximum element count for the replay buffer.
    let replayMapBuffer ( map ) ( bufferSize:int )( source:IObservable<'Source>) : IObservable<'Result> =
            Observable.Replay( source, Func<IObservable<'Source>,IObservable<'Result>>map, bufferSize )

    /// Returns an observable sequence that is the result of apply a map to a connectable observable sequence that
    /// shares a single subscription to the underlying sequence replaying notifications subject to
    /// a maximum time length.
    let replayMapWindow  ( map)( window:TimeSpan )( source:IObservable<'Source>) : IObservable<'Result> =
            Observable.Replay( source,Func<IObservable<'Source>,IObservable<'Result>>  map, window )

    /// Returns an observable sequence that is the result of apply a map to a connectable observable sequence that
    /// shares a single subscription to the underlying sequence replaying notifications subject to
    /// a maximum time length.
    let replayMapWindowOn (scheduler:Concurrency.IScheduler) ( map)( window:TimeSpan )( source:IObservable<'Source>) : IObservable<'Result> =
            Observable.Replay( source,Func<IObservable<'Source>,IObservable<'Result>>  map, window, scheduler )

    /// Returns an observable sequence that is the result of apply a map to a connectable observable sequence that
    /// shares a single subscription to the underlying sequence replaying notifications subject to
    /// a maximum time length and element count for the replay buffer.
    let replayMapBufferWindow  ( map )( bufferSize:int ) ( window:TimeSpan ) ( source:IObservable<'Source>): IObservable<'Result> =
        Observable.Replay( source, Func<IObservable<'Source>, IObservable<'Result>> map, bufferSize, window )

    /// Returns an observable sequence that is the result of apply a map to a connectable observable sequence that
    /// shares a single subscription to the underlying sequence replaying notifications subject to
    /// a maximum time length and element count for the replay buffer.
    let replayMapBufferWindowOn (scheduler:Concurrency.IScheduler) ( map )( bufferSize:int ) ( window:TimeSpan ) ( source:IObservable<'Source>): IObservable<'Result> =
        Observable.Replay( source, Func<IObservable<'Source>, IObservable<'Result>> map, bufferSize, window, scheduler )

    /// Repeats the source observable sequence until it successfully terminates.
    let retry ( source:IObservable<'Source>) : IObservable<'Source> =
        Observable.Retry( source )


    /// Repeats the source observable sequence the specified number of times or until it successfully terminates.
    let retryCount (count:int) ( source:IObservable<'Source>) : IObservable<'Source> =
        Observable.Retry( source, count )

    /// Returns an observable which emits a single value
    let result x : IObservable<_> = Observable.Return x

    /// Samples the observable at the given interval
    let sample (interval: TimeSpan) source =
        Observable.Sample(source, interval)


    /// Samples the observable sequence at each interval, using the specified scheduler to run sampling timers.
    /// Upon each sampling tick, the latest element (if any) in the source sequence during the
    /// last sampling interval is sent to the resulting sequence.
    let sampleOn scheduler interval source =
        Observable.Sample(source, interval, scheduler)


    /// Samples the source observable sequence using a samper observable sequence producing sampling ticks.
    /// Upon each sampling tick, the latest element (if any) in the source sequence during the
    /// last sampling interval is sent to the resulting sequence.
    let sampleWith   (sampler:IObservable<'Sample>) (source:IObservable<'Source>) : IObservable<'Source> =
        Observable.Sample( source, sampler )


    /// Applies an accumulator function over an observable sequence and returns each intermediate result.
    let scan (accumulator:'a->'a->'a)  source =
        Observable.Scan(source, Func<'a,'a,'a> accumulator  )


    /// Applies an accumulator function over an observable sequence and returns each intermediate result.
    /// The specified init value is used as the initial accumulator value.
    let scanInit (init:'TAccumulate) (accumulator) (source:IObservable<'Source>) : IObservable<'TAccumulate> =
        Observable.Scan( source, init, Func<'TAccumulate,'Source,'TAccumulate> accumulator )


    /// If the condition evaluates true, select the "thenSource" sequence. Otherwise, return an empty sequence.
    let selectIf condition thenSource =
        Observable.If( Func<bool> condition, thenSource )


    /// If the condition evaluates true, select the "thenSource" sequence.
    /// Otherwise, return an empty sequence generated on the specified scheduler.
    let selectIfOn (scheduler:IScheduler) condition thenSource =
        Observable.If( Func<bool> condition, thenSource, scheduler)


    /// If the condition evaluates true, select the "thenSource" sequence. Otherwise, select the else source
    let selectIfElse condition ( elseSource : IObservable<'Result>)
                               ( thenSource : IObservable<'Result>) =
        Observable.If( Func<bool> condition, thenSource, elseSource )


    ///  Returns an observable sequence that contains a single element.
    let single ( value:'Result) : IObservable<'Result> =
       Observable.Return(value)

    ///  Returns an observable sequence that contains a single element,
    /// using a specified scheduler to send out observer messages.
    let singleOn scheduler value =
        Observable.Return(value, scheduler)

    /// Bypasses a specified number of elements in an observable sequence and then returns the remaining elements.
    let skip (count:int) (source:IObservable<'Source>)  : IObservable<'Source> =
        Observable.Skip(source , count)


    /// Skips elements for the specified duration from the start of the observable source sequence.
    let skipSpan  (duration:TimeSpan ) (source:IObservable<'Source> ): IObservable<'Source> =
        Observable.Skip(source, duration)


    /// Skips elements for the specified duration from the start of the observable source sequence,
    /// using a specified scheduler to run timers.
    let skipSpanOn (scheduler:IScheduler) duration source =
        Observable.Skip(source, duration, scheduler)


    /// Bypasses a specified number of elements at the end of an observable sequence.
    let skipLast  (count:int ) ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.SkipLast (source, count )


    /// Skips elements for the specified duration from the end of the observable source sequence.
    let skipLastSpan (duration:TimeSpan ) ( source:IObservable<'Source>) : IObservable<'Source> =
        Observable.SkipLast ( source, duration)


    /// Skips elements for the specified duration from the end of the observable source sequence,
    /// using the specified scheduler to run timers.
    let skipLastSpanOn (scheduler:IScheduler) duration source =
        Observable.SkipLast(source, duration, scheduler)


    /// Skips elements from the observable source sequence until the specified start time.
    let skipUntil (startTime:DateTimeOffset ) ( source:IObservable<'Source> )  : IObservable<'Source> =
        Observable.SkipUntil(source, startTime )


    /// Skips elements from the observable source sequence until the specified start time,
    /// using the specified scheduler to run timers.
    let skipUntilOn (scheduler:IScheduler) startTime source =
        Observable.SkipUntil(source, startTime, scheduler)


    /// Returns the elements from the source observable sequence only after the other observable sequence produces an element.
    let skipUntilOther ( other:IObservable<'Other> )  ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.SkipUntil(source, other )



    /// Bypasses elements in an observable sequence as long as a specified condition is true and then returns the remaining elements.
    let skipWhile ( predicate:'Source -> bool ) ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.SkipWhile ( source, Func<'Source,bool> predicate )


    /// Bypasses elements in an observable sequence as long as a specified condition is true and then returns the remaining elements.
    /// The element's index is used in the logic of the predicate functio
    let skipWhilei ( predicate:'Source -> int -> bool)( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.SkipWhile ( source, Func<'Source,int,bool> predicate)


    /// Prepends a sequence of values to an observable sequence.
    let startWith  (values: #seq<'T>)  (source: IObservable<'T>) : IObservable<'T> =
        // TODO: re-evaluate wrapping the overload that takes a params array when params are supported by F#.
        Observable.StartWith( source, values )


    /// Prepends a sequence of values to an observable sequence.
    let startWithOn (scheduler:IScheduler) (values:#seq<'T>) source =
        Observable.StartWith(source, scheduler, values)


    /// Subscribes to the Observable with a next fuction.
    let subscribe(onNext: 'T -> unit) (observable: IObservable<'T>) =
          observable.Subscribe(Action<_> onNext)


    /// Subscribes to the Observable with a next and an error-function.
    let subscribeWithError  ( onNext     : 'T   -> unit     )
                            ( onError    : exn  -> unit     )
                            ( observable : IObservable<'T>  ) =
        observable.Subscribe( Action<_> onNext, Action<exn> onError )


    /// Subscribes to the Observable with a next and a completion callback.
    let subscribeWithCompletion (onNext: 'T -> unit) (onCompleted: unit -> unit) (observable: IObservable<'T>) =
            observable.Subscribe(Action<_> onNext, Action onCompleted)


    /// Subscribes to the observable with all three callbacks
    let subscribeWithCallbacks onNext onError onCompleted (observable: IObservable<'T>) =
        observable.Subscribe(Observer.Create(Action<_> onNext, Action<_> onError, Action onCompleted))


    /// Subscribes to the observable with the given observer
    let subscribeObserver observer (observable: IObservable<'T>) =
        observable.Subscribe observer


    /// Wraps the source sequence in order to run its subscription and unsubscription logic
    /// on the specified scheduler. This operation is not commonly used;  This only performs
    /// the side-effects of subscription and unsubscription on the specified scheduler.
    ///  In order to invoke observer callbacks on a scheduler, use 'observeOn'
    let subscribeOn (scheduler:Reactive.Concurrency.IScheduler) (source:IObservable<'Source>) : IObservable<'Source> =
        Observable.SubscribeOn( source, scheduler )

    /// Wraps the source sequence in order to run its subscription and unsubscription logic
    /// on the specified SynchronizationContext. This operation is not commonly used;  This only performs
    /// the side-effects of subscription and unsubscription on the specified scheduler.
    ///  In order to invoke observer callbacks on a scheduler, use 'observeOn'
    let subscribeOnContext (context:Threading.SynchronizationContext) (source:IObservable<'Source>) : IObservable<'Source> =
        Observable.SubscribeOn( source, context )


    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// Subscribe function to the observer's 'OnError channel. This function is typically used to write query operators.
    let subscribeSafe onNext (source : IObservable<_>) =
        source.SubscribeSafe (Observer.Create (Action<_> onNext, Action<_> ignore, Action ignore))


    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// Subscribe function to the observer's 'OnError channel. This function is typically used to write query operators.
    let subscribeSafeWithError onNext onError (source : IObservable<_>) =
        source.SubscribeSafe (Observer.Create (Action<_> onNext, Action<_> onError, Action ignore))


    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// Subscribe function to the observer's 'OnError channel. This function is typically used to write query operators.
    let subscribeSafeWithCompletion onNext onCompleted (source : IObservable<_>) =
        source.SubscribeSafe (Observer.Create (Action<_> onNext, Action<_> ignore, Action ignore))


    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// Subscribe function to the observer's 'OnError channel. This function is typically used to write query operators.
    let subscribeSafeObserver observer (source : IObservable<_>) =
        source.SubscribeSafe observer


    /// Subscribes to the specified source, re-routing synchronous exceptions during invocation of the
    /// Subscribe function to the observer's 'OnError channel. This function is typically used to write query operators.
    let subscribeSafeWithCallbacks onNext onError onCompleted (source : IObservable<_>) =
        source.SubscribeSafe (Observer.Create (Action<_> onNext, Action<_> onError, Action onCompleted))


    /// Transforms an observable sequence of observable sequences into an
    /// observable sequence producing values only from the most recent
    /// observable sequence.Each time a new inner observable sequnce is recieved,
    /// unsubscribe from the previous inner sequence
    let switch (sources:IObservable<IObservable<'Source>>) : IObservable<'Source>=
        Observable.Switch(sources)


    /// Transforms an observable sequence of tasks into an observable sequence
    /// producing values only from the most recent observable sequence.
    /// Each time a new task is received, the previous task's result is ignored.
    let switchTask (sources: IObservable<Threading.Tasks.Task<'Source>>) : IObservable<'Source> =
        Observable.Switch( sources )


    /// Transforms an observable sequence of F# Asyncs into an observable sequence
    /// producing values only from the most recent Async.
    /// Each time a new Async is received, the previous Async is cancelled,
    /// and will not continue to run in the background.
    let switchAsync (sources:IObservable<_>) =
        Observable.Switch(sources |> map ofAsync)


    /// Synchronizes the observable sequence so that notifications cannot be delivered concurrently
    /// this overload is useful to "fix" an observable sequence that exhibits concurrent
    /// callbacks on individual observers, which is invalid behavior for the query processor
    let synchronize  source : IObservable<'Source>=
        Observable.Synchronize( source )


    /// Synchronizes the observable sequence such that observer notifications
    /// cannot be delivered concurrently, using the specified gate object.This
    /// overload is useful when writing n-ary query operators, in order to prevent
    /// concurrent callbacks from different sources by synchronizing on a common gate object.
    let synchronizeGate (gate:obj)  (source:IObservable<'Source>): IObservable<'Source> =
        Observable.Synchronize( source, gate )


    /// Bypasses the first element in an observable sequence and then returns the remaining elements.
    let tail source = skip 1 source


    /// Takes n elements (from the beginning of an observable sequence? )
    let take (n: int) source : IObservable<'Source> =
        Observable.Take(source, n)


    /// Returns a specified number of contiguous elemenents from the start of an observable sequence,
    /// using the specified scheduler for the edge case of take(0).
    let takeOn (scheduler:IScheduler) (n:int) source =
        Observable.Take( source, n, scheduler )


    /// Takes elements for a specified duration from the start of the observable source sequence.
    let takeSpan (duration:TimeSpan) source =
        Observable.Take( source, duration )


    /// Takes elements for a specified duration from the start of the observable source sequence,
    /// using the specified scheduler to run timers.
    let takeSpanOn (scheduler:IScheduler) (duration:TimeSpan) source =
        Observable.Take( source, duration, scheduler )


    /// Returns a specified number of contiguous elements from the end of an obserable sequence
    let takeLast ( count:int ) source =
        Observable.TakeLast(source, count)


    /// Returns a specified number of contiguous elements from the end of an obserable sequence,
    /// using the specified scheduler to drain the queue.
    let takeLastOn (scheduler:IScheduler) (count:int) source =
        Observable.TakeLast( source, count, scheduler )


    /// Returns elements within the specified duration from the end of the observable source sequence.
    let takeLastSpan ( duration:TimeSpan ) ( source:IObservable<'Source> ): IObservable<'Source> =
        Observable.TakeLast( source, duration )


    /// Returns elements within the specified duration from the end of the observable source sequence,
    /// using the specified scheduler to run timers.
    let takeLastSpanOn (scheduler:IScheduler) (duration:TimeSpan) source =
        Observable.TakeLast( source, duration, scheduler)


    /// Returns a list with the elements within the specified duration from the end of the observable source sequence.
    let takeLastBuffer ( duration:TimeSpan )( source:IObservable<'Source> ): IObservable<Collections.Generic.IList<'Source>> =
        Observable.TakeLastBuffer( source, duration )


    /// Returns a list with the elements within the specified duration from the end of the observable source sequence,
    /// using the specified scheduler to run timers.
    let takeLastBufferOn (scheduler:IScheduler) duration source =
        Observable.TakeLastBuffer( source, duration, scheduler )


    /// Returns a list with the specified number of contiguous elements from the end of an observable sequence.
    let takeLastBufferCount ( count:int )( source:IObservable<'Source> ): IObservable<Collections.Generic.IList<'Source>> =
        Observable.TakeLastBuffer( source, count )


    /// Returns the elements from the source observable sequence until the other produces and element
    let takeUntilOther<'Other,'Source> other source =
        Observable.TakeUntil<'Source,'Other>(source , other )

//
    /// Returns the elements from the source observable until the specified time
    let takeUntilTime<'Source> (endtime:DateTimeOffset) source =
        Observable.TakeUntil<'Source>(source , endtime )


    /// Returns the elements from the source observable until the specified time,
    /// using the specified scheduler to run timers.
    let takeUntilTimeOn (scheduler:IScheduler) endtime source =
        Observable.TakeUntil( source, endtime, scheduler )


    /// Returns elements from an observable sequence as long as a specified condition is true.
    let takeWhile  (predicate) ( source:IObservable<'Source>): IObservable<'Source> =
        Observable.TakeWhile( source, Func<'Source,bool>predicate )


    /// Returns elements from an observable sequence as long as a specified condition is true.
    /// The element's index is used in the logic of the predicate functi
    let takeWhilei  ( predicate) (source:IObservable<'Source>) : IObservable<'Source> =
        Observable.TakeWhile( source, Func<'Source,int,bool> predicate )


    /// Ignores elements from an observable sequence which are followed by another element within a specified relative time duration.
    let throttle  (dueTime:TimeSpan) (source:IObservable<'Source>): IObservable<'Source> =
        Observable.Throttle( source, dueTime )

    /// Ignores elements from an observable sequence which are followed by another element within a specified relative time duration.
    let throttleOn (scheduler : IScheduler) (dueTime:TimeSpan) (source:IObservable<'Source>): IObservable<'Source> =
        Observable.Throttle( source, dueTime, scheduler )

    /// Ignores elements from an observable sequence which are followed by another value within a computed throttle duration
    let throttleComputed (throttleDurationSelector) ( source:IObservable<'Source>) : IObservable<'Source> =
        Observable.Throttle( source, Func<'Source,IObservable<'Throttle>> throttleDurationSelector )


    /// Returns an observable sequence that terminates with an exception.
    let throw ( except:exn ) : IObservable<'Result> =
        Observable.Throw( except )


    /// Returns an observable sequence that terminates with an exception.
    let throwWitness witness ex =
        Observable.Throw( ex, witness=witness )


    /// Returns an observable sequence that terminates with an exception,
    /// using the specified scheduler to send out the single OnError message.
    let throwOn scheduler ex =
        Observable.Throw( ex, scheduler=scheduler )


    /// Returns an observable sequence that terminates with an exception,
    /// using the specified scheduler to send out the single OnError message.
    let throwWitnessOn scheduler witeness ex =
        Observable.Throw( ex, witeness, scheduler )


    /// matches when the observable sequence has an available element and
    /// applies the map
    let thenMap map source =
        Observable.Then( source, Func<'Source,'Result> map )


    /// Records the time interval between consecutive elements in an observable sequence.
    let timeInterval ( source:IObservable<'Source>) : IObservable<TimeInterval<'Source>> =
        Observable.TimeInterval( source )


    /// Records the time interval between consecutive elements in an observable sequence,
    /// using the specified scheduler to compute time intervals.
    let timeIntervalOn scheduler source =
        Observable.TimeInterval( source, scheduler)


    /// Applies a timeout policy to the observable sequence based on an absolute time.
    /// If the sequence doesn't terminate before the specified absolute due time, a TimeoutException is propagated to the observer.
    let timeout ( timeout:System.DateTimeOffset ) ( source:IObservable<'Source>) =
        Observable.Timeout( source, timeout)


    /// Applies a timeout policy to the observable sequence based on an absolute time, using the specified scheduler to run timeout timers.
    /// If the sequence doesn't terminate before the specified absolute due time, a TimeoutException is propagated to the observer.
    let timeoutOn (scheduler:IScheduler) (timeout:DateTimeOffset) source =
        Observable.Timeout( source, timeout, scheduler )


    /// Applies a timeout policy to the observable sequence based on an absolute time.
    /// If the sequence doesn't terminate before the specified absolute due time, the other
    /// observable sequence is used to produce future messages from that point on.
    let timeoutOther ( timeout:System.DateTimeOffset ) ( other:IObservable<'Source>) ( source:IObservable<'Source>) =
        Observable.Timeout( source, timeout, other)


    /// Applies a timeout policy to the observable sequence based on an absolute time,
    /// using the specified scheduler to run timeout timers.
    /// If the sequence doesn't terminate before the specified absolute due time, the other
    /// observable sequence is used to produce future messages from that point on.
    let timeoutOtherOn (scheduler:IScheduler) (timeout:DateTimeOffset) other source =
        Observable.Timeout( source, timeout, other, scheduler )


    /// Applies a timeout policy for each element in the observable sequence.
    /// If the next element isn't received within the specified timeout duration starting from its
    /// predecessor, a TimeoutException is propagated to the observer.
    let timeoutSpan ( timeout:TimeSpan ) ( source:IObservable<'Source> ) =
        Observable.Timeout( source, timeout)


    /// Applies a timeout policy for each element in the observable sequence, using the specified scheduler to run timeout timers.
    /// If the next element isn't received within the specified timeout duration starting from its
    /// predecessor, a TimeoutException is propagated to the observer.
    let timeoutSpanOn (scheduler:IScheduler) (timeout:TimeSpan) source =
        Observable.Timeout( source, timeout, scheduler )


    /// Applies a timeout policy for each element in the observable sequence.
    /// If the next element isn't received within the specified timeout duration starting from
    /// its predecessor, the other observable sequence is used to produce future messages from that point on.
    let timeoutSpanOther( timeout:TimeSpan ) ( other:IObservable<'Source> ) ( source:IObservable<'Source> ) =
        Observable.Timeout( source, timeout, other)


    /// Applies a timeout policy for each element in the observable sequence, using the specified scheduler to run timeout timers.
    /// If the next element isn't received within the specified timeout duration starting from
    /// its predecessor, the other observable sequence is used to produce future messages from that point on.
    let timeoutSpanOtherOn (scheduler:IScheduler) (timeout:TimeSpan) other source =
        Observable.Timeout( source, timeout, other, scheduler)


    /// Applies a timeout policy to the observable sequence based on a timeout duration computed for each element.
    /// If the next element isn't received within the computed duration starting from its predecessor,
    /// a TimeoutException is propagated to the observer.
    let timeoutDuration ( durationSelector )( source:IObservable<'Source> ) : IObservable<'Source> =
        Observable.Timeout( source, Func<'Source,IObservable<'Timeout>> durationSelector   )


    /// Applies a timeout policy to the observable sequence based on an initial timeout duration
    /// for the first element, and a timeout duration computed for each subsequent element.
    /// If the next element isn't received within the computed duration starting from its predecessor,
    /// a TimeoutException is propagated to the observer.
    let timeout2Duration ( timeout:IObservable<'Timeout> )
                         ( durationSelector              )
                         ( source:IObservable<'Source>   ) =
        Observable.Timeout( source, timeout, Func<'Source, IObservable<'Timeout>> durationSelector)



    /// Applies a timeout policy to the observable sequence based on an initial timeout duration for the first
    /// element, and a timeout duration computed for each subsequent element.
    /// If the next element isn't received within the computed duration starting from its predecessor,
    /// the other observable sequence is used to produce future messages from that point on.
    let timeout2DurationOther   ( timeout: IObservable<'Timeout>)
                                ( durationSelector              )
                                ( other  : IObservable<'Source> )
                                ( source : IObservable<'Source> ) =
        Observable.Timeout( source, timeout, Func<'Source, IObservable<'Timeout>> durationSelector, other)


//    #endregion


    /// Returns an observable sequence that produces a single value at the specified absolute due time.
    let timer ( dueTime:DateTimeOffset ) : IObservable<int64> =
        Observable.Timer( dueTime )


    /// Returns an observable sequence that produces a single value at the specified absolute due time,
    /// using the specified scheduler to run the timer.
    let timerOn (scheduler:IScheduler) (dueTime:DateTimeOffset) =
        Observable.Timer( dueTime, scheduler )


    /// Returns an observable sequence that periodically produces a value starting at the specified initial absolute due time.
    let timerPeriod ( dueTime:DateTimeOffset) ( period:TimeSpan ) : IObservable<int64> =
        Observable.Timer( dueTime, period)


    /// Returns an observable sequence that produces a single value after the specified relative due time has elapsed.
    let timerSpan ( dueTime:TimeSpan ) : IObservable<int64> =
        Observable.Timer( dueTime )


    /// Returns an observable sequence that produces a single value after the specified relative due time has elapsed,
    /// using the specified scheduler to run the timer.
    let timerSpanOn (scheduler:IScheduler) (dueTime:TimeSpan) =
        Observable.Timer( dueTime, scheduler)


    /// Returns an observable sequence that periodically produces a value after the specified
    /// initial relative due time has elapsed.
    let timerSpanPeriod ( dueTime:TimeSpan, period:TimeSpan ) : IObservable<int64> =
        Observable.Timer( dueTime, period)


    /// Returns an observable sequence that periodically produces a value after the specified
    /// initial relative due time has elapsed, using the specified scheduler to run the timer.
    let timerSpanPeriodOn (scheduler:IScheduler) (dueTime:TimeSpan) (period:TimeSpan) =
        Observable.Timer( dueTime, period, scheduler)


    /// Timestamps each element in an observable sequence using the local system clock.
    let timestamp ( source:IObservable<'Source> ) : IObservable<Timestamped<'Source>> =
        Observable.Timestamp( source )

    /// Timestamps each element in an observable sequence using the supplied scheduler.
    let timestampOn (scheduler : IScheduler)  ( source:IObservable<'Source> ) : IObservable<Timestamped<'Source>> =
        Observable.Timestamp( source, scheduler )

    /// Converts an observable into a seq
    let toEnumerable (source: IObservable<'T>) = Observable.ToEnumerable(source)
    /// Creates an array from an observable sequence


    /// Creates an array from an observable sequence.
    let toArray  source =
        Observable.ToArray(source)

    /// Creates an observable sequence according to a specified key selector function
    let toDictionary keySelector source =
        Observable.ToDictionary(source, Func<_,_> keySelector)



    /// Creates an observable sequence according to a specified key selector function
    /// and an a comparer
    let toDictionaryComparer (keySelector:'Source->'Key) (comparer:'Key) (source:'Source) =
        Observable.ToDictionary( source, keySelector, comparer )


    /// Creates an observable sequence according to a specified key selector function
    let toDictionaryElements (keySelector:'Source->'Key )(elementSelector:'Source->'Elm) (source:'Source) =
        Observable.ToDictionary(source, keySelector, elementSelector)


    /// Creates an observable sequence according to a specified key selector function
    let toDictionaryCompareElements ( keySelector    : 'Source -> 'Key  )
                                    ( elementSelector: 'Source ->' Elm  )
                                    ( comparer:'Key ) ( source:'Source  ) =
        Observable.ToDictionary(    source                              ,
                                    Func<'Source,'Key> keySelector      ,
                                    Func<'Source,'Elm> elementSelector  ,
                                    comparer                            )


    /// Exposes an observable sequence as an object with an Action based .NET event
    let toEvent (source:IObservable<unit>) =
        Observable.ToEvent(source)


    /// Exposes an observable sequence as an object with an Action<'Source> based .NET event.
    let toEventType ( source:IObservable<'Source> ) : IEventSource<'Source> =
        Observable.ToEvent(source)


    /// Creates a list from an observable sequence
    let toList source =
        Observable.ToList(source)


    /// Creates a lookup from an observable sequence according to a specified key selector function.
    let toLookup ( keySelector )( source:IObservable<'Source> ) : IObservable<Linq.ILookup<'Key,'Source>> =
       Observable.ToLookup( source, Func<'Source,'Key> keySelector )


    /// Creates a lookup from an observable sequence according to a specified key selector function, and a comparer.
    let toLookupCompare ( keySelector ) ( comparer:IEqualityComparer<'Key> )( source:IObservable<'Source> ) : IObservable<Linq.ILookup<'Key,'Source>> =
       Observable.ToLookup( source,Func<'Source,'Key> keySelector, comparer)


    /// Creates a lookup from an observable sequence according to a specified key selector function, and an element selector function.
    let toLookupElement ( keySelector ) ( elementSelector ) ( comparer:IEqualityComparer<'Key>)( source:IObservable<'Source> ) : IObservable<Linq.ILookup<'Key,'Element>>=
       Observable.ToLookup( source, Func<'Source,'Key> keySelector, Func<'Source,'Element> elementSelector, comparer )


    /// Creates a lookup from an observable sequence according to a specified key selector function, and an element selector function.
    let toLookupCompareElement ( keySelector ) ( elementSelector )( source:IObservable<'Source> ) : IObservable<Linq.ILookup<'Key,'Element>> =
       Observable.ToLookup( source,Func<'Source,'Key>  keySelector, Func<'Source,'Element> elementSelector )


    /// Converts a seq into an observable
    let toObservable ( source: seq<'T> ) = Observable.ToObservable(source)


    /// Constructs an observable sequence that depends on a resource object, whose
    /// lifetime is tied to the resulting observable sequence's lifetime.
    let using ( resourceFactory: unit ->'TResource ) (observableFactory: 'TResource -> IObservable<'Result> ) : IObservable<'Result> =
        Observable.Using ( Func<_> resourceFactory, Func<_,_> observableFactory )


    /// Constructs an observable sequence that depends on a resource object, whose
    /// lifetime is tied to the resulting observable sequence's lifetime.
    /// The resource is obtained and used through asynchronous functions.
    /// The cancellation token passed to the asyncrhonous functions is tied to the returned disposable subscription,
    /// allowing best-effor cancellation at any stage of the resource acquisition or usage.
    let usingAsync resourceFactory observableFactory =
        Observable.Using ( Func<_, _> (resourceFactory >> Async.StartAsTask), Func< _, _, _> (fun d ct -> observableFactory d ct |> Async.StartAsTask))


    /// waits for the observable sequence to complete and returns the last
    /// element of the sequence. If the sequence terminates with OnError
    /// notification, the exception is thrown
    let wait  source =
        Observable.Wait( source )


    /// Repeats the given function as long as the specified condition holds
    /// where the condition is evaluated before each repeated source is
    /// subscribed to
    let whileLoop condition source =
        Observable.While( Func<bool> condition, source )


    /// Projects each element of an observable sequence into consecutive non-overlapping windows.
    /// windowClosingSelector - A function invoked to define the boundaries of the produced windows.
    /// A new window is started when the previous one is closed
    let window ( windowClosingSelector ) ( source:IObservable<'Source> ) : IObservable<IObservable<'Source>> =
        Observable.Window( source, Func<IObservable<'WindowClosing>> windowClosingSelector)


    /// Projects each element of an observable sequence into consecutive non-overlapping windows
    /// which are produced based on timing information.
    let windowTimeSpan ( timeSpan:TimeSpan )( source:IObservable<'Source> ) : IObservable<IObservable<'Source>> =
        Observable.Window( source, timeSpan )


   /// Projects each element of an observable sequence into consecutive non-overlapping windows
    /// which are produced based on timing information, using the specified scheduler to run timers.
    let windowTimeSpanOn (scheduler:IScheduler) timeSpan source =
        Observable.Window( source, timeSpan, scheduler )


    /// Projects each element of an observable sequence into zero or more windows.
    /// windowOpenings - Observable sequence whose elements denote the creation of new windows.
    /// windowClosingSelector - A function invoked to define the closing of each produced window.
    let windowOpenClose ( windowOpenings        : IObservable<'WinOpen>             )
                        ( windowClosingSelector : 'WinOpen->IObservable<'WinClose>  )
                        ( source                : IObservable<'Source>              ) : IObservable<IObservable<'Source>> =
        Observable.Window(source, windowOpenings, Func<_,_> windowClosingSelector)


    /// Projects each element of an observable sequence into consecutive non-overlapping windows.
    /// windowBoundaries - Sequence of window boundary markers. The current window is closed and a new window is opened upon receiving a boundary marker.
    let windowTimeShift ( timeSpan:TimeSpan )( timeShift:TimeSpan )( source:IObservable<'Source> ) : IObservable<IObservable<'Source>> =
        Observable.Window( source, timeSpan, timeShift )


    /// Projects each element of an observable sequence into consecutive non-overlapping windows, using the specified scheduler to run timers.
    /// windowBoundaries - Sequence of window boundary markers. The current window is closed and a new window is opened upon receiving a boundary marker.
    let windowTimeShiftOn (scheduler:IScheduler) (timeSpan:TimeSpan) (timeShift:TimeSpan) source =
        Observable.Window( source, timeSpan, timeShift, scheduler)


    /// Projects each element of an observable sequence into consecutive non-overlapping windows
    /// windowBoundaries - Sequence of window boundary markers. The current window is closed
    /// and a new window is opened upon receiving a boundary marker
    let windowBounded    ( windowBoundaries:IObservable<'WindowBoundary> )( source:IObservable<'Source> ) : IObservable<IObservable<'Source>> =
        Observable.Window( source, windowBoundaries )


    /// Projects each element of an observable sequence into zero or more windows which are produced based on element count information
    let windowCountSkip ( count:int )( skip:int ) ( source:IObservable<'Source> ): IObservable<IObservable<'Source>> =
        Observable.Window( source, count, skip )


    /// Projects each element of an observable sequence into consecutive non-overlapping windows
    /// which are produced based on element count information.
    let windowCount ( count:int )( source:IObservable<'Source> ) : IObservable<IObservable<'Source>> =
        Observable.Window( source, count )


    /// Projects each element of an observable sequence into a window that is completed when either it's full or
    /// a given amount of time has elapsed.
    /// A useful real-world analogy of this overload is the behavior of a ferry leaving the dock when all seats are
    /// taken, or at the scheduled time of departure, whichever event occurs first
    let windowTimeCount ( timeSpan:TimeSpan ) (count:int) ( source:IObservable<'Source> ): IObservable<IObservable<'Source>> =
        Observable.Window( source, timeSpan, count )


    /// Projects each element of an observable sequence into a window that is completed when either it's full or
    /// a given amount of time has elapsed, using the specified scheduler to run timers.
    /// A useful real-world analogy of this overload is the behavior of a ferry leaving the dock when all seats are
    /// taken, or at the scheduled time of departure, whichever event occurs first
    let windowTimeCountOn (scheduler:IScheduler) (timeSpan:TimeSpan) (count:int) source =
        Observable.Window( source, timeSpan, count, scheduler)


    /// Merges the specified observable sequences into one observable sequence by using the selector function
    /// only when the first observable sequence produces an element and there was some element produced by the second
    /// observable sequence
    let withLatestFrom resultSelector second first =
        Observable.WithLatestFrom( first, second, Func<_,_,_> resultSelector )


    /// Merges two observable sequences into one observable sequence of pairs.
    let zip ( first:IObservable<'Source1> ) ( second:IObservable<'Source2> ) : IObservable<'Source1 * 'Source2> =
        Observable.Zip( first, second, fun a b -> a,b)


    /// Merges three observable sequences into one observable sequence of triples.
    let zip3 ( first:IObservable<'Source1> ) ( second:IObservable<'Source2> ) ( third:IObservable<'Source3> ) : IObservable<'Source1 * 'Source2 * 'Source3> =
        Observable.Zip( first, second, third, fun a b c -> a,b,c)


    /// Merges two observable sequences into one observable sequence by combining their elements through a projection function.
    let zipWith ( resultSelector:'Source1 -> 'Source2 -> 'Result) ( first:IObservable<'Source1>) ( second:IObservable<'Source2>)  : IObservable<'Result> =
        Observable.Zip( first, second, Func<'Source1,'Source2,'Result> resultSelector)


    /// Merges the specified observable sequences into one observable sequence by emitting a
    ///  list with the elements of the observable sequences at corresponding indexes.
    let zipSeq ( sources:seq<IObservable<'Source>>) : IObservable<IList<'Source>> =
        Observable.Zip( sources )


    /// Merges the specified observable sequences into one observable sequence by emitting
    /// a list with the elements of the observable sequences at corresponding indexe
    let zipArray ( sources:IObservable<'Source> []) : IObservable<IList<'Source>> =
        Observable.Zip( sources )


    /// Merges the specified observable sequences into one observable sequence by using
    /// the selector function whenever all of the observable sequences have produced an
    /// element at a corresponding index.
    let zipSeqMap ( resultSelector: IList<'S> ->'R) ( sources: seq<IObservable<'S>>)  : IObservable<'R> =
        Observable.Zip( sources, Func<IList<'S>,'R> resultSelector)



    /// Merges an observable sequence and an enumerable sequence into one
    /// observable sequence by using the selector function.
    let zipWithSeq ( resultSelector: 'Source1 -> 'Source2 -> 'Result   )
                   ( second        : seq<'Source2>                       )
                   ( first         : IObservable<'Source1>               ) : IObservable<'Result> =
        Observable.Zip(first, second, Func<_,_,_> resultSelector )


(***************************************************************
 * F# extra combinator wrappers for Rx extensions
 ***************************************************************)

    /// **Description**
    /// Periodically repeats the observable sequence exposing a responses or failures.
    ///
    /// **Parameters**
    /// - `sch` - Accepts the scheduler on which the polling period should be run.
    /// - `period` - Accepts the period in which the polling should happen.
    /// - `source` - The source observable on which the polling happen.
    let pollOn sch period source =
        timerOn sch period
        |> bind (fun _ -> source)
        |> catchWith (Result.Error >> single)
        |> repeat

    /// **Description**
    /// Periodically repeats the observable sequence exposing a responses or failures.
    /// Using the `Scheduler.Default` on which the polling period should run.
    ///
    /// **Parameters**
    /// - `period` - Accepts the period in which the polling should happen.
    /// - `source` - The source observable on which the polling happen.
    let poll period source =
        pollOn Scheduler.Default period source

    /// **Description**
    /// Applies the given function to each value of the given source Observable
    /// comprimised of the results x for each element for which the function returns `Some(x)`.
    ///
    /// **Parameters**
    /// - `f` - Accepts a chooser function to only pick a subset of emits.
    /// - `source` - The source observable to take a subset from.
    let choose f source =
        Observable.Create (fun (o : IObserver<_>) ->
            subscribeSafeWithCallbacks
                (fun x -> Option.iter o.OnNext (try f x with ex -> o.OnError ex; None))
                o.OnError
                o.OnCompleted
                source)

    /// **Description**
    /// Projects each source value to an Observable which is merged in the output
    /// Observable, emitting values only from the most recently projected Observable.
    /// Equivalent to a map followed by a switch.
    ///
    /// **Returns**
    /// Returns an Observable that emits items based on applying a function that you
    /// supply to each item emitted by the source Observable, where that function
    /// returns an (so-called "inner") Observable. Each time it observes one of these
    /// inner Observables, the output Observable begins emitting the items emitted by
    /// that inner Observable. When a new inner Observable is emitted, `switchMap`
    /// stops emitting items from the earlier-emitted inner Observable and begins
    /// emitting items from the new one. It continues to behave like this for
    /// subsequent inner Observables.
    let switchMap f source =
        source |> map f |> switch

    /// Projects each source value to an Observable which is merged in the output
    /// Observable only if the previous projected Observable has completed.
    //
    /// **Returns**
    /// Returns an Observable that emits items based on applying a function that you
    /// supply to each item emitted by the source Observable, where that function
    /// returns an (so-called "inner") Observable. When it projects a source value to
    /// an Observable, the output Observable begins emitting the items emitted by
    /// that projected Observable. However, `exhaustMap` ignores every new projected
    ///  Observable if the previous projected Observable has not yet completed. Once
    /// that one completes, it will accept and flatten the next projected Observable
    /// and repeat this process.
    let exhaustMap f source =
        Observable.Create (fun (o : IObserver<_>) ->
            let mutable hasSubscription = false
            let mutable innerSub = None
            let onInnerCompleted () =
                hasSubscription <- false
                innerSub |> Option.iter Disposable.dispose
            let onOuterNext x =
                if not hasSubscription then
                    hasSubscription <- true
                    f x |> subscribeSafeWithCallbacks
                            o.OnNext o.OnError onInnerCompleted
                        |> fun y -> innerSub <- Some y
            source
            |> subscribeSafeWithCallbacks
                onOuterNext o.OnError o.OnCompleted)

(***************************************************************
 * F# advanced combinator wrappers for Rx extensions
   >> Inspired by the C# Rxx: https://github.com/RxDave/Rxx <<
 ***************************************************************)

    let private serveInternal sch max callOnError sourceFactory =
        Observable.Create (fun (o : IObserver<'a>) ->
            let subscribeSource self =
                let onError ex =
                    if callOnError ex then o.OnError ex
                    else self ()
                sourceFactory ()
                |> subscribeSafeWithCallbacks
                    o.OnNext onError self

            let com = Disposable.Composite
            for _ in 1..max do
                let current = new SerialDisposable ()
                com.Add current
                sch |> Schedule.actionRec (fun self ->
                        current
                        |> Disposable.setIndirectly
                            (fun () -> subscribeSource self))
                        |> com.Add
            com :> IDisposable)

    /// **Description**
    /// Concurrently invokes the specified factory to create observables as fast and often as possible and subscribes to all of them
    /// up to the default maximum concurrency.
    ///
    /// **Parameters**
    /// - `sch` - The scheduler on which the invocation should happen.
    /// - `gate` - Common gate on which the synchronization should happen.
    /// - `max` - The maximum number of observables to be subscribed simultaneously.
    /// - `callOnError` - Function to determine whether or not the given exception should call the `OnError`.
    /// - `sourceFactory` - Function that returns the observable to invoke concurrently.
    let serveGateCustomOn sch gate max callOnError sourceFactory =
        serveInternal sch max callOnError sourceFactory |> synchronizeGate gate

    /// **Description**
    /// Concurrently invokes the specified factory to create observables as fast and often as possible and subscribes to all of them
    /// up to the default maximum concurrency.
    ///
    /// **Parameters**
    /// - `sch` - The scheduler on which the invocation should happen.
    /// - `max` - The maximum number of observables to be subscribed simultaneously.
    /// - `callOnError` - Function to determine whether or not the given exception should call the `OnError`.
    /// - `sourceFactory` - Function that returns the observable to invoke concurrently.
    let serveCustomOn sch max callOnError sourceFactory =
        serveInternal sch max callOnError sourceFactory |> synchronize

    /// **Description**
    /// Concurrently invokes the specified factory to create observables as fast and often as possible and subscribes to all of them
    /// up to the default maximum concurrency.
    ///
    /// **Parameters**
    /// - `sch` - The scheduler on which the invocation should happen.
    /// - `gate` - Common gate on which the synchronization should happen.
    /// - `max` - The maximum number of observables to be subscribed simultaneously.
    /// - `sourceFactory` - Function that returns the observable to invoke concurrently.
    let serveOn sch max sourceFactory =
        serveCustomOn sch max (fun _ -> true) sourceFactory

    /// **Description**
    /// Concurrently invokes the specified factory to create observables as fast and often as possible and subscribes to all of them
    /// up to the default maximum concurrency.
    ///
    /// **Parameters**
    /// - `max` - The maximum number of observables to be subscribed simultaneously.
    /// - `sourceFactory` - Function that returns the observable to invoke concurrently.
    let serve max sourceFactory =
        serveOn Scheduler.CurrentThread max sourceFactory

    /// Generates a sequence using the producer/consumer pattern.
    /// The purpose of the source sequence is simply to notify the consumer when out-of-band data becomes available.
    /// The data in the source sequence provides additional information to the function,
    /// but it does not have to be the actual data being produced.
    ///
    /// The function is not necessarily called for every value in the source sequence.
    /// It is only called if the previous consumer's observable has completed; otherwise, the current notification is ignored.  This ensures
    /// that only one consumer is active at any given time, but it also means that the function is not guaranteed
    /// to receive every value in the source sequence; therefore, the function must read
    /// data from out-of-band storage instead; e.g., from a shared stream or queue.
    ///
    /// The function may also be called when data is not available.  For example, if the current consuming
    /// observable completes and additional notifications from the source were received, then the function
    /// is called again to check whether new data was missed. This avoids a race condition between the source sequence
    /// and the consuming observable's completion notification. If no data is available when function is called, then
    /// an empty sequence should be returned and the function will not be called again until another notification is observed
    /// from the source.
    ///
    /// Producers and the single active consumer are intended to access shared objects concurrently, yet it remains their responsibility
    /// to ensure thread-safety. The consume operator cannot do so without breaking concurrency. For example,
    /// a producer/consumer implementation that uses an in-memory queue must manually ensure that reads and writes to the queue are thread-safe.
    ///
    /// Multiple producers are supported.  Simply create an observable sequence for each producer that notifies when data is generated,
    /// merge them together using the merge operator, and use the merged observable as the source argument in the consume operator.
    /// Multiple consumers are supported by calling consume once and then calling subscribe multiple times on the cold observable that is returned.
    /// Just be sure that the source sequence is hot so that each subscription will consume based on the same producers' notifications.
    ///
    /// ## Parameters
    /// - `f` - A function that generates an observable sequence from out-of-band data.
    /// - `source` - Indicates when data becomes available from one or more producers.
    ///
    /// ## Returns
    /// An observable sequence that is the concatenation of all subscriptions to the consumer observable.
    let consumeMap (f : 'a -> IObservable<'b>) (source : IObservable<'a>) =
        Observable.Create (fun (o : IObserver<_>) ->
            let gate = obj ()
            let consumingSubscription = new SerialDisposable ()
            let schedule = new SerialDisposable ()

            let mutable lastSkipped = Unchecked.defaultof<'a>
            let mutable hasSkipped = false
            let mutable consuming = false
            let mutable stopped = false

            let onNext value =
                lock gate (fun () ->
                    if consuming
                        then lastSkipped <- value
                             hasSkipped <- true
                        else consuming <- true
                             hasSkipped <- false)

                let mutable additionalData = value
                let scheduleRec self =
                    let onCompleted () =
                        let consumeAgain, completeNow =
                            lock gate (fun () ->
                                consuming <- hasSkipped
                                if consuming then
                                    additionalData <- lastSkipped
                                    hasSkipped <- false
                                    true, false
                                else false, stopped)

                        if consumeAgain then self ()
                        if completeNow then o.OnCompleted ()

                    try Some (f additionalData)
                    with | ex -> o.OnError ex; None
                    |> Option.iter (fun xs ->
                        consumingSubscription
                        |> Disposable.setIndirectly (fun () ->
                            subscribeSafeWithCallbacks
                                o.OnNext o.OnError onCompleted xs))

                Scheduler.Immediate
                |> Schedule.actionRec scheduleRec
                |> Disposable.setInnerDisposalOf schedule

            let onCompleted () =
                let completeNow = lock gate (fun () ->
                    stopped <- true; consuming)
                if completeNow then o.OnCompleted ()

            let subscription =
                subscribeSafeWithCallbacks
                    onNext o.OnError onCompleted source

            Disposables.compose [ consumingSubscription; subscription; schedule ])

    /// Generates a sequence using the producer/consumer pattern.
    /// The purpose of the source sequence is simply to notify the consumer when out-of-band data becomes available.
    /// The data in the source sequence provides additional information to the function,
    /// but it does not have to be the actual data being produced.
    ///
    /// The function is not necessarily called for every value in the source sequence.
    /// It is only called if the previous consumer's observable has completed; otherwise, the current notification is ignored.  This ensures
    /// that only one consumer is active at any given time, but it also means that the function is not guaranteed
    /// to receive every value in the source sequence; therefore, the function must read
    /// data from out-of-band storage instead; e.g., from a shared stream or queue.
    ///
    /// The function may also be called when data is not available.  For example, if the current consuming
    /// observable completes and additional notifications from the source were received, then the function
    /// is called again to check whether new data was missed. This avoids a race condition between the source sequence
    /// and the consuming observable's completion notification. If no data is available when function is called, then
    /// an empty sequence should be returned and the function will not be called again until another notification is observed
    /// from the source.
    ///
    /// Producers and the single active consumer are intended to access shared objects concurrently, yet it remains their responsibility
    /// to ensure thread-safety. The consume operator cannot do so without breaking concurrency. For example,
    /// a producer/consumer implementation that uses an in-memory queue must manually ensure that reads and writes to the queue are thread-safe.
    ///
    /// Multiple producers are supported.  Simply create an observable sequence for each producer that notifies when data is generated,
    /// merge them together using the merge operator, and use the merged observable as the source argument in the consume operator.
    /// Multiple consumers are supported by calling consume once and then calling subscribe multiple times on the cold observable that is returned.
    /// Just be sure that the source sequence is hot so that each subscription will consume based on the same producers' notifications.
    ///
    /// ## Parameters
    /// - `f` - A function that generates an observable sequence from out-of-band data.
    /// - `source` - Indicates when data becomes available from one or more producers.
    ///
    /// ## Returns
    /// An observable sequence that is the concatenation of all subscriptions to the consumer observable.
    let consume consumer source = consumeMap (fun _ -> consumer) source

    /// Generates a sequence using the producer/consumer pattern.
    /// The purpose of the source sequence is simply to notify the consumer when out-of-band data becomes available.
    /// The data in the source sequence provides additional information to the function,
    /// but it does not have to be the actual data being produced.
    ///
    /// The function is not necessarily called for every value in the source sequence.
    /// It is only called if the previous consumer's observable has completed; otherwise, the current notification is ignored.  This ensures
    /// that only one consumer is active at any given time, but it also means that the function is not guaranteed
    /// to receive every value in the source sequence; therefore, the function must read
    /// data from out-of-band storage instead; e.g., from a shared stream or queue.
    ///
    /// The function may also be called when data is not available.  For example, if the current consuming
    /// observable completes and additional notifications from the source were received, then the function
    /// is called again to check whether new data was missed. This avoids a race condition between the source sequence
    /// and the consuming observable's completion notification. If no data is available when function is called, then
    /// an empty sequence should be returned and the function will not be called again until another notification is observed
    /// from the source.
    ///
    /// Producers and the single active consumer are intended to access shared objects concurrently, yet it remains their responsibility
    /// to ensure thread-safety. The consume operator cannot do so without breaking concurrency. For example,
    /// a producer/consumer implementation that uses an in-memory queue must manually ensure that reads and writes to the queue are thread-safe.
    ///
    /// Multiple producers are supported.  Simply create an observable sequence for each producer that notifies when data is generated,
    /// merge them together using the merge operator, and use the merged observable as the source argument in the consume operator.
    /// Multiple consumers are supported by calling consume once and then calling subscribe multiple times on the cold observable that is returned.
    /// Just be sure that the source sequence is hot so that each subscription will consume based on the same producers' notifications.
    ///
    /// ## Parameters
    /// - `f` - A function that is called iteratively to generate values from out-of-band data.
    /// - `source` - Indicates when data becomes available from one or more producers.
    ///
    /// ## Returns
    /// An observable sequence that is the concatenation of the values returned by the consumeNext function.
    let consumeNextOn sch f source =
        let rec onNext (o : IObserver<_>) x =
            try f x
            with ex -> o.OnError ex; None
            |> function
                | Some next -> o.OnNext next; onNext o x
                | None -> o.OnCompleted ()
        source
        |> consumeMap (fun x ->
            Observable.Create (fun o ->
                Schedule.action (fun () -> onNext o x) sch))

    /// Generates a sequence using the producer/consumer pattern.
    /// The purpose of the source sequence is simply to notify the consumer when out-of-band data becomes available.
    /// The data in the source sequence provides additional information to the function,
    /// but it does not have to be the actual data being produced.
    ///
    /// The function is not necessarily called for every value in the source sequence.
    /// It is only called if the previous consumer's observable has completed; otherwise, the current notification is ignored.  This ensures
    /// that only one consumer is active at any given time, but it also means that the function is not guaranteed
    /// to receive every value in the source sequence; therefore, the function must read
    /// data from out-of-band storage instead; e.g., from a shared stream or queue.
    ///
    /// The function may also be called when data is not available.  For example, if the current consuming
    /// observable completes and additional notifications from the source were received, then the function
    /// is called again to check whether new data was missed. This avoids a race condition between the source sequence
    /// and the consuming observable's completion notification. If no data is available when function is called, then
    /// an empty sequence should be returned and the function will not be called again until another notification is observed
    /// from the source.
    ///
    /// Producers and the single active consumer are intended to access shared objects concurrently, yet it remains their responsibility
    /// to ensure thread-safety. The consume operator cannot do so without breaking concurrency. For example,
    /// a producer/consumer implementation that uses an in-memory queue must manually ensure that reads and writes to the queue are thread-safe.
    ///
    /// Multiple producers are supported.  Simply create an observable sequence for each producer that notifies when data is generated,
    /// merge them together using the merge operator, and use the merged observable as the source argument in the consume operator.
    /// Multiple consumers are supported by calling consume once and then calling subscribe multiple times on the cold observable that is returned.
    /// Just be sure that the source sequence is hot so that each subscription will consume based on the same producers' notifications.
    ///
    /// ## Parameters
    /// - `f` - A function that is called iteratively to generate values from out-of-band data.
    /// - `source` - Indicates when data becomes available from one or more producers.
    ///
    /// ## Returns
    /// An observable sequence that is the concatenation of the values returned by the consumeNext function.
    let consumeNext f source = consumeNextOn Scheduler.Default f source
