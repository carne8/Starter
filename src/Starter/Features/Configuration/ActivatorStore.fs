namespace Starter.Features.Config

open Starter.SearchEngine
open R3
open System.Collections.Generic

[<Struct>]
type private ActivatorPrefixPair =
    { Activator: ISearchEngineActivator
      Prefix: string }

/// Keep an up-to-date store of activator associated with a prefix
type ActivatorStore(config: BehaviorSubject<Configuration>) =
    let activatorList = List<ISearchEngineActivator>()
    let list = List<ActivatorPrefixPair>()

    do
        config
        |> Observable.subscribe (fun newConfig ->
            activatorList
            |> Seq.choose (fun activator ->
                newConfig.ActivatorPrefixes
                |> Map.tryFind activator.Id
                |> Option.map (fun prefix ->
                    { Activator = activator
                      Prefix = prefix }
                )
            )
            |> Seq.toArray
            |> fun newListContent ->
                list.Clear()
                list.AddRange(newListContent)
        )
        |> ignore

    member _.GetActivatorFromText(text: string) =
        list
        |> Seq.tryFind (fun pair -> text.StartsWith pair.Prefix)
        |> Option.map (fun pair -> struct (pair.Activator, pair.Prefix))

    member _.AddSearchEngineActivators(searchEngine: SearchEngine) =
        searchEngine.LoadActivators()
        searchEngine.Activators.Subscribe(fun activators ->
            // Remove previous activators associated with this search engine
            list.RemoveAll(fun pair -> pair.Activator.SearchEngineId = searchEngine.Id) |> ignore
            activatorList.RemoveAll(fun activator -> activator.SearchEngineId = searchEngine.Id) |> ignore

            // Add activators
            activators |> activatorList.AddRange

            activators
            |> Seq.choose (fun activator ->
                config.Value.ActivatorPrefixes
                |> Map.tryFind activator.Id
                |> Option.map (fun prefix ->
                    { Activator = activator
                      Prefix = prefix }
                )
            )
            |> list.AddRange
        ) |> ignore
