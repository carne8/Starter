using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming

namespace FileIconLoader;

[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
internal struct SHFILEINFO
{
    public IntPtr hIcon;
    public int iIcon;
    public uint dwAttributes;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
    public string szDisplayName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
    public string szTypeName;
}

internal static class PInvokeCustom
{
    [DllImport("shell32.dll", CharSet=CharSet.Auto)]
    public static extern IntPtr SHGetFileInfo(
        string pszPath,
        uint dwFileAttributes,
        ref SHFILEINFO psfi,
        uint cbFileInfo,
        uint uFlags
    );
}
