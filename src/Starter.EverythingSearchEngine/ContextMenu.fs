module Starter.EverythingSearchEngine.ContextMenu

open System
open System.Runtime.InteropServices
open FsToolkit.ErrorHandling
open Starter.EverythingSearchEngine.IconHelper
open Starter.SearchEngine
open Vanara.InteropServices
open Vanara.PInvoke
open Vanara.Windows.Shell

let private loadContextMenuInterface (shellItemPath: string) (f: Shell32.IContextMenu -> User32.SafeHMENU -> _) =
    voption {
        use item = new ShellItem(shellItemPath)
        let! parent = item.Parent
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

    let loadVerb (contextMenu: Shell32.IContextMenu) (cmdId: uint32) =
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
                    Marshal.PtrToStringUni(buffer) |> Option.ofObj |> Option.defaultValue ""
                else
                    ""
            finally
                Marshal.FreeHGlobal(buffer)
        with _ -> ""


let private invokeContextMenuItem (shellItemPath: string) (verb: string) =
    loadContextMenuInterface shellItemPath (fun contextMenu _ ->
        let mutable info = Shell32.CMINVOKECOMMANDINFO(verb)
        info.hwnd <- HWND.NULL
        info.nShow <- ShowWindowCommand.SW_SHOWNORMAL
        contextMenu.InvokeCommand(&info) |> ignore
    ) |> ignore

let rec private loadContextMenuEntries contextMenu (hMenu: User32.SafeHMENU) =
    voption {
        let count = hMenu.GetItemCount()
        let mutable previousWasSeparator = false
        return
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
                        Choice1Of2()
                elif mii.wID <> 0u then
                    previousWasSeparator <- false
                    let cmdId = mii.wID - 1u // GetUIObjectOf offsets ids by idCmdFirst (1)

                    let description = HMenu.loadDescription contextMenu cmdId
                    let icon = HMenu.loadIcon mii
                    let verb = HMenu.loadVerb contextMenu cmdId

                    use subMenu = new User32.SafeHMENU(hMenu.GetSub(i))

                    struct {|
                        Id = string cmdId
                        Text = text.Replace("&", null)
                        Description = description
                        Icon = icon
                        Verb = verb
                        SubMenu = if subMenu.IsInvalid then ValueNone else ValueSome i
                    |}
                    |> Choice2Of2 |]
    }

let private loadSubContextMenu path itemIdx =
    loadContextMenuInterface path (fun contextMenu hMenu -> voption {
        use subHMenu = new User32.SafeHMENU(hMenu.GetSub(itemIdx))
        if subHMenu.IsInvalid then return! ValueNone else
        return! loadContextMenuEntries contextMenu subHMenu
    })
    |> ValueOption.bind id
    |> ValueOption.map (Array.choose (function
        | Choice1Of2 () -> None
        | Choice2Of2 entry ->
            match entry.Verb with
            | ValueNone ->
                { new IContextMenuResult with
                    member this.Id = entry.Id
                    member this.Name = "NO VERB: " + entry.Text
                    member this.Description = entry.Description
                    member this.Keywords = null
                    member this.Icon = entry.Icon
                    member this.IsSeparator = false
                    member this.Invoke() = ()
                    member this.GetContextMenu() = null }
                |> Some
            | ValueSome verb ->
                { new IContextMenuResult with
                    member this.Id = entry.Id
                    member this.Name = entry.Text
                    member this.Description = entry.Description
                    member this.Keywords = null
                    member this.Icon = entry.Icon
                    member this.IsSeparator = false
                    member this.Invoke() = invokeContextMenuItem path verb
                    member this.GetContextMenu() = null }
                |> Some
    ))
    |> ValueOption.defaultValue null

let loadContextMenu (path: string) =
    loadContextMenuInterface path (fun contextMenu hMenu ->
        loadContextMenuEntries contextMenu hMenu
        |> ValueOption.map (Array.choose (function
            | Choice1Of2 () ->
                { new IContextMenuResult with
                    member this.Id = null
                    member this.Name = String.Empty
                    member this.Description = null
                    member this.Keywords = null
                    member this.Icon = StarterIconSource.Empty
                    member this.IsSeparator = true
                    member this.Invoke() = ()
                    member this.GetContextMenu() = null }
                |> Some
            | Choice2Of2 entry ->
                match entry.Verb, entry.SubMenu with
                | ValueSome verb, ValueNone ->
                    { new IContextMenuResult with
                        member this.Id = entry.Id
                        member this.Name = entry.Text
                        member this.Description = entry.Description
                        member this.Keywords = null
                        member this.Icon = entry.Icon
                        member this.IsSeparator = false
                        member this.Invoke() = invokeContextMenuItem path verb
                        member this.GetContextMenu() = null }
                    |> Some
                | ValueNone, ValueSome itemIdx ->
                    { new IContextMenuResult with
                        member this.Id = entry.Id
                        member this.Name = entry.Text
                        member this.Description = entry.Description
                        member this.Keywords = null
                        member this.Icon = entry.Icon
                        member this.IsSeparator = false
                        member this.Invoke() = ()
                        member this.GetContextMenu() = loadSubContextMenu path itemIdx }
                    |> Some
                | _ -> None
        ))
    )
    |> ValueOption.bind id
    |> ValueOption.defaultValue null
