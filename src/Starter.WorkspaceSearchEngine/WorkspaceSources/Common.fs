module Starter.WorkspaceSearchEngine.WorkspaceSources.Common

open System
open System.IO

let private path =
    "PATH"
    |> Environment.GetEnvironmentVariable
    |> _.Split(':')

/// Find an executable from the PATH environment variable
let findCommandPath command =
    path |> Array.tryPick (fun dir ->
        let path = Path.Combine(dir, command)

        match File.Exists path with
        | true -> Some path
        | false -> None
    )