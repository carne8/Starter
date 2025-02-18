module Starter.ApplicationSearchEngine.IconHelper

open System
open System.IO
open System.Runtime.InteropServices
open Vanara.PInvoke

type Gdi32.SafeHBITMAP with
    /// Warning: this method does not dispose the current HBITMAP
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
                    Avalonia.Platform.AlphaFormat.Premul,
                    unmanagedData, size, dpi, stride
                )
        finally
            Marshal.FreeHGlobal unmanagedData

type User32.SafeHICON with
    member this.ToAvaloniaBitmap() =
        use hBitmap = this.ToHBITMAP()
        hBitmap.ToAvaloniaBitmap()

module IconHelper =
    let getFileIcon (filePath: string) =
        let imageList = Shell32.SHGetImageList Shell32.SHIL.SHIL_EXTRALARGE
        let mutable fileInfo = Shell32.SHFILEINFO()

        let res = Shell32.SHGetFileInfo(
            filePath,
            FileAttributes.None,
            &fileInfo,
            sizeof<Shell32.SHFILEINFO>,
            Shell32.SHGFI.SHGFI_SYSICONINDEX
        )

        if res = IntPtr.Zero then
            failwith "Failed to retrieve icon info"

        match fileInfo.iIcon with
        | 0 | 2 -> None // Avoid default icons
        | _ ->
            use hIcon = imageList.GetIcon(
                fileInfo.iIcon,
                ComCtl32.IMAGELISTDRAWFLAGS.ILD_NORMAL
            )

            match hIcon.IsNull with
            | true -> None
            | false -> hIcon.ToAvaloniaBitmap() |> Some
