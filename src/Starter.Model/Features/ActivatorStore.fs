namespace Starter.Features

open System
open Starter.SearchEngine
open Starter.Features.Config
open R3
open System.Collections.Generic

[<Struct>]
type private ActivatorPrefixPair =
    { Activator: ISearchEngineActivator
      Prefix: string }

/// Keep an up-to-date store of activator associated with a prefix
type ActivatorStore(configObservable: BehaviorSubject<Configuration>) as this =
    let activatorList = List<ISearchEngineActivator>()
    let pairList = List<ActivatorPrefixPair>()

    let mutable config = configObservable.Value
    let sub = configObservable.Subscribe this.SetConfig

    interface IDisposable with
        member _.Dispose() = sub.Dispose()

    member _.SetConfig(newConfig) =
        config <- newConfig

        activatorList
        |> Seq.choose (fun activator ->
            newConfig.ActivatorPrefixes
            |> Map.tryFind activator.Id
            |> Option.map (fun prefix -> { Activator = activator; Prefix = prefix })
        )
        |> Seq.toArray
        |> fun newListContent ->
            pairList.Clear()
            pairList.AddRange(newListContent)

    member _.AddSearchEngineActivators(searchEngine: SearchEngine) =
        searchEngine.LoadActivators()
        searchEngine.Activators.Subscribe(fun activators ->
            // Remove previous activators associated with this search engine
            pairList.RemoveAll(fun pair -> pair.Activator.SearchEngineId = searchEngine.Id) |> ignore
            activatorList.RemoveAll(fun activator -> activator.SearchEngineId = searchEngine.Id) |> ignore

            // Add activators
            activators |> activatorList.AddRange

            activators
            |> Seq.choose (fun activator ->
                config.ActivatorPrefixes
                |> Map.tryFind activator.Id
                |> Option.map (fun prefix -> { Activator = activator; Prefix = prefix })
            )
            |> pairList.AddRange
        ) |> ignore

    member _.GetActivatorFromText(text: string) =
        pairList
        |> Seq.tryFind (fun pair -> text.StartsWith pair.Prefix)
        |> function
            | Some pair -> ValueSome struct (pair.Activator, pair.Prefix)
            | None -> ValueNone
