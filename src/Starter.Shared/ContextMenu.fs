module Starter.Shared.ContextMenu

open System
open System.Collections.Concurrent
open System.Runtime.InteropServices
open System.Threading
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
            thread.Join()

type WindowsContextMenuEntry =
    { Id: string
      CmdOffset: uint32
      Text: string
      Description: string
      Icon: StarterIconSource
      SubMenu: int voption }

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

let rec private loadContextMenuEntries
    itemCountLoaded
    resultLoaded
    resultFailed
    contextMenu
    (hMenu: User32.SafeHMENU)
    =
    let count = hMenu.GetItemCount()
    let mutable previousWasSeparator = false

    if count < 0 then
        let error =
            Marshal.GetLastWin32Error()
            |> uint32
            |> Win32Error
            |> _.ToString()

        Serilog.Log.Error($"Failed to load menu entries: {error}")
    else
        itemCountLoaded count

    for i in 0 .. count - 1 do
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
                    Choice1Of2 () |> resultLoaded i
            elif idCmdFirst <= mii.wID then
                previousWasSeparator <- false
                let cmdOffset = mii.wID - idCmdFirst // GetUIObjectOf offsets ids by idCmdFirst (1)

                let description = HMenu.loadDescription contextMenu cmdOffset
                let icon = HMenu.loadIcon mii

                use subMenu = new User32.SafeHMENU(hMenu.GetSub(i))

                { Id = string cmdOffset
                  CmdOffset = cmdOffset
                  Text = text.Replace("&", null)
                  Description = description |> ValueOption.defaultValue String.Empty
                  Icon = icon
                  SubMenu = if subMenu.IsInvalid then ValueNone else ValueSome i }
                |> Choice2Of2
                |> resultLoaded i
        with e ->
            resultFailed i e

type SubContextMenuLoader(
        sta: StaThreadDispatcher,
        contextMenu: Shell32.IContextMenu,
        itemIdx
    )
    =
    member private _.ParseContextMenuEntry(entry: WindowsContextMenuEntry) : IContextMenuResult =
        { new IContextMenuEntry with
            member this.Id = entry.Id
            member this.Name = entry.Text
            member this.Description = entry.Description
            member this.Keywords = null
            member this.Icon = entry.Icon
            member this.HasContextMenu = false
            member this.Invoke(platformHandle) =
                sta.Invoke(fun () ->
                    invokeContextMenuItem contextMenu entry.CmdOffset platformHandle
                )
                null }

    interface IContextMenuLoader with
        override this.LoadItems(itemsListed, resultLoaded, resultFailed, completed) =
            sta.Invoke(fun () ->
                use hMenu = User32.CreatePopupMenu()
                let res = contextMenu.QueryContextMenu(hMenu, 0u, idCmdFirst, 0x7FFFu, Shell32.CMF.CMF_EXTENDEDVERBS)
                if res.Failed then () else

                use subHMenu = new User32.SafeHMENU(hMenu.GetSub(itemIdx))
                if subHMenu.IsInvalid then () else

                loadContextMenuEntries
                    itemsListed.Invoke
                    (fun idx entry ->
                        let result =
                            match entry with
                            | Choice1Of2 () -> ContextMenuSeparator.Instance :> IContextMenuResult
                            | Choice2Of2 entry -> this.ParseContextMenuEntry entry

                        resultLoaded.Invoke(result, idx)
                    )
                    (fun idx error -> resultFailed.Invoke(error, idx))
                    contextMenu
                    subHMenu

                completed.Invoke()
            )


type ContextMenuLoader(itemPath: string) =
    let sta = new StaThreadDispatcher()
    let mutable savedInterfaces = ValueNone

    member private _.ParseContextMenuEntry(entry: WindowsContextMenuEntry) : IContextMenuResult =
        match entry.SubMenu with
        | ValueSome itemIdx -> // Sub menu
            { new IContextMenuEntry with
                member this.Id = entry.Id
                member this.Name = entry.Text
                member this.Description = entry.Description
                member this.Keywords = null
                member this.Icon = entry.Icon
                member this.HasContextMenu = true
                member this.Invoke platformHandle =
                    match savedInterfaces with
                    | ValueNone -> null
                    | ValueSome (contextMenu, _) ->
                        SubContextMenuLoader(sta, contextMenu, itemIdx) }

        | ValueNone -> // Action
            { new IContextMenuEntry with
                member this.Id = entry.Id
                member this.Name = entry.Text
                member this.Description = entry.Description
                member this.Keywords = null
                member this.Icon = entry.Icon
                member this.HasContextMenu = false
                member this.Invoke(platformHandle) =
                    match savedInterfaces with
                    | ValueNone -> ()
                    | ValueSome (contextMenu, _) ->
                        sta.Invoke(fun () ->
                            invokeContextMenuItem contextMenu entry.CmdOffset platformHandle
                        )
                    null }

    interface IContextMenuLoader with
        override this.LoadItems(itemsListed, resultLoaded, resultFailed, completed) =
            sta.Invoke(fun () ->
                match loadContextMenuInterface itemPath with
                | ValueNone -> ()
                | ValueSome (contextMenu, hMenu, disposable) ->
                    savedInterfaces <- ValueSome (contextMenu, disposable)

                    loadContextMenuEntries
                        itemsListed.Invoke
                        (fun idx entry ->
                            let result =
                                match entry with
                                | Choice1Of2 () -> ContextMenuSeparator.Instance :> IContextMenuResult
                                | Choice2Of2 entry -> this.ParseContextMenuEntry entry

                            resultLoaded.Invoke(result, idx)
                        )
                        (fun idx error -> resultFailed.Invoke(error, idx))
                        contextMenu
                        hMenu

                completed.Invoke()
            )

    interface IDisposable with
        override _.Dispose() =
            savedInterfaces |> ValueOption.iter (fun (_, disposable) ->
                disposable.Dispose()
            )

            (sta :> IDisposable).Dispose()
