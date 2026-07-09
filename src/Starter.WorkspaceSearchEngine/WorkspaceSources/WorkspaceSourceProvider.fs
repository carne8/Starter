module Starter.WorkspaceSearchEngine.WorkspaceSourceProvider

open ObservableCollections
open Starter.WorkspaceSearchEngine.Logger

// type Software =
//     | VisualStudio
//     | GitKraken
//     | Godot

let loadWorkspaceSources pluginPath (settings: Settings) : WorkspaceSource array =
    [| WorkspaceSources.VsCode.builder false
       WorkspaceSources.VsCode.builder true
       WorkspaceSources.JetBrains.builder WorkspaceSources.JetBrains.JetBrainsIDE.Rider
       WorkspaceSources.JetBrains.builder WorkspaceSources.JetBrains.JetBrainsIDE.PyCharm
       WorkspaceSources.JetBrains.builder (WorkspaceSources.JetBrains.JetBrainsIDE.IntelliJ true)
       WorkspaceSources.JetBrains.builder (WorkspaceSources.JetBrains.JetBrainsIDE.IntelliJ false)
       WorkspaceSources.JetBrains.builder WorkspaceSources.JetBrains.JetBrainsIDE.GoLand
       WorkspaceSources.JetBrains.builder WorkspaceSources.JetBrains.JetBrainsIDE.PhpStorm
       WorkspaceSources.JetBrains.builder WorkspaceSources.JetBrains.JetBrainsIDE.WebStorm
       WorkspaceSources.JetBrains.builder WorkspaceSources.JetBrains.JetBrainsIDE.RubyMine
       WorkspaceSources.JetBrains.builder WorkspaceSources.JetBrains.JetBrainsIDE.RustRover
       WorkspaceSources.JetBrains.builder WorkspaceSources.JetBrains.JetBrainsIDE.CLion
       WorkspaceSources.AndroidStudio.builder
       WorkspaceSources.WindowsTerminal.builder |]
    |> Array.choose (fun builder ->
        builder
        |> WorkspaceSourceBuilder.build false pluginPath
        |> Option.map (fun source ->
            let showIfNoActivator =
                match settings.ShowIfNoActivator.TryGetValue source.Id with
                | true, v -> v
                | false, _ ->
                    settings.ShowIfNoActivator.Add(source.Id, false)
                    false

            source.ShowIfNoActivator <- showIfNoActivator
            source
        )
    )
    |> Array.sortBy _.Name
    |> fun workspaceSources ->
        settings.ShowIfNoActivator.add_CollectionChanged(NotifyCollectionChangedEventHandler(fun args ->
            let sourceId = args.NewItem.Key
            let source =
                workspaceSources
                |> Array.tryFind (fun source -> source.Id = sourceId)

            match source with
            | None -> logger.Error $"Cannot apply \"show if activator\" settings to {sourceId}: Workspace source not found"
            | Some source -> source.ShowIfNoActivator <- args.NewItem.Value
        ))

        workspaceSources
