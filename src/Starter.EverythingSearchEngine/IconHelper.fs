module Starter.EverythingSearchEngine.IconHelper

#nowarn 9

open System
open System.IO
open System.Runtime.InteropServices
open Microsoft.FSharp.NativeInterop
open Vanara.PInvoke

type HBITMAP with
    member this.ToAvaloniaBitmap() =
        let bitmap = Gdi32.GetObject<Gdi32.BITMAP> this

        let size = Avalonia.PixelSize(bitmap.bmWidth, bitmap.bmHeight)
        let dpi = Avalonia.Vector(96, 96)
        let stride = bitmap.bmWidthBytes

        use hDc = User32.GetDC IntPtr.Zero
        let bitmapInfo = Gdi32.BITMAPINFO(
            bmiHeader = Gdi32.BITMAPINFOHEADER(
                biSize = uint sizeof<Gdi32.BITMAPINFOHEADER>,
                biWidth = size.Width,
                biHeight = -size.Height, // Negative height for top-down DIB
                biPlanes = 1us,
                biBitCount = bitmap.bmBitsPixel,
                biCompression = Gdi32.BitmapCompressionMode.BI_RGB
            )
        )
        use safeBitmapInfo = new Gdi32.SafeBITMAPINFO(&bitmapInfo)
        let unmanagedData = Marshal.AllocHGlobal(bitmap.bmHeight * stride)

        try
            match Gdi32.GetDIBits(hDc, this, 0u, uint size.Height, unmanagedData, safeBitmapInfo, Gdi32.DIBColorMode.DIB_RGB_COLORS) with
            | 0 -> failwith "Failed to get DIB pixel data."
            | _ ->
                new Avalonia.Media.Imaging.Bitmap(
                    Avalonia.Platform.PixelFormat.Bgra8888,
                    Avalonia.Platform.AlphaFormat.Unpremul,
                    unmanagedData, size, dpi, stride
                )
        finally
            Marshal.FreeHGlobal unmanagedData

type Gdi32.SafeHBITMAP with
    /// Warning: this method does not dispose the current HBITMAP
    member this.ToAvaloniaBitmap() = HBITMAP(this.DangerousGetHandle()).ToAvaloniaBitmap()

type User32.SafeHICON with
    member this.ToAvaloniaBitmap() =
        /// Warning: this method does not dispose the current HICON
        use hBitmap = this.ToHBITMAP()
        hBitmap.ToAvaloniaBitmap()

module IconHelper =
    open FsToolkit.ErrorHandling

    let private getFileHIcon imageListSize (filePath: string) =
        let imageList = Shell32.SHGetImageList imageListSize
        let mutable fileInfo = Shell32.SHFILEINFO()

        let res = Shell32.SHGetFileInfo(
            filePath,
            FileAttributes.None,
            &fileInfo,
            sizeof<Shell32.SHFILEINFO>,
            Shell32.SHGFI.SHGFI_ICON ||| Shell32.SHGFI.SHGFI_LARGEICON
        )

        if res = IntPtr.Zero then
            failwith "Failed to retrieve icon info"

        // Dispose the useless HICON
        new User32.SafeHICON(fileInfo.hIcon) |> _.Dispose()

        match fileInfo.iIcon with
        | 0 | 2 -> ValueNone // Avoid default icons
        | _ ->
            use hIcon = imageList.GetIcon(
                fileInfo.iIcon,
                ComCtl32.IMAGELISTDRAWFLAGS.ILD_NORMAL
            )

            match hIcon.IsNull with
            | true -> ValueNone
            | false -> hIcon.ToAvaloniaBitmap() |> ValueSome

    let private isValidIcon (bitmap: Avalonia.Media.Imaging.Bitmap) =
        // Some .exe files doesn't have high resolution icon
        // In these cases, the returned image using the JUMBO image list is mainly empty
        // If the bottom half of the icon is empty (transparent), it is invalid

        let rect = Avalonia.PixelRect(0, bitmap.PixelSize.Height / 2, bitmap.PixelSize.Width, bitmap.PixelSize.Height / 2)
        let pixels = Array.zeroCreate<byte> (rect.Width * bitmap.Format.Value.BitsPerPixel / 8 * rect.Height)
        use pixelsPtr = fixed pixels
        bitmap.CopyPixels(rect, NativePtr.toNativeInt pixelsPtr, pixels.Length, rect.Width * bitmap.Format.Value.BitsPerPixel / 8)

        pixels |> Array.exists ((<>) 0uy)

    let getFileIcon desiredSize (filePath: string) =
        try
            voption {
                use! jumboIcon = getFileHIcon Shell32.SHIL.SHIL_JUMBO filePath

                match isValidIcon jumboIcon with
                | true ->
                    return jumboIcon.CreateScaledBitmap(desiredSize, Avalonia.Media.Imaging.BitmapInterpolationMode.HighQuality)
                | false -> return! getFileHIcon Shell32.SHIL.SHIL_EXTRALARGE filePath
            }
        with e ->
            ValueNone

    let getUrlFileIcon (file: string) =
        voption {
            let! lines =
                try File.ReadAllLines file |> ValueSome
                with _ -> ValueNone

            let! iconFileLine = lines |> Array.tryFind _.StartsWith("IconFile=")
            let! file =
                iconFileLine.Substring "IconFile=".Length
                |> ValueSome
                |> ValueOption.filter (String.IsNullOrWhiteSpace >> not)

            try return new Avalonia.Media.Imaging.Bitmap(file)
            with _ -> return! ValueNone
        }
