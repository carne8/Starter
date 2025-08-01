module Starter.WorkspaceSearchEngine.WorkspaceSourceProvider

open System
open System.IO
open FsToolkit.ErrorHandling

// type JetBrainsIde =
//     | IntelliJ
//     | PyCharm
//     | PhpStorm
//     | GoLand
//     | Rider
//     | CLion
//     | RustRover
//     | WebStorm
//     | RubyMine
//
// type Software =
//     | VsCode of insiders: bool
//     | VisualStudio
//     | JetBrains of JetBrainsIde
//     | GitKraken
//     | Godot

let private getVsCodeWorkspaceSource insiders pluginPath =
    insiders
    |> WorkspaceSources.VsCode.findVsCode
    |> Option.map (fun vsCodePath ->
        let icon =
            match insiders with
            | true -> Icons.IconName.vsCodeInsiders
            | false -> Icons.IconName.vsCode
            |> Icons.loadIcon pluginPath

        { Icon = icon
          LoadWorkspaces = fun () -> WorkspaceSources.VsCode.loadWorkspaces vsCodePath insiders
          WorkspacesChanged = R3.Observable.Empty() }
    )

let loadWorkspaceSources pluginPath =
    [| pluginPath |> getVsCodeWorkspaceSource false
       pluginPath |> getVsCodeWorkspaceSource true |]
    |> Array.choose id
