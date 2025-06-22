namespace Starter.ApplicationSearchEngine

open System
open System.IO
open System.Xml
open FsToolkit.ErrorHandling
open Vanara.Windows.Shell

type WindowsPackage =
    { Id: string * string
      ManifestPath: string }

module WindowsPackage =
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

    let getIconResourceName (package: WindowsPackage) =
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
            |> Option.map (fst >> fun x -> x, x) // TODO: Load dark icon also
    }
