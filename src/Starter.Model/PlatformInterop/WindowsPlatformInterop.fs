namespace Starter.Features.PlatformInterop

open System
open System.IO
open System.Threading.Tasks
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Win32.Input
open Serilog
open Starter.Features
open Starter.Features.Logging
open Vanara.PInvoke
open Vanara.Windows.Shell
open FsToolkit.ErrorHandling
open Helpers

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

type WindowsPlatformInterop() =

    static let startupFolder = Environment.SpecialFolder.Startup |> Environment.GetFolderPath
    static let startupFile = Path.Combine(startupFolder, Constants.Platform.Windows.StartupFile)

    [<Literal>]
    static let hotkeyId = 0

    interface IPlatformInterop with
        override _.ToggleLaunchAtStartup(enable) =
            try
                match enable, File.Exists startupFile with
                | false, true -> File.Delete startupFile
                | true, false ->
                    use shortcut = new ShellLink(
                        Constants.ProcessExecutableFile,
                        null,
                        Constants.ProcessDirectory,
                        "Starter",
                        IconLocation = IconLocation(Constants.ProcessExecutableFile, 0)
                    )

                    shortcut.SaveAs startupFile
                | _ -> ()
            with err ->
                Log.Error(err, "Failed to toggle launch at startup")

        override _.IsLaunchAtStartupEnabled() = File.Exists startupFile

        override _.HotkeyRegistrable = true
        override _.RegisterHotkey shortcut window =
            result {
                let! platformHandle =
                    window.TryGetPlatformHandle()
                    |> Result.requireNotNull "Failed to retrieve window platform handle"

                User32.UnregisterHotKey(platformHandle.Handle, hotkeyId) |> ignore

                let! key =
                    shortcut.Key
                    |> VK.fromKey
                    |> Result.ofValueOption $"Failed to parse key: {shortcut.Key}"

                let res = User32.RegisterHotKey(
                    platformHandle.Handle,
                    hotkeyId,
                    shortcut.Modifiers |> HotKeyModifiers.fromKeys,
                    key
                )

                match res with
                | false -> return! Error "Failed to setup hotkey"
                | true -> return ()
            }
            |> function
                | Ok () -> ValueTask.FromResult false
                | Error err ->
                    logger.Error err
                    ValueTask.FromResult true

        override _.SetupHotkeyCallback(window: Window) =
            let wndProcCallback =
                Win32Properties.CustomWndProcHookCallback(
                    fun (_hWnd: nativeint) (msg: uint32) (_wParam: nativeint) (_lParam: nativeint) _ ->
                        if msg = uint User32.WindowMessage.WM_HOTKEY then
                            window.Show()
                        0
                )

            Win32Properties.AddWndProcHookCallback(window, wndProcCallback)
