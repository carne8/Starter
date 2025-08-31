module Starter.ApplicationSearchEngine.Loaders.Windows.Uwp

open System.Diagnostics
open R3
open Starter.ApplicationSearchEngine
open Starter.SearchEngine

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.IO
open System.Security.Principal
open System.Text
open System.Threading.Tasks
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
        member this.Icon = this.Icon

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
        let resourceFolder = resource |> Path.GetDirectoryName
        let absolutePath = Path.Combine(installedPath, resourceFolder)

        let resourceName = Path.GetFileNameWithoutExtension resource
        let resourceExt = Path.GetExtension resource

        if not <| Directory.Exists absolutePath then
            Array.empty
        else
        Directory.GetFiles(absolutePath, $"{resourceName}*{resourceExt}")
        |> Array.map (fun file ->
            let fileName =
                file
                |> Path.GetFileNameWithoutExtension
                |> _.ToLowerInvariant()

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

    /// Return a low score for a resource that matches the criteria of a good app icon for Starter
    let getResourceScore (_file, qualifiers: IDictionary<string, string>) =
        let contrastScore =
            match qualifiers.TryGetValue("contrast") with
            | false, _
            | true, "standard" -> 0
            | _ -> 100

        let targetSizeScore =
            match qualifiers.TryGetValue("targetsize") with
            | false, _ -> 44 // Because Square44x44Logo
            | true, size ->
                match Int32.TryParse size with
                | true, scale -> scale
                | false, _ -> Int32.MaxValue
            |> fun scale -> 100 - scale |> abs

        let scaleScore =
            match qualifiers.TryGetValue("scale") with
            | false, _ -> 100
            | true, scale ->
                match Int32.TryParse scale with
                | true, scale -> scale
                | false, _ -> Int32.MaxValue
            |> fun scale -> 200 - scale |> abs // Distance to 200

        let qualifiersCountScore = qualifiers.Count

        contrastScore*100_000
        + targetSizeScore*10_000
        + scaleScore*100
        + qualifiersCountScore

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

type AppxApplication(packageVersion: PackageVersion, installedLocation, packageId: PackageId, xml: XElement) =
    let uapNamespace = XNamespace.Get("http://schemas.microsoft.com/appx/manifest/uap/windows10")

    let iconKey =
        match packageVersion with
        | Windows10 -> Some "Square44x44Logo"
        | Windows81 -> Some "Square30x30Logo"
        | Windows8 -> Some "SmallLogo"
        | Unknown -> None

    // App assets documentation: https://learn.microsoft.com/windows/uwp/controls-and-patterns/tiles-and-notifications-app-assets
    // Windows 10 https://msdn.microsoft.com/library/windows/apps/dn934817.aspx
    // Windows 8.1 https://msdn.microsoft.com/library/windows/apps/hh965372.aspx#target_size
    // Windows 8 https://msdn.microsoft.com/library/windows/apps/br211475.aspx
    member private this.GetIconName() =
        iconKey |> Option.bind (fun key ->
            try
                xml.Element(uapNamespace + "VisualElements")
                |> _.Attribute(key)
                |> _.Value
                |> Some
            with _ -> None
        )

    member private this.GetIconPath() =
        option {
            let! resourceName = this.GetIconName()
            let resourceFiles = resourceName |> PackageResource.getResourceFiles installedLocation

            let lightResources, darkResources =
                resourceFiles |> Array.partition (fun (file, _) ->
                    let fileName =
                        file
                        |> Path.GetFileNameWithoutExtension
                        |> _.ToLowerInvariant()

                    fileName.Contains "theme-light" || fileName.Contains "altform-lightunplated"
                )

            let lightIcon =
                lightResources
                |> Array.sortBy PackageResource.getResourceScore
                |> Array.tryHead
                |> Option.map fst

            let darkIcon =
                darkResources
                |> Array.sortBy PackageResource.getResourceScore
                |> Array.tryHead
                |> Option.map fst

            match lightIcon, darkIcon with
            | Some f, None
            | None, Some f -> return f, f
            | Some l, Some d -> return l, d
            | None, None -> return! None
        }

    member private this.GetId() =
        try Some <| xml.Attribute("Id").Value
        with _ -> None

    member private this.GetAppListEntry() =
        try
            xml.Element(uapNamespace + "VisualElements")
            |> _.Attribute("AppListEntry")
            |> _.Value
            |> Some
        with _ -> None

    member private this.GetDisplayNameResourceId() =
        try
            xml.Element(uapNamespace + "VisualElements")
            |> _.Attribute("DisplayName")
            |> _.Value
            |> Some
        with _ -> None

    member this.ToSearchResult() : ISearchResult option =
        option {
            let! appId = this.GetId()

            let! nameResourceId = this.GetDisplayNameResourceId()
            let! name = PackageResource.loadResourceFromPri packageId.FullName nameResourceId

            let! lightIconPath, darkIconPath = this.GetIconPath()
            let lightIconStream = lightIconPath |> File.OpenRead
            let darkIconStream = darkIconPath |> File.OpenRead
            let icon = StarterIconSource(
                Bitmap.DecodeToHeight(lightIconStream, Constants.IconSize),
                Bitmap.DecodeToHeight(darkIconStream, Constants.IconSize)
            )

            do! match this.GetAppListEntry() with
                | Some "none" -> None
                | Some _ | None -> Some ()

            return
                { Id = packageId.FullName + appId
                  Name = name
                  PackageId = $"{packageId.FamilyName}!{appId}"
                  Icon = icon } :> ISearchResult
        }

type AppxManifest(package: Package) =
    let manifestPath = Path.Combine(package.InstalledLocation.Path, "AppxManifest.xml")
    let xml = XDocument.Load manifestPath

    let packageVersion =
        xml
        |> Xml.getNamespaces
        |> PackageVersion.fromNamespaces
        |> Option.get

    member this.GetApplications() =
        let ns = xml.Root.GetDefaultNamespace()
        xml.Descendants(ns + "Application") |> Seq.choose (fun xml ->
            AppxApplication(
                packageVersion,
                package.InstalledPath,
                package.Id,
                xml
            ).ToSearchResult()
        )

let runApp (app: UwpApplication) =
    ProcessStartInfo(
        FileName = $"shell:AppsFolder\\{app.PackageId}",
        UseShellExecute = true
    )
    |> Process.Start
    |> ignore

let loadApplications (logger: Serilog.ILogger) : Task<ISearchResult seq> =
    let packageManager = PackageManager()
    let currentUser = WindowsIdentity.GetCurrent().Owner

    try
        task {
            let bag = ConcurrentBag()

            do! Parallel.ForEachAsync(
                packageManager.FindPackagesForUser(currentUser.Value),
                Func<_, _, _>(fun package _ ->
                    AppxManifest(package).GetApplications() |> Seq.iter bag.Add
                    ValueTask.CompletedTask
                )
            )

            return bag :> ISearchResult seq
        }
    with e ->
        logger.Error(e, "Failed to load applications from Windows packages.")
        Seq.empty |> Task.FromResult

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
                appList.RemoveAll(_.Id >> _.StartsWith(evt.Package.Id.FullName)) |> ignore
                subject.OnNext()
        )

    let update =
        Windows.Foundation.TypedEventHandler<_, PackageUpdatingEventArgs>(fun _ evt ->
            if evt.IsComplete then
                appList.RemoveAll(_.Id >> _.StartsWith(evt.SourcePackage.Id.FullName)) |> ignore
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
