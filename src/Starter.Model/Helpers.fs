[<AutoOpen>]
module Helpers

open System
open System.Threading
open Starter.SearchEngine

open FluentAvalonia.UI.Controls

type StarterIconSource with
    /// Transform StarterIconSource in IconSource
    static member buildIconSource lightMode (iconSource: StarterIconSource) =
        match iconSource.Geometry with
        | null -> ImageIconSource(Source = iconSource.GetImage lightMode) :> IconSource
        | geo -> PathIconSource(Data = geo)

let disposeOnCancelled (ct: CancellationToken) (d: IDisposable) =
    fun () -> d.Dispose()
    |> ct.Register
    |> ignore

[<RequireQualifiedAccess>]
module Observable =
    open R3

    let inline map ([<InlineIfLambda>] f: 'A -> 'B) (obs: Observable<'A>) : Observable<'B>  = obs.Select(f)
    let inline subscribe ([<InlineIfLambda>] f: 'A -> unit) (obs: Observable<'A>)  = obs.Subscribe(f)

[<RequireQualifiedAccess>]
module Result =
    let inline ofOption error opt =
        match opt with
        | None -> Error error
        | Some v -> Ok v

    let inline ofValueOption error opt =
        match opt with
        | ValueNone -> Error error
        | ValueSome v -> Ok v

    let inline requireNotNull error (ok: 'a | null) : Result<'a, _> =
        match ok with
        | null -> Error error
        | other -> Ok other
