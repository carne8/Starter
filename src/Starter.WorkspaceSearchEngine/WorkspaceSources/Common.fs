module Starter.WorkspaceSearchEngine.WorkspaceSources.Common

open System
open System.IO

let private path =
    let path = Environment.GetEnvironmentVariable "PATH"
    if OperatingSystem.IsWindows() then
        path.Split(';')
    else
        path.Split(':')

/// Find an executable from the PATH environment variable
let findCommandPath command =
    path |> Array.tryPick (fun dir ->
        let path = Path.Combine(dir, command)

        match File.Exists path with
        | true -> Some path
        | false -> None
    )

type private String with
    member inline this.TryIndexOf(c: char) =
        match this.IndexOf c with
        | -1 -> ValueNone
        | n -> ValueSome n

    member inline this.TryIndexOf(c: char, startIndex: int) =
        match this.IndexOf(c, startIndex) with
        | -1 -> ValueNone
        | n -> ValueSome n
