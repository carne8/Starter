module Starter.ApplicationSearchEngine.Loaders.Linux.XDGDesktop

open System
open System.Collections.Concurrent
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Threading.Tasks
open FsToolkit.ErrorHandling
open R3
open Starter.ApplicationSearchEngine
open Starter.SearchEngine

type DesktopApplication =
    { Id: string
      Name: string
      Exec: string
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = "Application"
        member this.Icon = this.Icon

module FolderConfiguration =
    let Default =
        { Folders =
            [| Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".local/share/applications"
               )
               "/usr/share/applications/"
               "/usr/local/share/applications/" |]
          ExcludedFolders = Array.empty  }

type String with
    member inline this.TryIndexOf(s: string) =
        match this.IndexOf(s) with
        | -1 -> ValueNone
        | n -> ValueSome n

    member inline this.TryIndexOf(c: char) =
        match this.IndexOf(c) with
        | -1 -> ValueNone
        | n -> ValueSome n

let runApp (app: DesktopApplication) =
    let struct (fileName, args) =
        match app.Exec.IndexOf ' ' with
        | -1 -> struct (app.Exec, String.Empty)
        | i -> struct (app.Exec[..i-1], app.Exec[i..])

    ProcessStartInfo(
        FileName = fileName,
        Arguments = args,
        UseShellExecute = true
    )
    |> Process.Start
    |> ignore

let loadAppIcon iconName =
    "/usr/share/icons/hicolor"
    |> Directory.EnumerateDirectories
    |> Seq.choose (fun dir ->
        let dirName = Path.GetFileName dir

        match dirName.TryIndexOf 'x' with
        | ValueNone -> None
        | ValueSome xIndex ->
            dirName[0..xIndex-1]
            |> Int32.TryParse
            |> function
                | true, v -> Some struct (dir, v)
                | false, _ -> None
    )
    |> Seq.sortByDescending (fun struct (_, size) -> size)
    |> Seq.tryPick (fun struct (dir, _) ->
        let iconFile = Path.Combine(dir, "apps", $"{iconName}.png")
        if iconFile |> File.Exists then Some iconFile
        else
            Path.Combine(dir, "apps")
            |> Directory.GetFiles
            |> Array.tryFind (Path.GetFileName >> (=) iconName)
    )
    |> Option.map (fun iconPath ->
        let bmp = new Avalonia.Media.Imaging.Bitmap(iconPath)
        StarterIconSource(bmp, bmp)
    )

let getAppFromFile filePath =
    taskOption {
        let! lines = filePath |> File.ReadAllLinesAsync
        let! desktopEntryStart = lines |> Array.tryFindIndex _.StartsWith("[Desktop Entry]")
        let desktopEntryEnd =
            lines[desktopEntryStart+1..]
            |> Array.tryFindIndex _.StartsWith("[")
            |> function
                | None -> lines.Length-1
                | Some i -> i
        let desktopEntry = lines[desktopEntryStart+1..desktopEntryEnd]

        let mutable appName = ValueNone
        let mutable appExec = ValueNone
        let mutable appIcon = ValueNone

        desktopEntry
        |> Array.choose (fun line ->
            option {
                let mutable key = ValueNone
                let variant =
                    line.TryIndexOf '[' |> ValueOption.bind (fun start ->
                        key <- ValueSome line[..start-1]
                        line.TryIndexOf "]"
                        |> ValueOption.map (fun end' -> line[start+1..end'-1])
                    )

                if variant.IsSome then do! None // TODO: I18n

                let! equalPos = line.TryIndexOf "="

                match key with
                | ValueNone -> key <- ValueSome line[..equalPos-1]
                | _ -> ()

                let! key = key

                return
                    {| Key = key
                       Variant = variant
                       Value = line[equalPos+1..] |}
            }
        )
        |> Array.iter (fun line ->
            if appName.IsNone && line.Key = "Name" then appName <- ValueSome line.Value
            if appExec.IsNone && line.Key = "Exec" then appExec <- ValueSome line.Value
            if appIcon.IsNone && line.Key = "Icon" then appIcon <- ValueSome line.Value
        )


        // if filePath.Contains "zen-browser" then
        //     printfn "%A" (desktopEntryStart, desktopEntryEnd)
        //     printfn "%A" desktopEntry
        //     printfn "Name: %A" appName
        //     printfn "Exec: %A" appExec
        //     printfn "Icon: %A" appIcon
        // if appName.IsValueNone then return! None
        // let! appExec = appExec
        // let! appIcon = appIcon

        match appName, appExec, appIcon with
        | ValueSome name, ValueSome exec, ValueSome appIcon ->
            if name.Contains "Google Maps" || name.Contains "Touchpad" then
                printfn "%A" lines

            return { Id = filePath
                     Name = name
                     Exec = exec
                     Icon = appIcon |> loadAppIcon |> Option.defaultValue null }
        | _ -> return! None
    }


let loadApplications (config: FolderConfiguration) : Task<ISearchResult seq> =
    Task.Run<ISearchResult seq>(fun () ->
        task {
            let apps = ConcurrentDictionary<_, _>()

            for folder in config.Folders do
                let files = Directory.EnumerateFiles(folder, "*.desktop")

                do! Parallel.ForEachAsync(
                    files,
                    Func<_, _, _>(fun path _ ->
                        match path |> FolderConfiguration.isFileExcluded config with
                        | true -> ValueTask.CompletedTask
                        | false ->
                            path
                            |> getAppFromFile
                            |> TaskOption.map (fun app ->
                                apps.TryAdd(path |> Path.GetFileName, app :> ISearchResult) |> ignore
                            )
                            |> ValueTask
                    )
                )

            return apps.Values :> ISearchResult seq
        }
    )

// let observeApplicationChanges (appList: List<ISearchResult>) (config: FolderConfiguration) =
//     let subject = new Subject<unit>()
//     let watchers = config.Folders |> Array.map (fun folder ->
//         let watcher = new FileSystemWatcher(folder)
//         watcher.Filters.Add("*.exe")
//         watcher.Filters.Add("*.lnk")
//         watcher.NotifyFilter <-
//             NotifyFilters.CreationTime
//             ||| NotifyFilters.DirectoryName
//             ||| NotifyFilters.FileName
//             ||| NotifyFilters.LastWrite
//
//         watcher.Created.Add(fun args ->
//             args.FullPath
//             |> getAppFromFile
//             |> Option.iter appList.Add
//             subject.OnNext()
//         )
//         watcher.Deleted.Add(fun args ->
//             appList.FindIndex(_.Id >> (=) args.FullPath) |> appList.RemoveAt
//             subject.OnNext()
//         )
//         watcher.Renamed.Add(fun args ->
//             appList.FindIndex(_.Id >> (=) args.OldFullPath) |> appList.RemoveAt
//
//             args.FullPath
//             |> getAppFromFile
//             |> Option.iter appList.Add
//             subject.OnNext()
//         )
//
//         watcher.IncludeSubdirectories <- true
//         watcher.EnableRaisingEvents <- true
//         watcher
//     )
//
//     subject,
//     { new IDisposable with
//         member _.Dispose() =
//             watchers |> Array.iter _.Dispose()
//             subject.Dispose() }
