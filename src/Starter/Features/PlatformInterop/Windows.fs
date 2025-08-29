namespace Starter.Features.PlatformInterop

open System
open System.IO
open Avalonia.Controls
open Starter.Features
open Starter.Features.Logging
open Vanara.PInvoke
open Vanara.Windows.Shell

type Windows() =
    inherit PlatformInterop()

    static let startupFolder = Environment.SpecialFolder.Startup |> Environment.GetFolderPath
    static let startupFile = Path.Combine(startupFolder, Constants.Platform.Windows.StartupFile)

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
