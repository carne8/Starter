namespace Starter.Features.PlatformInterop

open System
open System.IO
open Avalonia.Controls
open Starter.Features.Logging
open Vanara.PInvoke
open Vanara.Windows.Shell

module Constants = Starter.Features.Constants.Platform.Windows

type Windows() =
    inherit PlatformInterop()

    static let startupFolder = Environment.SpecialFolder.Startup |> Environment.GetFolderPath
    static let startupFile = Path.Combine(startupFolder, Constants.StartupFile)
    static let processFile =
        match Environment.ProcessPath with
        | null -> failwith "No process path available"
        | path -> path

    override _.ToggleLaunchAtStartup(enable) =
        match enable with
        | false ->
            if File.Exists startupFile then File.Delete startupFile
        | true ->
            if not <| File.Exists startupFile then
                use shortcut = new ShellLink(
                    Constants.StartupFile,
                    null,
                    startupFolder,
                    TargetPath = processFile,
                    Description = "Starter",
                    IconLocation = IconLocation(processFile, 0)
                )

                shortcut.SaveAs startupFile

    override _.IsLaunchAtStartupEnabled() = File.Exists startupFile

    member _.RegisterHotkey(window: Window) =
        match window.TryGetPlatformHandle() with
        | null ->
            logger.Fatal "Failed to retrieve window platform handle"
            failwith "Failed to retrieve window platform handle"
        | platformHandle ->
            User32.RegisterHotKey(
                platformHandle.Handle,
                0, // Hotkey id
                User32.HotKeyModifiers.MOD_ALT,
                User32.VK.VK_SPACE |> uint
            ) |> ignore


    member _.SetupHotkeyCallback(window: Window) =
        let wndProcCallback =
            Win32Properties.CustomWndProcHookCallback(
                fun (_hWnd: nativeint) (msg: uint32) (_wParam: nativeint) (_lParam: nativeint) _ ->
                    if msg = uint User32.WindowMessage.WM_HOTKEY then
                        window.Show()
                    0
            )

        Win32Properties.AddWndProcHookCallback(window, wndProcCallback)
