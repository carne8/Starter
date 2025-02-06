using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Win32;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;
using Avalonia.Media.Imaging;

namespace FileIconLoader;

[SupportedOSPlatform("windows6.0.6000")]
public class FileIconLoader
{
    private static readonly Guid IidIImageList = new("46EB5926-582E-4017-9FDF-E8998DAA0950");

    private static int LoadImageIndex(string filePath)
    {
        var psfi = new SHFILEINFO();
        _ = PInvokeCustom.SHGetFileInfo(
            filePath,
            (uint)FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
            ref psfi,
            (uint)Marshal.SizeOf<SHFILEINFO>(),
            (uint)SHGFI_FLAGS.SHGFI_SYSICONINDEX
        );

        return psfi.iIcon;
    }

    private static HIMAGELIST LoadImageList()
    {
        var hres = PInvoke.SHGetImageList(
            (int)PInvoke.SHIL_LARGE,
            in IidIImageList,
            out var imageList
        );
        if (hres.Failed || imageList is null)
            throw new Exception("Failed to load image list");

        var imageListPtr = Marshal.GetIUnknownForObject(imageList);
        try
        {
            return new HIMAGELIST(imageListPtr);
        }
        finally
        {
            Marshal.Release(imageListPtr);
        }
    }

    private static HICON LoadIcon(HIMAGELIST imageListHandle, int iconIndex)
    {
        var iconHandle = PInvoke.ImageList_GetIcon(
            imageListHandle,
            iconIndex,
            IMAGE_LIST_DRAW_STYLE.ILD_NORMAL
        );
        if (iconHandle.IsNull) throw new Exception($"Failed to load icon");
        return iconHandle;
    }

    public static Bitmap? LoadBitmap(string filePath)
    {
        try
        {
            var imageList = LoadImageList();
            var iconIndex = LoadImageIndex(filePath);
            var iconHandle = LoadIcon(imageList, iconIndex);
            var bitmap = iconHandle.ToAvaloniaBitmap();

            PInvoke.DestroyIcon(iconHandle);
            return bitmap;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
