namespace Starter.ApplicationSearchEngine

open Starter.SearchEngine
open FileIconLoader

open System
open System.IO
open System.Diagnostics
open System.Management
open System.Collections.Generic
open Microsoft.Win32

open FSharp.Control.Reactive
open FsToolkit.ErrorHandling

[<AutoOpen>]
module Helpers =
    module Option =
        let require f v =
            match v |> f with
            | true -> Some v
            | false -> None

type ApplicationSearchResult =
    { Name: string
      Path: string }

    interface ISearchResult with
        member this.Name = this.Name
        member this.LoadIcon() = FileIconLoader.LoadBitmap(this.Path)

type ApplicationSearchEngine() =
    let applications = SortedList<string, ApplicationSearchResult>()
    // let applications =
    //     [ Environment.SpecialFolder.StartMenu
    //       Environment.SpecialFolder.CommonStartMenu ]
    //     |> Seq.collect (Environment.GetFolderPath >> fun path ->
    //         Directory.EnumerateFiles(path, "*.lnk", SearchOption.AllDirectories)
    //     )
    //     |> Seq.map (fun path ->
    //         { Name = path |> Path.GetFileNameWithoutExtension
    //           Path = path }
    //     )
    //     |> Seq.sortBy _.Name
    //     |> Seq.toList

    do
    // member _.GetInstalledAppsFromWMI() =
        // let query = ObjectQuery("SELECT * FROM Win32_Product");
        //
        // use searcher = new ManagementObjectSearcher(query)
        // printfn "Indexing"
        // for obj in searcher.Get() do
        //     let name = obj["Name"]  :?> string | null
        //     let version = obj["Version"] :?> string | null
        //
        //     match isNull name || isNull version with
        //     | true ->
        //         printfn "Error: %A" obj
        //     | false ->
        //         if not <| applications.TryAdd(name, { Name = name; Path = version }) then
        //             printfn "Error2: %A" (obj, name, version)

        // RegistryHelper.GetSubKeyValues(RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", apps, true);
        // RegistryHelper.GetSubKeyValues(RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", apps, true);

        let getSubKeyValues hive subKey =
            use baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry32)
            use key = baseKey.OpenSubKey subKey
            if not <| isNull key then
                for subKeyName in key.GetSubKeyNames() do
                    option {
                        use subKeyItem = key.OpenSubKey subKeyName

                        let! displayName =
                            subKeyItem.GetValue("DisplayName")
                            :?> string
                            |> Option.require (String.IsNullOrEmpty >> not)
                        let! path =
                            subKeyItem.GetValue("InstallLocation")
                            :?> string
                            |> Option.require (String.IsNullOrEmpty >> not)

                        let iconPath =
                            subKeyItem.GetValue "DisplayIcon"
                            :?> string
                            |> Option.require (String.IsNullOrEmpty >> not)

                        applications.Add(displayName, { Name = displayName; Path = path })
                    } |> ignore

        getSubKeyValues RegistryHive.LocalMachine @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"
        getSubKeyValues RegistryHive.CurrentUser @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"

    interface ISearchEngine with
        member _.Name = nameof ApplicationSearchEngine

        member _.Search(_ct, query) =
            applications
            |> Seq.choose (_.Value >> fun app ->
                match app.Name.Contains(query, StringComparison.OrdinalIgnoreCase) with
                | true -> app :> ISearchResult |> Some
                | false -> None
            )
            |> Observable.single

        member _.SearchResultSelected(searchResult) =
            match searchResult with
            | :? ApplicationSearchResult as sr ->
                printfn "%A" sr
                // ProcessStartInfo(
                //     FileName = sr.Path,
                //     UseShellExecute = true
                // )
                // |> Process.Start
                // |> ignore
            | _ -> ()
