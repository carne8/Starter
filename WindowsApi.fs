module WindowsApi

open System.Runtime.InteropServices

type HHOOK = nativeint
type HookProc = delegate of int * nativeint * nativeint -> nativeint

[<RequireQualifiedAccess>]
module IdHook =
    let WH_KEYBOARD_LL: int = 13

module User32 =
    [<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern HHOOK SetWindowsHookExA(
        int idHook,
        HookProc lpfn,
        nativeint hmod,
        uint32 dwThreadId
    )

    [<DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)>]
    extern bool UnhookWindowsHookEx(HHOOK hhk)
