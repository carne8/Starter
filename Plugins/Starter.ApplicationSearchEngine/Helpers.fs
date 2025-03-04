[<AutoOpen>]
module Starter.ApplicationSearchEngine.Helpers

open System
open System.IO
open System.Collections.Generic
open Vanara.PInvoke
open Vanara.Windows.Shell

module Option =
    let require f v =
        match v |> f with
        | true -> Some v
        | false -> None

module Dict =
    let tryGet key (d: IDictionary<_, _>) =
        match d.TryGetValue key with
        | true, v -> Some v
        | false, _ -> None

module ShellItem =
    module Property =
        let get propertyName (shellItem: ShellItem) =
            propertyName
            |> Ole32.PROPERTYKEY
            |> shellItem.Properties.GetPropertyString
            |> Option.require (String.IsNullOrEmpty >> not)

        let getInstallPath (shellItem: ShellItem) =
            shellItem
            |> get "System.AppUserModel.PackageInstallPath"
            |> Option.orElseWith (fun _ ->
                shellItem
                |> get "System.Link.TargetParsingPath"
                |> Option.map Path.GetDirectoryName
            )
