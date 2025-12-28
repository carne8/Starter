namespace Starter.Features.PlatformInterop

open System
open System.IO
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Win32.Input
open Starter.Features
open Starter.Features.Logging
open Vanara.PInvoke
open Vanara.Windows.Shell

module HotKeyModifiers =
    let fromKeys =
        Array.fold
            (fun modifiers key ->
                match key with
                | Key.LWin
                | Key.RWin -> modifiers ||| User32.HotKeyModifiers.MOD_WIN
                | Key.LeftAlt
                | Key.RightAlt -> modifiers ||| User32.HotKeyModifiers.MOD_ALT
                | Key.LeftCtrl
                | Key.RightCtrl -> modifiers ||| User32.HotKeyModifiers.MOD_CONTROL
                | Key.LeftShift
                | Key.RightShift -> modifiers ||| User32.HotKeyModifiers.MOD_SHIFT
                | _ -> modifiers
            )
            User32.HotKeyModifiers.MOD_NOREPEAT

module VK =
    let fromKey key =
        match key with
        | Key.None ->
            logger.Error "Can't parse key None."
            ValueNone
        | Key.LWin
        | Key.RWin
        | Key.LeftAlt
        | Key.RightAlt
        | Key.LeftCtrl
        | Key.RightCtrl
        | Key.LeftShift
        | Key.RightShift ->
            logger.Error $"{key} is a modifier key. Can't register hotkey."
            ValueNone
        | key ->
            key
            |> KeyInterop.VirtualKeyFromKey
            |> uint32
            |> ValueSome

type Windows() =
    inherit PlatformInterop()

    static let startupFolder = Environment.SpecialFolder.Startup |> Environment.GetFolderPath
    static let startupFile = Path.Combine(startupFolder, Constants.Platform.Windows.StartupFile)

    [<Literal>]
    static let hotkeyId = 0

    override _.ToggleLaunchAtStartup(enable) =
        match enable, File.Exists startupFile with
        | false, true -> File.Delete startupFile
        | true, false ->
            use shortcut = new ShellLink(
                Constants.Platform.Windows.StartupFile,
                null,
                startupFolder,
                TargetPath = Constants.ProcessExecutableFile,
                Description = "Starter",
                IconLocation = IconLocation(Constants.ProcessExecutableFile, 0)
            )

            shortcut.SaveAs startupFile
        | _ -> ()

    override _.IsLaunchAtStartupEnabled() = File.Exists startupFile

    override _.RegisterHotkey modifiers key window =
        match window.TryGetPlatformHandle() with
        | null -> logger.Fatal "Failed to retrieve window platform handle"
        | platformHandle ->
            User32.UnregisterHotKey(platformHandle.Handle, hotkeyId) |> ignore

            key
            |> VK.fromKey
            |> ValueOption.iter (fun key ->
                User32.RegisterHotKey(
                    platformHandle.Handle,
                    hotkeyId,
                    modifiers |> HotKeyModifiers.fromKeys,
                    key
                ) |> ignore
            )


    override _.SetupHotkeyCallback(window: Window) =
        let wndProcCallback =
            Win32Properties.CustomWndProcHookCallback(
                fun (_hWnd: nativeint) (msg: uint32) (_wParam: nativeint) (_lParam: nativeint) _ ->
                    if msg = uint User32.WindowMessage.WM_HOTKEY then
                        window.Show()
                    0
            )

        Win32Properties.AddWndProcHookCallback(window, wndProcCallback)
