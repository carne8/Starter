using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace FileIconLoader;

internal static class IconExtensions
{
    /// <summary>
    /// A helper to convert an icon to an Avalonia bitmap without using an intermediary bitmap.
    /// </summary>
    [SupportedOSPlatform("windows5.0")]
    public static unsafe Bitmap ToAvaloniaBitmap(this HICON icon)
    {
        ICONINFO iconInfo;
        if (!PInvoke.GetIconInfo(icon, &iconInfo))
            throw new InvalidOperationException("Failed to get icon info.");

        try
        {
            var dib = iconInfo.hbmColor;
            if (dib == IntPtr.Zero) throw new InvalidOperationException("Invalid SIB handle.");

            // Get bitmap details
            BITMAP bmp;
            if (PInvoke.GetObject(dib, Marshal.SizeOf<BITMAP>(), &bmp) == 0)
                throw new InvalidOperationException("Failed to get bitmap details.");

            var size = new Avalonia.PixelSize(bmp.bmWidth, bmp.bmHeight);
            var dpi = new Avalonia.Vector(96, 96);
            var stride = bmp.bmWidthBytes;

            var hdc = PInvoke.GetDC((HWND)IntPtr.Zero);
            try
            {
                var bmi = new BITMAPINFO
                {
                    bmiHeader = new BITMAPINFOHEADER
                    {
                        biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                        biWidth = size.Width,
                        biHeight = -size.Height, // Negative height for top-down DIB
                        biPlanes = 1,
                        biBitCount = bmp.bmBitsPixel,
                        biCompression = 0 // BI_RGB
                    }
                };

                var unmanagedData = Marshal.AllocHGlobal(bmp.bmHeight * stride);
                try
                {
                    if (PInvoke.GetDIBits(hdc, dib, 0, (uint)size.Height, unmanagedData.ToPointer(), &bmi,
                            DIB_USAGE.DIB_RGB_COLORS) == 0)
                        throw new InvalidOperationException("Failed to get DIB pixel data.");
                    return new Bitmap(PixelFormat.Bgra8888, AlphaFormat.Premul, unmanagedData, size, dpi, stride);
                }
                finally
                {
                    Marshal.FreeHGlobal(unmanagedData);
                }
            }
            finally
            {
                PInvoke.ReleaseDC((HWND)IntPtr.Zero, hdc);
            }
        }
        finally
        {
            if (iconInfo.hbmMask != IntPtr.Zero) PInvoke.DeleteObject(iconInfo.hbmMask);
            if (iconInfo.hbmColor != IntPtr.Zero) PInvoke.DeleteObject(iconInfo.hbmColor);
        }
    }
}
