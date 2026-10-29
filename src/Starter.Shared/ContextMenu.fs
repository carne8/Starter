module Starter.Shared.ContextMenu

open System
open System.ComponentModel
open System.Collections.Concurrent
open System.Collections.ObjectModel
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks
open Avalonia.Platform
open FsToolkit.ErrorHandling
open Starter.SearchEngine
open Starter.Shared.IconHelper
open Vanara.InteropServices
open Vanara.PInvoke
open Vanara.Windows.Shell

type StaThreadDispatcher() =
    let queue = new BlockingCollection<unit -> unit>()

    let thread =
        Thread(fun () ->
            for action in queue.GetConsumingEnumerable() do
                action ()
        )

    do
        thread.SetApartmentState(ApartmentState.STA)
        thread.IsBackground <- true
        thread.Start()

    member _.Invoke(func: unit -> unit) = queue.Add func

    interface IDisposable with
        member _.Dispose() =
            queue.CompleteAdding()
            Task.Run<unit>(fun () -> thread.Join()) |> ignore

type WindowsContextMenuEntry(index, invokeSubMenu, invoke) =
    let mutable status = ContextMenuStatus.Loading
    let mutable data = ValueNone
    let propertyChangedEvent = Event<PropertyChangedEventHandler, PropertyChangedEventArgs>()

    member _.Index = index

    member this.SetLoaded(
        id: string | null,
        cmdOffset: uint32,
        text: string,
        description: string | null,
        icon: StarterIconSource,
        subMenu: int voption
    ) =
        status <- ContextMenuStatus.Loaded
        data <- ValueSome struct {|
            Id = id;
            CmdOffset = cmdOffset;
            Text = text;
            Description = description;
            Icon = icon;
            SubMenu = subMenu
        |}

        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs null)

    member this.SetSeparator() =
        status <- ContextMenuStatus.Separator
        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs(nameof this.Status))

    member this.SetFailed() =
        status <- ContextMenuStatus.Failed
        propertyChangedEvent.Trigger(this, PropertyChangedEventArgs(nameof this.Status))

    member _.Status = status
    member _.CmdOffset = data |> ValueOption.get |> _.CmdOffset

    interface IContextMenuEntry with
        member _.Status = status
        member _.Id = data |> ValueOption.get |> _.Id
        member _.Name = data |> ValueOption.map _.Text |> ValueOption.defaultValue null
        member _.Description = data |> ValueOption.map _.Description |> ValueOption.defaultValue null
        member _.Keywords = null
        member _.Icon = data |> ValueOption.map _.Icon |> ValueOption.defaultValue StarterIconSource.Empty
        member this.Invoke() =
            match data |> ValueOption.bind _.SubMenu with
            | ValueNone -> invoke this
            | ValueSome subMenu -> invokeSubMenu subMenu

    interface INotifyPropertyChanged with
        [<CLIEvent>]
        member _.PropertyChanged = propertyChangedEvent.Publish


let idCmdFirst = 1u

let private loadContextMenuInterface (shellItemPath: string) =
    voption {
        let item = new ShellItem(shellItemPath)
        let! parent = item.Parent |> ValueOption.ofObj
        let folder = new ShellFolder(parent)

        let contextMenu = folder.GetChildrenUIObjects<Shell32.IContextMenu>(HWND.NULL, item)
        let hMenu = User32.CreatePopupMenu()

        let disposable =
            { new IDisposable with
                override _.Dispose() =
                    (item :> IDisposable).Dispose()
                    (folder :> IDisposable).Dispose()
                    (hMenu :> IDisposable).Dispose()
                    if not (isNull (box contextMenu)) then
                        Marshal.ReleaseComObject(contextMenu) |> ignore }

        let res = contextMenu.QueryContextMenu(hMenu, 0u, idCmdFirst, 0x7FFFu, Shell32.CMF.CMF_EXTENDEDVERBS)
        if res.Failed then
            disposable.Dispose()
            return! ValueNone
        else
            return contextMenu, hMenu, disposable
    }

module private HMenu =
    let loadIcon (mii: User32.MENUITEMINFO) =
        if mii.hbmpItem.IsInvalid then StarterIconSource.Empty else
            try
                let i = mii.hbmpItem.ToAvaloniaBitmap()
                StarterIconSource(i, i)
            with _ ->
                StarterIconSource.Empty

    let loadDescription (contextMenu: Shell32.IContextMenu) (cmdId: uint32) =
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
                    Marshal.PtrToStringUni(buffer)
                    |> ValueOption.ofObj
                    |> ValueOption.map _.Replace("&", null)
                else
                    ValueNone
            finally
                Marshal.FreeHGlobal(buffer)
        with _ -> ValueNone


let private invokeContextMenuItem
    (contextMenu: Shell32.IContextMenu)
    (cmdOffset: uint32)
    (platformHandle: IPlatformHandle)
    =
    let mutable info = Shell32.CMINVOKECOMMANDINFOEX()

    info.lpVerb <- ResourceId.op_Implicit(nativeint cmdOffset)
    info.fMask <- Shell32.CMIC.CMIC_MASK_UNICODE
    info.hwnd <- HWND(platformHandle.Handle)
    info.nShow <- ShowWindowCommand.SW_SHOWNORMAL

    contextMenu.InvokeCommand(&info) |> ignore

let rec private loadContextMenuEntries itemsCountLoaded separatorLoaded itemLoaded itemFailed contextMenu (hMenu: User32.SafeHMENU) =
    let count = hMenu.GetItemCount()
    let mutable previousWasSeparator = false

    if count < 0 then
        let error =
            Marshal.GetLastWin32Error()
            |> uint32
            |> Win32Error
            |> _.ToString()

        Serilog.Log.Error($"Failed to load menu entries: {error}")
        false
    else
        itemsCountLoaded count
        for i = 0 to count - 1 do
            try
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

                let cch = mii.cch + idCmdFirst
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
                        separatorLoaded i
                elif idCmdFirst <= mii.wID then
                    previousWasSeparator <- false
                    let cmdOffset = mii.wID - idCmdFirst // GetUIObjectOf offsets ids by idCmdFirst (1)

                    let description = HMenu.loadDescription contextMenu cmdOffset
                    let icon = HMenu.loadIcon mii

                    use subMenu = new User32.SafeHMENU(hMenu.GetSub(i))

                    struct {|
                        Id = string cmdOffset
                        CmdOffset = cmdOffset
                        Text = text.Replace("&", null)
                        Description = description |> ValueOption.defaultValue String.Empty
                        Icon = icon
                        SubMenu = if subMenu.IsInvalid then ValueNone else ValueSome i
                    |}
                    |> itemLoaded i
            with e ->
                itemFailed i e
        true

type SubContextMenuLoader(
        sta: StaThreadDispatcher,
        contextMenu: Shell32.IContextMenu,
        itemIdx
    )
    =

    member private _.Invoke platformHandle (entry: WindowsContextMenuEntry) =
        sta.Invoke(fun () ->
            invokeContextMenuItem contextMenu entry.CmdOffset platformHandle
        )
        null

    interface IContextMenuLoader with
        override this.LoadItems(platformHandle) =
            let tcs = TaskCompletionSource<_>()
            sta.Invoke(fun () ->
                use hMenu = User32.CreatePopupMenu()
                let res = contextMenu.QueryContextMenu(hMenu, 0u, idCmdFirst, 0x7FFFu, Shell32.CMF.CMF_EXTENDEDVERBS)
                if res.Failed then () else

                use subHMenu = new User32.SafeHMENU(hMenu.GetSub(itemIdx))
                if subHMenu.IsInvalid then () else

                let mutable entries = null
                let mutable observableCollection = null

                let succeed =
                    loadContextMenuEntries
                        (fun count -> // Items count loaded
                            entries <-
                                Array.init count (fun idx ->
                                    WindowsContextMenuEntry(
                                        idx,
                                        (fun _ -> null),
                                        this.Invoke platformHandle
                                    )
                                )
                            observableCollection <-
                                entries
                                |> Seq.cast<IContextMenuEntry>
                                |> ObservableCollection

                            tcs.SetResult observableCollection
                        )
                        (fun i -> entries[i].SetSeparator()) // Separator loaded
                        (fun i entry -> // Item loaded
                            entries[i].SetLoaded(
                                entry.Id,
                                entry.CmdOffset,
                                entry.Text,
                                entry.Description,
                                entry.Icon,
                                entry.SubMenu
                            )

                            if i = entries.Length - 1 then // When last item has been loaded
                                entries |> Seq.iter (fun e ->
                                    if e.Status = ContextMenuStatus.Loading then
                                        observableCollection.Remove e |> ignore
                                )
                        )
                        (fun i _ -> // Item failed
                            entries[i].SetFailed()
                            observableCollection.Remove entries[i] |> ignore
                        )
                        contextMenu
                        subHMenu

                if not succeed then
                    tcs.SetException(exn "Failed to load context menu entries.")
            )

            tcs.Task


type ContextMenuLoader(itemPath: string) =
    let mutable sta: StaThreadDispatcher | null = null
    let mutable savedInterfaces = ValueNone

    member private _.Invoke platformHandle (entry: WindowsContextMenuEntry) =
        match savedInterfaces with
        | ValueNone -> ()
        | ValueSome (contextMenu, _) ->
            sta.Invoke(fun () ->
                invokeContextMenuItem contextMenu entry.CmdOffset platformHandle
            )
        null

    member private _.InvokeSubMenu subMenu =
        match savedInterfaces with
        | ValueNone ->
            null :> IContextMenuLoader
        | ValueSome (contextMenu, _) ->
            SubContextMenuLoader(sta, contextMenu, subMenu)

    interface IContextMenuLoader with
        override this.LoadItems(platformHandle) =
            let tcs = TaskCompletionSource<ObservableCollection<_>>()
            sta <- new StaThreadDispatcher()
            sta.Invoke(fun () ->
                match loadContextMenuInterface itemPath with
                | ValueNone -> tcs.SetException(exn "Failed to load context menu interface.")
                | ValueSome (contextMenu, hMenu, disposable) ->
                    savedInterfaces <- ValueSome (contextMenu, disposable)

                    let mutable entries = null
                    let mutable observableCollection = null

                    let res =
                        loadContextMenuEntries
                            (fun count -> // Items count loaded
                                entries <-
                                    Array.init count (fun idx ->
                                        WindowsContextMenuEntry(
                                            idx,
                                            this.InvokeSubMenu,
                                            this.Invoke platformHandle
                                        )
                                    )
                                observableCollection <-
                                    entries
                                    |> Seq.cast<IContextMenuEntry>
                                    |> ObservableCollection

                                tcs.SetResult observableCollection
                            )
                            (fun i -> entries[i].SetSeparator()) // Separator loaded
                            (fun i entry -> // Item loaded
                                entries[i].SetLoaded(
                                    entry.Id,
                                    entry.CmdOffset,
                                    entry.Text,
                                    entry.Description,
                                    entry.Icon,
                                    entry.SubMenu
                                )

                                if i = entries.Length - 1 then // When last item has been loaded
                                    entries |> Seq.iter (fun e ->
                                        if e.Status = ContextMenuStatus.Loading then
                                            observableCollection.Remove e |> ignore
                                    )
                            )
                            (fun i _ -> // Item failed
                                entries[i].SetFailed()
                                observableCollection.Remove entries[i] |> ignore
                            )
                            contextMenu
                            hMenu

                    if not res then
                        tcs.SetException(exn "Failed to load context menu entries.")
            )

            tcs.Task

    interface IDisposable with
        override _.Dispose() =
            savedInterfaces |> ValueOption.iter (fun (_, disposable) ->
                disposable.Dispose()
            )

            if sta |> isNull |> not then
                (sta :> IDisposable).Dispose()
