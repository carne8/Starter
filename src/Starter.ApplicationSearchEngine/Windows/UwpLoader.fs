module Starter.ApplicationSearchEngine.Windows.UwpLoader

open R3
open Starter.SearchEngine
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Logger

open System
open System.Collections.Generic
open System.Diagnostics
open System.IO
open System.Security.Principal
open System.Text
open System.Xml.Linq

open FsToolkit.ErrorHandling
open Avalonia.Media.Imaging

open Windows.ApplicationModel
open Windows.Management.Deployment
open Vanara

type UwpApplication =
    { Id: string
      Name: string
      PackageId: string
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = "Application"
        member this.Keywords = Array.empty
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty

module Xml =
    let getNamespaces (xml: XDocument) =
        match xml.Root with
        | null -> Array.empty
        | root ->
            root.Attributes()
            |> Seq.filter _.IsNamespaceDeclaration
            |> Seq.groupBy (fun attribute ->
                match attribute.Name.Namespace = XNamespace.None with
                | true -> String.Empty
                | false -> attribute.Name.LocalName
            )
            |> Seq.map (fun (_, attributes) ->
                attributes
                |> Seq.head
                |> fun attr -> XNamespace.Get(attr.Value).ToString())
            |> Seq.toArray

type PackageVersion =
    | Windows10
    | Windows81
    | Windows8
    | Unknown

    static member fromNamespaces (namespaces: string[]) =
        [ "http://schemas.microsoft.com/appx/manifest/foundation/windows10", PackageVersion.Windows10
          "http://schemas.microsoft.com/appx/2013/manifest", PackageVersion.Windows81
          "http://schemas.microsoft.com/appx/2010/manifest", PackageVersion.Windows8 ]
        |> List.tryPick (fun (n, version) ->
            match namespaces |> Array.contains n with
            | true -> Some version
            | false -> None
        )

module PackageResource =
    /// Retrieve the files corresponding to the resource
    let getResourceFiles (installedPath: string) (resource: string) =
        voption {
            let! resourceFolder = resource |> Path.GetDirectoryName
            let absolutePath = Path.Combine(installedPath, resourceFolder)

            let! resourceName = Path.GetFileNameWithoutExtension resource
            let! resourceExt = Path.GetExtension resource

            return
                if not <| Directory.Exists absolutePath then Array.empty
                else
                Directory.GetFiles(absolutePath, $"{resourceName}*{resourceExt}")
                |> Array.choose (fun file ->
                    voption {
                        // Check file access
                        try let stream = file |> File.OpenRead
                            stream.Dispose()
                        with _ -> return! ValueNone

                        let! fileName = file |> Path.GetFileNameWithoutExtension
                        let fileName = fileName.ToLowerInvariant()

                        if fileName = resourceName then // No qualifiers
                            return file, dict []
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

                            return file, qualifiers
                    }
                    |> Option.ofValueOption
                )
        }

    /// Return a low malus for a resource that matches the criteria of a good app icon for Starter
    let getResourceMalus (_file, qualifiers: IDictionary<string, string>) =
        let contrastMalus =
            match qualifiers.TryGetValue("contrast") with
            | false, _
            | true, "standard" -> 0
            | _ -> 100

        let targetSizeMalus =
            match qualifiers.TryGetValue("targetsize") with
            | false, _ -> 44 // Because Square44x44Logo
            | true, size ->
                match Int32.TryParse size with
                | true, scale -> scale
                | false, _ -> Int32.MaxValue
            |> fun scale -> 100 - scale |> abs

        let scaleMalus =
            match qualifiers.TryGetValue("scale") with
            | false, _ -> 100
            | true, scale ->
                match Int32.TryParse scale with
                | true, scale -> scale
                | false, _ -> Int32.MaxValue
            |> fun scale -> 200 - scale |> abs // Distance to 200

        let qualifiersCountMalus = qualifiers.Count

        contrastMalus*100_000
        + targetSizeMalus*10_000
        + scaleMalus*100
        + qualifiersCountMalus

    let private tryLoadIndirectString (source: string) (stringBuilder: StringBuilder) =
        let res = PInvoke.ShlwApi.SHLoadIndirectString(source, stringBuilder, uint stringBuilder.Capacity)
        if res.Failed then
            None
        else
            let str = stringBuilder.ToString()
            let len = str.IndexOf('\x00')
            let loaded =
                match len >= 0 with
                | true -> str[..len]
                | false -> str

            match loaded |> String.IsNullOrEmpty with
            | true -> None
            | false -> Some loaded

    let loadResourceFromPri packageFullName (resource: string) =
        let prefix = "ms-resource:"
        if String.IsNullOrWhiteSpace(resource) || not <| resource.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
            Some resource
        else
        let key = resource.Substring(prefix.Length)
        let parsed, fallback =
            if key.StartsWith("//", StringComparison.Ordinal) then
                prefix + key, None
            elif key.StartsWith('/') then
                prefix + "//" + key, None
            elif key.Contains("resources", StringComparison.OrdinalIgnoreCase) then
                prefix + key, None
            else
                prefix + "///resources/" + key, Some (prefix + "///" + key)

        let source = $"@{{{packageFullName}? {parsed}}}"
        let strBuilder = StringBuilder(1024)

        match tryLoadIndirectString source strBuilder, fallback with
        | None, Some fallback ->
            let sourceFallback = $"@{{{packageFullName}?{fallback}}}"
            tryLoadIndirectString sourceFallback strBuilder
        | None, None -> None
        | Some resourceValue, _ -> Some resourceValue

type AppxApplication(packageVersion: PackageVersion, installLocation, packageId: PackageId, xml: XElement) =
    let uapNamespace = XNamespace.Get("http://schemas.microsoft.com/appx/manifest/uap/windows10")

    let iconKey =
        match packageVersion with
        | Windows10 -> ValueSome "Square44x44Logo"
        | Windows81 -> ValueSome "Square30x30Logo"
        | Windows8 -> ValueSome "SmallLogo"
        | Unknown -> ValueNone

    // App assets documentation: https://learn.microsoft.com/windows/uwp/controls-and-patterns/tiles-and-notifications-app-assets
    // Windows 10 https://msdn.microsoft.com/library/windows/apps/dn934817.aspx
    // Windows 8.1 https://msdn.microsoft.com/library/windows/apps/hh965372.aspx#target_size
    // Windows 8 https://msdn.microsoft.com/library/windows/apps/br211475.aspx
    member private this.GetIconName() =
        voption {
            let! iconKey = iconKey
            return!
                xml.Elements(uapNamespace + "VisualElements")
                |> Seq.tryPick (fun visualElement ->
                    visualElement.Attribute iconKey
                    |> Option.ofObj
                    |> Option.map _.Value
                )
        }

    member private this.GetIconPath(iconName) =
        voption {
            let! resourceFiles = iconName |> PackageResource.getResourceFiles installLocation

            let lightResources = ResizeArray()
            let darkResources = ResizeArray()
            resourceFiles |> Array.iter (fun (file, qualifiers) ->
                file
                |> Path.GetFileNameWithoutExtension
                |> ValueOption.ofObj
                |> ValueOption.iter (fun fileName ->
                    let fileName = fileName.ToLowerInvariant()

                    if fileName.Contains "theme-light" || fileName.Contains "altform-lightunplated" then
                        lightResources.Add (file, qualifiers)
                    else
                        darkResources.Add (file, qualifiers)
                )
            )

            let lightIcon =
                if lightResources.Count = 0 then ValueNone else
                    lightResources
                    |> Seq.minBy PackageResource.getResourceMalus
                    |> fst
                    |> ValueSome

            let darkIcon =
                if darkResources.Count = 0 then ValueNone else
                    darkResources
                    |> Seq.minBy PackageResource.getResourceMalus
                    |> fst
                    |> ValueSome

            match lightIcon, darkIcon with
            | ValueSome l, ValueSome d -> return l, d
            | ValueSome f, ValueNone | ValueNone, ValueSome f -> return f, f
            | ValueNone, ValueNone -> return! ValueNone
        }

    member private this.GetId() =
        xml.Attribute "Id"
        |> ValueOption.ofObj
        |> ValueOption.map _.Value

    member private this.GetAppListEntry() =
        xml.Elements(uapNamespace + "VisualElements")
        |> Seq.tryPick (fun visualElement ->
            visualElement.Attribute "AppListEntry"
            |> Option.ofObj
            |> Option.map _.Value
        )
        |> ValueOption.ofOption

    member private this.GetDisplayNameResourceId() =
        xml.Elements(uapNamespace + "VisualElements")
        |> Seq.tryPick (fun visualElement ->
            visualElement.Attribute "DisplayName"
            |> Option.ofObj
            |> Option.map _.Value
        )
        |> ValueOption.ofOption

    member this.ToSearchResult() : ISearchResult voption =
        try
            voption {
                let! appId = this.GetId()
                let! nameResourceId = this.GetDisplayNameResourceId()
                let! name = PackageResource.loadResourceFromPri packageId.FullName nameResourceId

                let! iconName = this.GetIconName()
                let! lightIconPath, darkIconPath = this.GetIconPath(iconName)

                let lightIconStream = lightIconPath |> File.OpenRead
                let darkIconStream = darkIconPath |> File.OpenRead
                let icon = StarterIconSource(
                    Bitmap.DecodeToHeight(lightIconStream, Constants.IconSize),
                    Bitmap.DecodeToHeight(darkIconStream, Constants.IconSize)
                )

                do! match this.GetAppListEntry() with
                    | ValueSome "none" -> ValueNone
                    | _ -> ValueSome ()

                return
                    { Id = packageId.FullName + appId
                      Name = name
                      PackageId = $"{packageId.FamilyName}!{appId}"
                      Icon = icon } :> ISearchResult
            }
        with exn ->
            logger.Warning(exn, "Failed to convert package to search result")
            ValueNone

type AppxManifest(package: Package) =
    let manifestPath = Path.Combine(package.InstalledLocation.Path, "AppxManifest.xml")
    let xml = XDocument.Load manifestPath

    let packageVersion =
        xml
        |> Xml.getNamespaces
        |> PackageVersion.fromNamespaces
        |> Option.get

    member this.GetApplications() =
        voption {
            let! root = xml.Root
            let ns = root.GetDefaultNamespace()

            return xml.Descendants(ns + "Application") |> Seq.choose (fun xml -> // TODO: Seq.tryPick and Seq.choose to voption
                AppxApplication(
                    packageVersion,
                    package.InstalledPath,
                    package.Id,
                    xml
                ).ToSearchResult()
                |> Option.ofValueOption
            )
        }
        |> ValueOption.defaultValue Seq.empty

let runApp (app: UwpApplication) =
    ProcessStartInfo(
        FileName = $"shell:AppsFolder\\{app.PackageId}",
        UseShellExecute = true
    )
    |> Process.Start
    |> function null -> () | d -> d.Dispose()

let loadApplications (logger: Serilog.ILogger) : ISearchResult seq =
    try
        let packageManager = PackageManager()

        WindowsIdentity.GetCurrent().Owner
        |> ValueOption.ofObj
        |> ValueOption.map (fun currentUser ->
            currentUser.Value
            |> packageManager.FindPackagesForUser
            |> Seq.collect (fun package -> AppxManifest(package).GetApplications())
        )
        |> ValueOption.defaultValue Seq.empty

    with e ->
        logger.Error(e, "Failed to load applications from Windows packages.")
        Seq.empty

let observeApplicationChanges (appList: List<ISearchResult>) =
    let catalog = PackageCatalog.OpenForCurrentUser()
    let subject = new Subject<unit>()

    let add =
        Windows.Foundation.TypedEventHandler<_, PackageInstallingEventArgs>(fun _ evt ->
            if evt.IsComplete then
                let oldCount = appList.Count
                AppxManifest(evt.Package).GetApplications() |> appList.AddRange
                if oldCount <> appList.Count then subject.OnNext()
        )

    let remove =
        Windows.Foundation.TypedEventHandler<_, PackageUninstallingEventArgs>(fun _ evt ->
            if evt.IsComplete then
                appList.RemoveAll (fun app ->
                    match app.Id with
                    | null -> false
                    | id -> id.StartsWith(evt.Package.Id.FullName)
                ) |> ignore
                subject.OnNext()
        )

    let update =
        Windows.Foundation.TypedEventHandler<_, PackageUpdatingEventArgs>(fun _ evt ->
            if evt.IsComplete then
                appList.RemoveAll (fun app ->
                    match app.Id with
                    | null -> false
                    | id -> id.StartsWith(evt.SourcePackage.Id.FullName)
                ) |> ignore
                AppxManifest(evt.TargetPackage).GetApplications() |> appList.AddRange
                subject.OnNext()
        )

    catalog.add_PackageInstalling(add)
    catalog.add_PackageUninstalling(remove)
    catalog.add_PackageUpdating(update)

    subject,
    { new IDisposable with
        member this.Dispose() =
            catalog.remove_PackageInstalling(add)
            catalog.remove_PackageUninstalling(remove)
            catalog.remove_PackageUpdating(update)
            subject.Dispose() }
