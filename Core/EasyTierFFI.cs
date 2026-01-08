using System.Runtime.InteropServices;

namespace AstralLite.Core;

/// <summary>
/// EasyTier µÄµ×²ã FFI °ó¶¨
/// </summary>
internal static class EasyTierFFI
{
    private const string DllName = "easytier_ffi.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int parse_config([MarshalAs(UnmanagedType.LPStr)] string cfgStr);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int run_network_instance([MarshalAs(UnmanagedType.LPStr)] string cfgStr);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int retain_network_instance(IntPtr instNames, int length);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int collect_network_infos(IntPtr infos, int maxLength);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void get_error_msg(out IntPtr errorMsg);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void free_string(IntPtr str);

    [StructLayout(LayoutKind.Sequential)]
    internal struct KeyValuePair
    {
        public IntPtr Key;
        public IntPtr Value;
    }

    internal static string GetErrorMessage()
    {
        get_error_msg(out IntPtr errorMsgPtr);
        if (errorMsgPtr == IntPtr.Zero)
        {
            return "Î´Öª´íÎó";
        }

        string? errorMsg = Marshal.PtrToStringAnsi(errorMsgPtr);
        free_string(errorMsgPtr);
        return errorMsg ?? "Î´Öª´íÎó";
    }
}
