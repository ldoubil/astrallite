using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace AstralLite.Services;

internal static class NetworkInterfaceMetricService
{
    private const int NetshTimeoutMs = 3000;
    private static readonly object WatcherLock = new();
    private static readonly ConcurrentDictionary<string, ManagementEventWatcher> CreationWatchers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly TimeSpan CreationWatcherInterval = TimeSpan.FromSeconds(1);

    public static Task<bool> EnsureMetricZeroWithWatcherAsync(string interfaceName, int retryCount, TimeSpan retryDelay)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
        {
            return Task.FromResult(false);
        }

        StartCreationWatcher(interfaceName, retryCount, retryDelay);
        return TryEnsureMetricZeroWithRetryAsync(interfaceName, retryCount, retryDelay);
    }

    public static void StopAllWatchers()
    {
        lock (WatcherLock)
        {
            foreach (var watcher in CreationWatchers.Values)
            {
                try
                {
                    watcher.Stop();
                }
                catch
                {
                }

                watcher.Dispose();
            }

            CreationWatchers.Clear();
        }
    }

    public static bool TryEnsureMetricZero(string interfaceName)
    {
        if (string.IsNullOrWhiteSpace(interfaceName))
        {
            return false;
        }

        if (AreMetricsZero(interfaceName))
        {
            return true;
        }

        if (!TrySetMetricToZero(interfaceName))
        {
            return false;
        }

        return AreMetricsZero(interfaceName);
    }

    private static async Task<bool> TryEnsureMetricZeroWithRetryAsync(string interfaceName, int retryCount, TimeSpan retryDelay)
    {
        for (var attempt = 0; attempt < retryCount; attempt++)
        {
            if (TryEnsureMetricZero(interfaceName))
            {
                return true;
            }

            await Task.Delay(retryDelay);
        }

        return false;
    }

    private static void StartCreationWatcher(string interfaceName, int retryCount, TimeSpan retryDelay)
    {
        lock (WatcherLock)
        {
            if (CreationWatchers.ContainsKey(interfaceName))
            {
                return;
            }
        }

        var sanitizedName = interfaceName.Replace("'", "''");

        try
        {
            var condition =
                $"TargetInstance ISA 'Win32_NetworkAdapter' AND (TargetInstance.NetConnectionID = '{sanitizedName}' OR TargetInstance.Name = '{sanitizedName}')";
            var query = new WqlEventQuery("__InstanceOperationEvent", CreationWatcherInterval, condition);
            var watcher = new ManagementEventWatcher("root\\CIMV2", query.QueryString);
            watcher.EventArrived += async (_, __) =>
            {
                try
                {
                    var success = await TryEnsureMetricZeroWithRetryAsync(interfaceName, retryCount, retryDelay);
                    if (success)
                    {
                        Debug.WriteLine($"[NetworkInterfaceMetricService] Interface metric set to 0 after creation for {interfaceName}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NetworkInterfaceMetricService] Metric watcher failed for {interfaceName}: {ex.Message}");
                }
            };

            watcher.Start();

            lock (WatcherLock)
            {
                CreationWatchers[interfaceName] = watcher;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NetworkInterfaceMetricService] Failed to start creation watcher for {interfaceName}: {ex.Message}");
        }
    }

    private static bool AreMetricsZero(string interfaceName)
    {
        var ipv4Metric = GetInterfaceMetric(interfaceName, "ipv4");
        var ipv6Metric = GetInterfaceMetric(interfaceName, "ipv6");

        var hasMetric = ipv4Metric.HasValue || ipv6Metric.HasValue;

        return hasMetric
            && (!ipv4Metric.HasValue || ipv4Metric.Value == 0)
            && (!ipv6Metric.HasValue || ipv6Metric.Value == 0);
    }

    private static bool TrySetMetricToZero(string interfaceName)
    {
        var ipv4Ok = RunNetsh($"interface ipv4 set interface interface=\"{interfaceName}\" metric=0");
        var ipv6Ok = RunNetsh($"interface ipv6 set interface interface=\"{interfaceName}\" metric=0");
        return ipv4Ok || ipv6Ok;
    }

    private static int? GetInterfaceMetric(string interfaceName, string ipVersion)
    {
        var arguments = ipVersion.Equals("ipv6", StringComparison.OrdinalIgnoreCase)
            ? $"interface ipv6 show interface interface=\"{interfaceName}\""
            : $"interface ipv4 show interface interface=\"{interfaceName}\"";

        if (!RunNetshWithOutput(arguments, out var stdout, out _))
        {
            return null;
        }

        return ParseInterfaceMetric(stdout);
    }

    private static bool RunNetsh(string arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            if (!process.WaitForExit(NetshTimeoutMs))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
                return false;
            }

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                Debug.WriteLine($"[Netsh] {arguments} failed: {error}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Netsh] {arguments} failed: {ex.Message}");
            return false;
        }
    }

    private static bool RunNetshWithOutput(string arguments, out string stdout, out string stderr)
    {
        stdout = string.Empty;
        stderr = string.Empty;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return false;
            }

            if (!process.WaitForExit(NetshTimeoutMs))
            {
                try
                {
                    process.Kill();
                }
                catch
                {
                }
                return false;
            }

            stdout = process.StandardOutput.ReadToEnd();
            stderr = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0)
            {
                Debug.WriteLine($"[Netsh] {arguments} failed: {stderr}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Netsh] {arguments} failed: {ex.Message}");
            return false;
        }
    }

    private static int? ParseInterfaceMetric(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        foreach (var rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var parts = line.Split(':', 2);
            if (parts.Length < 2)
            {
                continue;
            }

            var label = parts[0].Trim();
            if (!label.Equals("Metric", StringComparison.OrdinalIgnoreCase)
                && !label.Equals("跃点数", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var value = parts[1].Trim();

            if (int.TryParse(value, out var metric))
            {
                return metric;
            }

            var digits = value.Where(char.IsDigit).ToArray();
            if (digits.Length > 0 && int.TryParse(new string(digits), out metric))
            {
                return metric;
            }
        }

        return null;
    }
}
