module Starter.ApplicationSearchEngine.Windows.ExeLoader

open Avalonia.Threading
open Starter.SearchEngine
open Starter.ApplicationSearchEngine
open Starter.ApplicationSearchEngine.Windows.Exe.IconHelper
open Starter.ApplicationSearchEngine.Logger

open System
open System.IO
open System.Threading
open System.Threading.Tasks
open System.Diagnostics
open System.Collections.Generic
open System.Collections.Concurrent

open FsToolkit.ErrorHandling
open Vanara.InteropServices
open Vanara.PInvoke
open Vanara.Windows.Shell

open System.Runtime.InteropServices

let invokeContextMenuItem (shellItemPath: string) (verb: string) =
    voption {
        use item = new ShellItem(shellItemPath)
        let! parent = item.Parent
        use folder = new ShellFolder(parent)

        let contextMenu = folder.GetChildrenUIObjects<Shell32.IContextMenu>(HWND.NULL, item)
        try
            use hMenu = User32.CreatePopupMenu()
            let res = contextMenu.QueryContextMenu(hMenu, 0u, 1u, 0x7FFFu, Shell32.CMF.CMF_EXTENDEDVERBS)
            if res.Failed then return! ValueNone else

            let mutable info = Shell32.CMINVOKECOMMANDINFO(verb)
            info.hwnd <- HWND.NULL
            info.nShow <- ShowWindowCommand.SW_SHOWNORMAL
            contextMenu.InvokeCommand(&info) |> ignore
        finally
            if not (isNull (box contextMenu)) then
                Marshal.ReleaseComObject(contextMenu) |> ignore
    } |> ignore

let rec loadSubContextMenu (path: string) i =
    voption {
        use item = new ShellItem(path)

        let! parent = item.Parent
        use folder = new ShellFolder(parent)

        let contextMenu = folder.GetChildrenUIObjects<Shell32.IContextMenu>(HWND.NULL, item)
        try
            use hMenu = User32.CreatePopupMenu()
            let res = contextMenu.QueryContextMenu(hMenu, 0u, 1u, 0x7FFFu, Shell32.CMF.CMF_EXTENDEDVERBS)
            if res.Failed then return! ValueNone else

            use subHMenu = new User32.SafeHMENU(hMenu.GetSub(i))

            let count = subHMenu.GetItemCount()
            let mutable previousWasSeparator = false
            let results =
                [| for i in 0 .. count - 1 do
                    let mutable mii = User32.MENUITEMINFO()
                    mii.cbSize <- uint32 (Marshal.SizeOf(typeof<User32.MENUITEMINFO>))
                    mii.fMask <-
                        User32.MenuItemInfoMask.MIIM_ID
                        ||| User32.MenuItemInfoMask.MIIM_STRING
                        ||| User32.MenuItemInfoMask.MIIM_FTYPE
                        ||| User32.MenuItemInfoMask.MIIM_BITMAP
                        ||| User32.MenuItemInfoMask.MIIM_SUBMENU

                    // First call to get required string buffer size
                    mii.dwTypeData <- StrPtrAuto()
                    let succeeded = User32.GetMenuItemInfo(subHMenu, uint32 i, true, &mii)
                    if not succeeded then () else

                    let cch = mii.cch + 1u
                    mii.dwTypeData <- StrPtrAuto(uint cch * 2u) // wide chars
                    mii.cch <- cch
                    let succeeded = User32.GetMenuItemInfo(subHMenu, uint32 i, true, &mii)
                    if not succeeded then () else

                    let isSeparator = mii.fType.HasFlag(User32.MenuItemType.MFT_SEPARATOR)
                    let text =
                        if isSeparator || mii.dwTypeData.IsNull then ""
                        else mii.dwTypeData.ToString()
                    mii.dwTypeData.Free()

                    if isSeparator then
                        if not previousWasSeparator then
                            previousWasSeparator <- true
                            { new IContextMenuResult with
                                member this.Id = null
                                member this.Name = String.Empty
                                member this.Description = null
                                member this.Keywords = null
                                member this.Icon = StarterIconSource.Empty
                                member this.IsSeparator = true
                                member this.Invoke() = ()
                                member this.GetContextMenu() = null }
                    elif mii.wID <> 0u then
                        previousWasSeparator <- false
                        let cmdId = mii.wID - 1u // GetUIObjectOf offsets ids by idCmdFirst (1)

                        // Description (help text) via GetCommandString
                        let description =
                            try
                                let cchMax = 512u
                                let buffer = Marshal.AllocHGlobal(int cchMax * 2) // wide chars, 2 bytes each
                                try
                                    let hr =
                                        contextMenu.GetCommandString(
                                            UIntPtr(cmdId),
                                            Shell32.GCS.GCS_HELPTEXTW,
                                            IntPtr.Zero,
                                            buffer,
                                            cchMax
                                        )

                                    if hr.Succeeded then
                                        Marshal.PtrToStringUni(buffer) |> Option.ofObj |> Option.defaultValue ""
                                    else
                                        ""
                                finally
                                    Marshal.FreeHGlobal(buffer)
                            with _ -> ""

                        // Icon
                        let icon =
                            if mii.hbmpItem.IsInvalid then StarterIconSource.Empty else
                                try
                                    let i = mii.hbmpItem.ToAvaloniaBitmap()
                                    StarterIconSource(i, i)
                                with _ ->
                                    StarterIconSource.Empty

                        let verb =
                            let cchMax = 256u
                            let buffer = Marshal.AllocHGlobal(int cchMax * 2)
                            try
                                let hr = contextMenu.GetCommandString(
                                    UIntPtr(cmdId),
                                    Shell32.GCS.GCS_VERBW,
                                    IntPtr.Zero,
                                    buffer,
                                    cchMax
                                )
                                if hr.Succeeded then
                                    buffer
                                    |> Marshal.PtrToStringUni
                                    |> ValueOption.ofObj
                                else
                                    ValueNone
                            finally
                                Marshal.FreeHGlobal(buffer)

                        match verb with
                        | ValueNone ->
                            { new IContextMenuResult with
                                member this.Id = string cmdId
                                member this.Name = text.Replace("&", null)
                                member this.Description = description
                                member this.Keywords = null
                                member this.Icon = icon
                                member this.IsSeparator = false
                                member this.Invoke() = ()
                                member this.GetContextMenu() = loadSubContextMenu path i }
                        | ValueSome verb ->
                            { new IContextMenuResult with
                                member this.Id = string cmdId
                                member this.Name = text.Replace("&", null)
                                member this.Description = description
                                member this.Keywords = null
                                member this.Icon = icon
                                member this.IsSeparator = false
                                member this.Invoke() = invokeContextMenuItem path verb
                                member this.GetContextMenu() = null } |]

            return results
        finally
            if not (isNull (box contextMenu)) then
                Marshal.ReleaseComObject(contextMenu) |> ignore
    } |> ValueOption.defaultValue null

let getContextMenuItems (path: string) =
    voption {
        use item = new ShellItem(path)

        let! parent = item.Parent
        use folder = new ShellFolder(parent)

        let contextMenu = folder.GetChildrenUIObjects<Shell32.IContextMenu>(HWND.NULL, item)

        try
            use hMenu = User32.CreatePopupMenu()
            let res = contextMenu.QueryContextMenu(hMenu, 0u, 1u, 0x7FFFu, Shell32.CMF.CMF_EXTENDEDVERBS)
            if res.Failed then return! ValueNone else

            let count = hMenu.GetItemCount()
            let mutable previousWasSeparator = false
            let results =
                [| for i in 0 .. count - 1 do
                    let mutable mii = User32.MENUITEMINFO()
                    mii.cbSize <- uint32 (Marshal.SizeOf(typeof<User32.MENUITEMINFO>))
                    mii.fMask <-
                        User32.MenuItemInfoMask.MIIM_ID
                        ||| User32.MenuItemInfoMask.MIIM_STRING
                        ||| User32.MenuItemInfoMask.MIIM_FTYPE
                        ||| User32.MenuItemInfoMask.MIIM_BITMAP
                        ||| User32.MenuItemInfoMask.MIIM_SUBMENU

                    // First call to get required string buffer size
                    mii.dwTypeData <- StrPtrAuto()
                    let succeeded = User32.GetMenuItemInfo(hMenu, uint32 i, true, &mii)
                    if not succeeded then () else

                    let cch = mii.cch + 1u
                    mii.dwTypeData <- StrPtrAuto(uint cch * 2u) // wide chars
                    mii.cch <- cch
                    let succeeded = User32.GetMenuItemInfo(hMenu, uint32 i, true, &mii)
                    if not succeeded then () else

                    let isSeparator = mii.fType.HasFlag(User32.MenuItemType.MFT_SEPARATOR)
                    let text =
                        if isSeparator || mii.dwTypeData.IsNull then ""
                        else mii.dwTypeData.ToString()
                    mii.dwTypeData.Free()

                    if isSeparator then
                        if not previousWasSeparator then
                            previousWasSeparator <- true
                            { new IContextMenuResult with
                                member this.Id = null
                                member this.Name = String.Empty
                                member this.Description = null
                                member this.Keywords = null
                                member this.Icon = StarterIconSource.Empty
                                member this.IsSeparator = true
                                member this.Invoke() = ()
                                member this.GetContextMenu() = null }
                    elif mii.wID <> 0u then
                        previousWasSeparator <- false
                        let cmdId = mii.wID - 1u // GetUIObjectOf offsets ids by idCmdFirst (1)

                        // Description (help text) via GetCommandString
                        let description =
                            try
                                let cchMax = 512u
                                let buffer = Marshal.AllocHGlobal(int cchMax * 2) // wide chars, 2 bytes each
                                try
                                    let hr =
                                        contextMenu.GetCommandString(
                                            UIntPtr(cmdId),
                                            Shell32.GCS.GCS_HELPTEXTW,
                                            IntPtr.Zero,
                                            buffer,
                                            cchMax
                                        )

                                    if hr.Succeeded then
                                        Marshal.PtrToStringUni(buffer) |> Option.ofObj |> Option.defaultValue ""
                                    else
                                        ""
                                finally
                                    Marshal.FreeHGlobal(buffer)
                            with _ -> ""

                        // Icon
                        let icon =
                            if mii.hbmpItem.IsInvalid then StarterIconSource.Empty else
                                try
                                    let i = mii.hbmpItem.ToAvaloniaBitmap()
                                    StarterIconSource(i, i)
                                with _ ->
                                    StarterIconSource.Empty

                        let verb =
                            let cchMax = 256u
                            let buffer = Marshal.AllocHGlobal(int cchMax * 2)
                            try
                                let hr = contextMenu.GetCommandString(
                                    UIntPtr(cmdId),
                                    Shell32.GCS.GCS_VERBW,
                                    IntPtr.Zero,
                                    buffer,
                                    cchMax
                                )
                                if hr.Succeeded then
                                    buffer
                                    |> Marshal.PtrToStringUni
                                    |> ValueOption.ofObj
                                else
                                    ValueNone
                            finally
                                Marshal.FreeHGlobal(buffer)

                        match verb with
                        | ValueNone ->
                            { new IContextMenuResult with
                                member this.Id = string cmdId
                                member this.Name = text.Replace("&", null)
                                member this.Description = description
                                member this.Keywords = null
                                member this.Icon = icon
                                member this.IsSeparator = false
                                member this.Invoke() = ()
                                member this.GetContextMenu() = loadSubContextMenu path i }
                        | ValueSome verb ->
                            { new IContextMenuResult with
                                member this.Id = string cmdId
                                member this.Name = text.Replace("&", null)
                                member this.Description = description
                                member this.Keywords = null
                                member this.Icon = icon
                                member this.IsSeparator = false
                                member this.Invoke() = invokeContextMenuItem path verb
                                member this.GetContextMenu() = null } |]

            return results
        finally
            if not (isNull (box contextMenu)) then
                Marshal.ReleaseComObject(contextMenu) |> ignore
    }


                    // let invoke () =
                    //     let mutable info = Shell32.CMINVOKECOMMANDINFO()
                    //     info.hwnd <- HWND.NULL
                    //     info.lpVerb <- Marshal.PtrToStringAnsi(IntPtr(cmdId))
                    //     info.nShow <- ShowWindowCommand.SW_SHOWNORMAL
                    //     contextMenu.InvokeCommand(&info) |> ignore


let loadAppContextMenu (path: string) =
    path
    |> getContextMenuItems
    |> function
        | ValueNone -> null
        | ValueSome results -> results

type ExeApplication =
    { Id: string
      Name: string
      Path: string
      Keywords: string array
      mutable AlternativeDescription: bool
      Icon: StarterIconSource }

    interface ISearchResult with
        member this.Id = this.Id
        member this.Name = this.Name
        member this.Description = if this.AlternativeDescription then this.Path else "Application"
        member this.Keywords = this.Keywords
        member this.Icon = this.Icon
        member this.ShowIfNoActivator = true
        member this.ActivatorFilter = Array.empty
        member this.GetContextMenu() = loadAppContextMenu this.Path

let runApp (app: ExeApplication) =
    ProcessStartInfo(
        FileName = app.Path,
        UseShellExecute = true
    )
    |> Process.Start
    |> function null -> () | d -> d.Dispose()

let private getAppFromFile (file: string) =
    voption {
        let! ext = file |> Path.GetExtension
        let ext = ext.ToLowerInvariant()
        do! match ext with
            | ".exe" | ".lnk" | ".url" -> ValueSome ()
            | _ -> ValueNone

        use! shellItem =
            try new ShellItem(file) |> ValueSome
            with _ -> ValueNone

        let! name = shellItem.GetDisplayName(ShellItemDisplayString.NormalDisplay)
        let icon =
            match ext = ".url" with
            | false ->
                Dispatcher.UIThread.Invoke(
                    (fun () -> file |> IconHelper.getFileIcon Constants.iconPixelSize),
                    DispatcherPriority.Background
                )
            | true -> file |> IconHelper.getUrlFileIcon
            |> ValueOption.defaultWith (fun () ->
                shellItem
                    .Images
                    .GetImage(SIZE(Constants.IconSize, Constants.IconSize), ShellItemGetImageOptions.IconOnly)
                    .ToAvaloniaBitmap()
            )

        return
            { Id = file
              Name = name
              Path = file
              Keywords = [| ext |]
              AlternativeDescription = false
              Icon = StarterIconSource(icon, icon) }
    }

let loadApplications (ct: CancellationToken) (config: FolderConfiguration) =
    let loadAppsOfFolder folder =
        task {
            if ct.IsCancellationRequested then return seq {} else

            let files =
                Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                |> Seq.filter (FolderConfiguration.isFileExcluded config >> not)
                |> Seq.toArray

            let apps = ConcurrentBag()

            do! Parallel.ForEachAsync(
                files,
                ParallelOptions(CancellationToken = ct),
                Func<_, _, ValueTask>(fun appFile _ ->
                    appFile
                    |> getAppFromFile
                    |> ValueOption.iter apps.Add

                    ValueTask.CompletedTask
                )
            )

            return apps :> _ seq
        }

    task {
        let! apps =
            config.Folders
            |> Array.map loadAppsOfFolder
            |> Task.WhenAll

        let appsDict = Dictionary()
        let appsOutput = ResizeArray<ISearchResult>()

        // Load apps according to the config order to prevent duplicate names
        apps |> Array.iter (Seq.iter (fun app ->
            if config.AllowDuplicates then
                if not <| appsDict.TryAdd(app.Name, app) then
                    logger.Verbose $"Duplicate app: {app.Path}"
                    appsDict[app.Name].AlternativeDescription <- true
                    app.AlternativeDescription <- true

                appsOutput.Add app
            else
                match appsDict.TryAdd(app.Name, app) with
                | false -> logger.Verbose $"Duplicate app (file is ignored): {app.Path}"
                | true -> appsOutput.Add app
        ))

        return appsOutput
    }

let observeFolder added removed (folder: string) =
    let watcher = new FileSystemWatcher(folder)

    watcher.Filters.Add("*.exe")
    watcher.Filters.Add("*.lnk")
    watcher.Filters.Add("*.url")
    watcher.NotifyFilter <-
        NotifyFilters.CreationTime
        ||| NotifyFilters.DirectoryName
        ||| NotifyFilters.FileName
        ||| NotifyFilters.LastWrite

    watcher.Created.Add(fun args ->
        args.FullPath
        |> getAppFromFile
        |> ValueOption.iter added
    )
    watcher.Deleted.Add(fun args -> args.FullPath |> removed)
    watcher.Renamed.Add(fun args ->
        args.OldFullPath |> removed
        args.FullPath
        |> getAppFromFile
        |> ValueOption.iter added
    )

    watcher.IncludeSubdirectories <- true
    watcher.EnableRaisingEvents <- true
    watcher :> IDisposable

type ExeAppsLoader() =
    let apps  = ResizeArray<ISearchResult>()
    let changedEvent = Event<unit>()
    let mutable watchers = Array.empty<IDisposable>

    let mutable cts = new CancellationTokenSource()

    member this.Apps = apps

    member this.LoadApps(folderConfig: FolderConfiguration) =
        cts.Cancel()
        cts <- new CancellationTokenSource()
        let ct = cts.Token
        apps.Clear()

        Task.Run<unit>(fun () -> task {
            let! newApps = loadApplications ct folderConfig
            if not ct.IsCancellationRequested then
                apps.AddRange newApps
                changedEvent.Trigger()
        })
        |> ignore

    member this.ObserveFolders(folderConfig: FolderConfiguration) =
        watchers |> Array.iter _.Dispose()
        watchers <- folderConfig.Folders |> Array.map (
            observeFolder
                (fun newApp ->
                    if newApp.Path
                       |> FolderConfiguration.isFileExcluded folderConfig
                       |> not then
                        apps.Add newApp
                        changedEvent.Trigger()
                )
                (fun appPathToRemove ->
                    let removedCount = apps.RemoveAll(fun app -> app.Id = appPathToRemove)
                    if removedCount <> 0 then changedEvent.Trigger()
                )
        )

    [<CLIEvent>]
    member this.Changed = changedEvent.Publish
