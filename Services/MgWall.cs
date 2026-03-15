using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AstralLite.Services;

public sealed class MgWall : IDisposable
{
    private const string DllPath = "mg_wall.dll";
    private bool _disposed;
    private bool _started;

    public const int Success = 0;
    public const int ErrNotStarted = -1;
    public const int ErrRuleExists = -2;
    public const int ErrRuleNotFound = -3;
    public const int ErrInvalidParam = -4;
    public const int ErrEngine = -10;

    [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mg_start();

    [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mg_stop();

    [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mg_add_rule(
        IntPtr id,
        IntPtr name,
        int enabled,
        IntPtr action,
        IntPtr protocol,
        IntPtr direction,
        IntPtr ipVersion,
        IntPtr appPath,
        IntPtr remoteIp,
        IntPtr localIp,
        IntPtr remotePort,
        IntPtr localPort,
        byte weight);

    [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mg_remove_rule(IntPtr id);

    [DllImport(DllPath, CallingConvention = CallingConvention.Cdecl)]
    private static extern int mg_get_status(
        out int isRunning,
        out int activeRules,
        out int totalRules);

    private static IntPtr ToUtf8Ptr(string? s)
    {
        if (string.IsNullOrEmpty(s)) return IntPtr.Zero;
        var bytes = System.Text.Encoding.UTF8.GetBytes(s + "\0");
        var ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        return ptr;
    }

    private static void FreeUtf8Ptr(IntPtr ptr)
    {
        if (ptr != IntPtr.Zero) Marshal.FreeHGlobal(ptr);
    }

    public void Start()
    {
        if (_started) return;
        
        var result = mg_start();
        if (result != Success)
        {
            throw new InvalidOperationException($"mg_start failed: {result}");
        }
        _started = true;
    }

    public void Stop()
    {
        if (!_started) return;
        
        mg_stop();
        _started = false;
    }

    public void AddRule(MgWallRule rule)
    {
        EnsureStarted();

        System.Diagnostics.Debug.WriteLine($"[MgWall] AddRule: id={rule.Id}, action={rule.Action}, protocol={rule.Protocol}, direction={rule.Direction}, ipVersion={rule.IpVersion}");
        System.Diagnostics.Debug.WriteLine($"[MgWall] appPath={rule.AppPath ?? ""}, remoteIp={rule.RemoteIp ?? ""}, remotePort={rule.RemotePort ?? ""}");

        var ptrs = new[]
        {
            ToUtf8Ptr(rule.Id),
            ToUtf8Ptr(rule.Name),
            ToUtf8Ptr(rule.Action),
            ToUtf8Ptr(rule.Protocol),
            ToUtf8Ptr(rule.Direction),
            ToUtf8Ptr(rule.IpVersion),
            ToUtf8Ptr(rule.AppPath),
            ToUtf8Ptr(rule.RemoteIp),
            ToUtf8Ptr(rule.LocalIp),
            ToUtf8Ptr(rule.RemotePort),
            ToUtf8Ptr(rule.LocalPort)
        };

        try
        {
            var result = mg_add_rule(
                ptrs[0], ptrs[1],
                rule.Enabled ? 1 : 0,
                ptrs[2], ptrs[3], ptrs[4], ptrs[5],
                ptrs[6], ptrs[7], ptrs[8],
                ptrs[9], ptrs[10],
                rule.Weight);

            if (result == ErrRuleExists)
            {
                throw new InvalidOperationException($"Rule already exists: {rule.Id}");
            }
            
            if (result != Success)
            {
                throw new InvalidOperationException($"mg_add_rule failed: {result}");
            }
        }
        finally
        {
            foreach (var ptr in ptrs) FreeUtf8Ptr(ptr);
        }
    }

    public void RemoveRule(string id)
    {
        EnsureStarted();

        var ptr = ToUtf8Ptr(id);
        try
        {
            var result = mg_remove_rule(ptr);
            if (result == ErrRuleNotFound)
            {
                return;
            }
            
            if (result != Success)
            {
                throw new InvalidOperationException($"mg_remove_rule failed: {result}");
            }
        }
        finally
        {
            FreeUtf8Ptr(ptr);
        }
    }

    public MgWallStatus GetStatus()
    {
        mg_get_status(out var isRunning, out var activeRules, out var totalRules);
        return new MgWallStatus
        {
            IsRunning = isRunning != 0,
            ActiveRules = activeRules,
            TotalRules = totalRules
        };
    }

    private void EnsureStarted()
    {
        if (!_started)
        {
            throw new InvalidOperationException("MgWall not started");
        }
    }

    private static string GetNtPath(string dosPath)
    {
        var path = Path.GetFullPath(dosPath);
        var driveLetter = path[..2];

        var devicePath = QueryDosDevice(driveLetter);
        if (string.IsNullOrEmpty(devicePath))
        {
            return dosPath;
        }

        var remainingPath = path[2..];
        var cleanedDevicePath = devicePath.TrimEnd('\\').Replace("\\??\\", "");

        var finalPath = cleanedDevicePath.EndsWith('\\') && remainingPath.StartsWith('\\')
            ? $"{cleanedDevicePath}{remainingPath[1..]}"
            : $"{cleanedDevicePath}{remainingPath}";

        return finalPath;
    }

    private static string QueryDosDevice(string deviceName)
    {
        var buffer = new StringBuilder(1024);
        var result = NativeMethods.QueryDosDeviceW(deviceName, buffer, buffer.Capacity);
        return result > 0 ? buffer.ToString() : "";
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int QueryDosDeviceW(
            string lpDeviceName,
            StringBuilder lpTargetPath,
            int ucchMax);
    }
}

public class MgWallRule
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string Action { get; set; } = "block";
    public string Protocol { get; set; } = "both";
    public string Direction { get; set; } = "outbound";
    public string IpVersion { get; set; } = "both";
    public string? AppPath { get; set; }
    public string? RemoteIp { get; set; }
    public string? LocalIp { get; set; }
    public string? RemotePort { get; set; }
    public string? LocalPort { get; set; }
    public byte Weight { get; set; } = 15;
}

public class MgWallStatus
{
    public bool IsRunning { get; set; }
    public int ActiveRules { get; set; }
    public int TotalRules { get; set; }
}
