namespace Starter.Features.InternalSearchEngines.Settings.ViewModels

open R3
open Starter.SearchEngine

type ActivatorPrefixViewModel(activator: ISearchEngineActivator, basePrefix, onPrefixChanged: string -> unit) =
    let mutable prefix = basePrefix

    member this.Icon = activator.Icon
    member this.Name = activator.Name
    member this.Prefix
        with get () = prefix
        and set v = prefix <- v; v |> onPrefixChanged

type SearchEngineActivatorsViewModel(se: SearchEngine, activators: Observable<struct (ISearchEngineActivator * string) seq>, onPrefixChanged) =
    let activatorPrefixVms =
        activators |> Observable.map (Array.ofSeq >> function
            | [| struct (:? DefaultSearchEngineActivator as activator, prefix) |] ->
                Choice1Of2 <| ActivatorPrefixViewModel(activator, prefix, onPrefixChanged activator.Id)
            | activators ->
                activators
                |> Array.map (fun struct (activator, prefix) ->
                    ActivatorPrefixViewModel(activator, prefix, onPrefixChanged activator.Id)
                )
                |> Choice2Of2
        )

    member this.Icon = se.Icon
    member this.Name = se.Name
    member this.ActivatorPrefixVms = activatorPrefixVms
