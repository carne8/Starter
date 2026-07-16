module Starter.Shared.ContextMenu

open System
open System.Collections.Concurrent
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

type WindowsContextMenuEntry =
    { Id: string
      CmdId: uint32
      Text: string
      Description: string
      Icon: StarterIconSource
      SubMenu: int voption }

let private loadContextMenuInterface (shellItemPath: string) (f: Shell32.IContextMenu -> User32.SafeHMENU -> 'a) =
    voption {
        use item = new ShellItem(shellItemPath)
        let! parent = item.Parent |> ValueOption.ofObj
        use folder = new ShellFolder(parent)

        let contextMenu = folder.GetChildrenUIObjects<Shell32.IContextMenu>(HWND.NULL, item)
        try
            use hMenu = User32.CreatePopupMenu()
            let res = contextMenu.QueryContextMenu(hMenu, 0u, 1u, 0x7FFFu, Shell32.CMF.CMF_NORMAL)
            if res.Failed then return! ValueNone else

            return f contextMenu hMenu
        finally
            if not (isNull (box contextMenu)) then
                Marshal.ReleaseComObject(contextMenu) |> ignore
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


let private invokeContextMenuItem (shellItemPath: string) (cmdId: uint32) (platformHandle: IPlatformHandle) =
    loadContextMenuInterface shellItemPath (fun contextMenu _ ->
        let mutable info = Shell32.CMINVOKECOMMANDINFOEX(int cmdId)

        info.fMask <- Shell32.CMIC.CMIC_MASK_UNICODE
        info.hwnd <- HWND(platformHandle.Handle)
        info.nShow <- ShowWindowCommand.SW_SHOWNORMAL

        contextMenu.InvokeCommand(&info) |> ignore
        ValueSome ()
    ) |> ignore

let rec private loadContextMenuEntries
    itemCountLoaded
    resultLoaded
    resultFailed
    contextMenu
    (hMenu: User32.SafeHMENU)
    =
    let count = hMenu.GetItemCount()
    let mutable previousWasSeparator = false

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
                    Choice1Of2 () |> resultLoaded i
            elif mii.wID <> 0u then
                previousWasSeparator <- false
                let cmdId = mii.wID - 1u // GetUIObjectOf offsets ids by idCmdFirst (1)

                let description = HMenu.loadDescription contextMenu cmdId
                let icon = HMenu.loadIcon mii

                use subMenu = new User32.SafeHMENU(hMenu.GetSub(i))

                { Id = string cmdId
                  CmdId = cmdId
                  Text = text.Replace("&", null)
                  Description = description |> ValueOption.defaultValue String.Empty
                  Icon = icon
                  SubMenu = if subMenu.IsInvalid then ValueNone else ValueSome i }
                |> Choice2Of2
                |> resultLoaded i
        with e ->
            resultFailed i e

type SubContextMenuLoader(itemPath: string, itemIdx) =
    member private _.ParseContextMenuEntry(entry: WindowsContextMenuEntry) : IContextMenuResult =
        { new IContextMenuEntry with
            member this.Id = entry.Id
            member this.Name = entry.Text
            member this.Description = entry.Description
            member this.Keywords = null
            member this.Icon = entry.Icon
            member this.Invoke(platformHandle) =
                invokeContextMenuItem itemPath entry.CmdId platformHandle
                null }

    interface IContextMenuLoader with
        override this.LoadItems(itemsListed, resultLoaded, resultFailed, completed) =
            let thread =
                Thread(fun () ->
                    loadContextMenuInterface itemPath (fun contextMenu hMenu ->
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
                    ) |> ignore

                    completed.Invoke()
                )

            thread.SetApartmentState(ApartmentState.STA)
            thread.IsBackground <- true
            thread.Start()

type ContextMenuLoader(itemPath: string) =
    member private _.ParseContextMenuEntry(entry: WindowsContextMenuEntry) : IContextMenuResult =
        match entry.SubMenu with
        | ValueSome itemIdx -> // Sub menu
            { new IContextMenuEntry with
                member this.Id = entry.Id
                member this.Name = entry.Text
                member this.Description = entry.Description
                member this.Keywords = null
                member this.Icon = entry.Icon
                member this.Invoke _ = SubContextMenuLoader(itemPath, itemIdx) }

        | ValueNone -> // Action
            { new IContextMenuEntry with
                member this.Id = entry.Id
                member this.Name = entry.Text
                member this.Description = entry.Description
                member this.Keywords = null
                member this.Icon = entry.Icon
                member this.Invoke(platformHandle) =
                    invokeContextMenuItem itemPath entry.CmdId platformHandle
                    null }

    interface IContextMenuLoader with
        override this.LoadItems(itemsListed, resultLoaded, resultFailed, completed) =
            let thread =
                Thread(fun () ->
                    loadContextMenuInterface itemPath (
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
                    ) |> ignore

                    completed.Invoke()
                )

            thread.SetApartmentState(ApartmentState.STA)
            thread.IsBackground <- true
            thread.Start()
