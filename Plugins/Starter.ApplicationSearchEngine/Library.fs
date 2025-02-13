namespace Starter.ApplicationSearchEngine

open System
open System.IO
open System.Xml
open System.Diagnostics
open System.Threading.Tasks
open System.Collections.Generic

open FSharp.Control.Reactive
open FsToolkit.ErrorHandling
open Vanara.PInvoke
open Vanara.Windows.Shell

open Starter.SearchEngine
open IconHelper
open Avalonia.Media.Imaging

[<AutoOpen>]
module Helpers =
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

type Package =
    { Id: string * string
      ManifestPath: string }
module Package =
    let ofShellItem (shellItem: ShellItem) =
        option {
            let! packageId = shellItem |> ShellItem.Property.get "System.AppUserModel.ID"
            let! installPath = shellItem |> ShellItem.Property.getInstallPath
            let manifestPath = Path.Combine(installPath, "AppxManifest.xml")

            // Check if file exists
            do! manifestPath
                |> File.Exists
                |> Option.require id
                |> Option.ignore

            return { Id = packageId.Split("!") |> fun arr -> arr[0], arr[1]
                     ManifestPath = manifestPath }
        }

    let getIconResourceName (package: Package) =
        let xml = XmlDocument()
        xml.Load package.ManifestPath

        let nsManager = XmlNamespaceManager xml.NameTable
        nsManager.AddNamespace("default", "http://schemas.microsoft.com/appx/manifest/foundation/windows10")
        nsManager.AddNamespace("uap", "http://schemas.microsoft.com/appx/manifest/uap/windows10")

        let selector = $"""/default:Package/default:Applications/default:Application[@Id="{package.Id |> snd}"]/uap:VisualElements/@Square44x44Logo"""

        xml.SelectSingleNode(selector, nsManager)
        |> Option.ofNull
        |> Option.map _.Value

    /// Retrieve the files corresponding to the resource
    let getResourceFiles (resource: string) package =
        let packageFolder = package.ManifestPath |> Path.GetDirectoryName
        let resourceFolder = resource |> Path.GetDirectoryName
        let absolutePath = Path.Combine(packageFolder, resourceFolder)

        let resourceName = Path.GetFileNameWithoutExtension resource
        let resourceExt = Path.GetExtension resource

        Directory.GetFiles(absolutePath, $"{resourceName}*{resourceExt}")
        |> Array.map (fun file ->
            let fileName = Path.GetFileNameWithoutExtension file

            if fileName = resourceName then // Is no qualifiers
                file, dict []
            else
                // Parse qualifiers (ex: fileName = logo.contrast-high_scale-400)
                let qualifiers =
                    fileName.Split(".")
                    |> Array.last
                    |> _.Split("_")
                    |> Array.map (fun qualifier ->
                        if qualifier.Contains "-" then
                            let arr = qualifier.Split "-"
                            arr[0], arr[1]
                        else
                            qualifier, ""
                    )
                    |> dict

                file, qualifiers
        )

    let getIcon package = option {
        let! resourceName = package |> getIconResourceName
        let resourceFiles = package |> getResourceFiles resourceName

        return!
            resourceFiles
            |> Array.sortBy (fun (_file, qualifiers) ->
                let contrastScore =
                    match qualifiers |> Dict.tryGet "contrast" with
                    | None
                    | Some "standard" -> 0
                    | _ -> 100

                let targetSizeScore =
                    match qualifiers |> Dict.tryGet "targetsize" with
                    | None -> 44 // Because Square44x44Logo
                    | Some size ->
                        match Int32.TryParse size with
                        | true, scale -> scale
                        | false, _ -> Int32.MaxValue
                    |> fun scale -> 100 - scale |> abs

                let scaleScore =
                    match qualifiers |> Dict.tryGet "scale" with
                    | None -> 100
                    | Some scale ->
                        match Int32.TryParse scale with
                        | true, scale -> scale
                        | false, _ -> Int32.MaxValue
                    |> fun scale -> 200 - scale |> abs // Distance to 200

                let qualifiersCountScore = qualifiers.Count

                contrastScore*100_000
                + targetSizeScore*10_000
                + scaleScore*100
                + qualifiersCountScore
            )
            |> Array.tryHead
            |> Option.map fst
    }

[<RequireQualifiedAccess>]
type ExecutionPath =
    | ExeFile of string
    | PackageId of string

type SearchResult =
    { Name: string
      ExecutionPath: ExecutionPath
      LoadIcon: unit -> Task<Bitmap> }

    interface ISearchResult with
        member this.Name = this.Name
        member this.LoadIcon() = this.LoadIcon()

/// Indexes apps and cache their icon
type AppIndexer() =
    let applications = SortedList<string, SearchResult>()
    let iconsCache = Dictionary<string, Bitmap>()
    let mutable indexingFinished = false

    let cachedIconLoader name iconLoader () =
        task {
            match iconsCache.TryGetValue name with
            | true, bitmap -> return bitmap
            | false, _ ->
              let bitmap = iconLoader()
              iconsCache.TryAdd(name, bitmap) |> ignore
              return bitmap
        }

    let getAppIcon (targetPathOpt: string option) (app: ShellItem) =
        // Icon from the Appx apps
        let packageIconOpt =
            app
            |> Package.ofShellItem
            |> Option.bind Package.getIcon

        let getShellIcon () =
            app.Images
               .GetImage(SIZE(96, 96))
               .ToAvaloniaBitmap()

        match packageIconOpt, targetPathOpt with
        | Some iconPath, _ -> new Bitmap(iconPath) // Found an icon associated with package
        | None, Some linkPath when linkPath.EndsWith ".exe" || linkPath.EndsWith ".lnk" -> // Take the .exe/.lnk icon
            IconHelper.getFileIcon linkPath
            |> Option.defaultWith getShellIcon // Let the shell load the icon
        | _ ->
            getShellIcon() // Let the shell load the icon

    let indexApps () = Task.Run<unit>(fun () -> task {
        new ShellFolder(Shell32.KNOWNFOLDERID.FOLDERID_AppsFolder)
        |> Seq.iter (fun app ->
            option {
                let! name = app.Name |> Option.require (String.IsNullOrEmpty >> not)
                let packageIdOpt = app |> ShellItem.Property.get "System.AppUserModel.ID"
                let targetPathOpt = app |> ShellItem.Property.get "System.Link.TargetParsingPath"

                let icon = app |> getAppIcon targetPathOpt

                let! executionPath =
                    match packageIdOpt, targetPathOpt with
                    | Some pkgId, _ -> ExecutionPath.PackageId pkgId |> Some
                    | _, Some linkPath -> ExecutionPath.ExeFile linkPath |> Some
                    | None, None -> None

                app.Dispose()

                iconsCache.Add(name, icon)
                applications.TryAdd(
                    name,
                    { Name = name
                      ExecutionPath = executionPath
                      LoadIcon = fun () -> task {
                          match iconsCache.TryGetValue name with
                          | true, icon -> return icon
                          | false, _ -> return null
                      } }
                ) |> ignore
            } |> ignore
        )

        indexingFinished <- true
    })

    do indexApps() |> ignore

    member _.Applications =
        match indexingFinished with
        | true -> Some applications
        | false -> None

type ApplicationSearchEngine() =
    let indexer = AppIndexer()

    interface ISearchEngine with
        member _.Id = nameof ApplicationSearchEngine
        member _.DisplayName = "Application"

        member _.Search(_ct, query) =
            match indexer.Applications with
            | None -> Observable.empty
            | Some apps ->
                apps
                |> Seq.choose (_.Value >> fun app ->
                    match app.Name.Contains(query, StringComparison.OrdinalIgnoreCase) with
                    | true -> app :> ISearchResult |> Some
                    | false -> None
                )
                |> Observable.single

        member _.SearchResultSelected(searchResult) =
            match searchResult with
            | :? SearchResult as sr ->
                let execStr =
                    match sr.ExecutionPath with
                    | ExecutionPath.ExeFile path -> path
                    | ExecutionPath.PackageId pkgId -> $"shell:AppsFolder\\{pkgId}"

                ProcessStartInfo(
                    FileName = execStr,
                    UseShellExecute = true
                )
                |> Process.Start
                |> ignore
            | _ -> ()
