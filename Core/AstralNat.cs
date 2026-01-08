using System.Runtime.InteropServices;
using System.Text;

namespace AstralLite.Core;

/// <summary>
/// 用于管理 EasyTier 网络实例的高级静态类
/// </summary>
public static class AstralNat
{
    private static readonly object _lock = new object();
    private static readonly List<string> _activeInstances = new List<string>();


    /// <summary>
    /// 使用指定的配置启动新的网络实例
    /// </summary>
    /// <param name="config">TOML 格式的配置字符串</param>
    /// <param name="instanceName">可选的实例名称（如果为空则从配置中提取）</param>
    /// <exception cref="ArgumentException">配置为空</exception>
    /// <exception cref="InvalidOperationException">启动实例失败</exception>
    public static void StartNetwork(string config, string? instanceName = null)
    {
        if (string.IsNullOrWhiteSpace(config))
        {
            throw new ArgumentException("配置不能为空", nameof(config));
        }

        lock (_lock)
        {
            int result = EasyTierFFI.run_network_instance(config);
            if (result < 0)
            {
                throw new InvalidOperationException($"启动网络失败: {EasyTierFFI.GetErrorMessage()}");
            }

            // 跟踪实例
            if (!string.IsNullOrEmpty(instanceName))
            {
                if (!_activeInstances.Contains(instanceName))
                {
                    _activeInstances.Add(instanceName);
                }
            }
            else
            {
                // 尝试从配置中提取 instance_name
                var extractedName = ExtractInstanceName(config);
                if (!string.IsNullOrEmpty(extractedName) && !_activeInstances.Contains(extractedName))
                {
                    _activeInstances.Add(extractedName);
                }
            }
        }
    }

    /// <summary>
    /// 停止指定的网络实例
    /// </summary>
    /// <param name="instanceName">要停止的实例名称</param>
    public static void StopNetwork(string instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
        {
            throw new ArgumentException("实例名称不能为空", nameof(instanceName));
        }

        lock (_lock)
        {
            var instancesToKeep = _activeInstances.Where(n => n != instanceName).ToArray();
            RetainInstances(instancesToKeep);
            _activeInstances.Remove(instanceName);
        }
    }

    /// <summary>
    /// 停止所有网络实例
    /// </summary>
    public static void StopAllNetworks()
    {
        lock (_lock)
        {
            RetainInstances(Array.Empty<string>());
            _activeInstances.Clear();
        }
    }

    /// <summary>
    /// 获取所有活动网络实例的信息
    /// </summary>
    /// <param name="maxResults">返回的最大结果数</param>
    /// <returns>包含网络信息的字典</returns>
    public static Dictionary<string, string> GetNetworkInfo(int maxResults = 100)
    {
        IntPtr buffer = Marshal.AllocHGlobal(Marshal.SizeOf<EasyTierFFI.KeyValuePair>() * maxResults);
        try
        {
            int count = EasyTierFFI.collect_network_infos(buffer, maxResults);
            if (count < 0)
            {
                throw new InvalidOperationException($"收集网络信息失败: {EasyTierFFI.GetErrorMessage()}");
            }

            var result = new Dictionary<string, string>();
            for (int i = 0; i < count; i++)
            {
                var kv = Marshal.PtrToStructure<EasyTierFFI.KeyValuePair>(
                    buffer + i * Marshal.SizeOf<EasyTierFFI.KeyValuePair>());
                
                string? key = Marshal.PtrToStringAnsi(kv.Key);
                string? value = Marshal.PtrToStringAnsi(kv.Value);

                EasyTierFFI.free_string(kv.Key);
                EasyTierFFI.free_string(kv.Value);

                if (!string.IsNullOrEmpty(key))
                {
                    result[key] = value ?? string.Empty;
                }
            }

            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>
    /// 获取当前跟踪的活动实例名称列表
    /// </summary>
    public static IReadOnlyList<string> GetActiveInstances()
    {
        lock (_lock)
        {
            return _activeInstances.ToList().AsReadOnly();
        }
    }


    /// <summary>
    /// 检查 EasyTier DLL 是否可用
    /// </summary>
    /// <returns>如果所有必需的 DLL 都存在则返回 true</returns>
    public static bool CheckDllsAvailable()
    {
        try
        {
            // 尝试调用一个简单的函数来验证 DLL 是否已加载
            EasyTierFFI.parse_config("");
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch
        {
            // 其他错误意味着 DLL 至少是可加载的
            return true;
        }
    }

    #region 私有方法

    private static void RetainInstances(string[] instanceNames)
    {
        IntPtr[]? namePointers = null;
        IntPtr namesPtr = IntPtr.Zero;

        try
        {
            if (instanceNames != null && instanceNames.Length > 0)
            {
                namePointers = new IntPtr[instanceNames.Length];
                for (int i = 0; i < instanceNames.Length; i++)
                {
                    if (string.IsNullOrEmpty(instanceNames[i]))
                    {
                        throw new ArgumentException("实例名称不能为空");
                    }
                    namePointers[i] = Marshal.StringToHGlobalAnsi(instanceNames[i]);
                }

                namesPtr = Marshal.AllocHGlobal(Marshal.SizeOf<IntPtr>() * namePointers.Length);
                Marshal.Copy(namePointers, 0, namesPtr, namePointers.Length);
            }

            int result = EasyTierFFI.retain_network_instance(namesPtr, instanceNames?.Length ?? 0);
            if (result < 0)
            {
                throw new InvalidOperationException($"保留实例失败: {EasyTierFFI.GetErrorMessage()}");
            }
        }
        finally
        {
            if (namePointers != null)
            {
                foreach (var ptr in namePointers)
                {
                    if (ptr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }

            if (namesPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(namesPtr);
            }
        }
    }

    private static string? ExtractInstanceName(string config)
    {
        // 简单的解析器，用于从 TOML 配置中提取 instance_name
        var lines = config.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("instance_name", StringComparison.OrdinalIgnoreCase))
            {
                var parts = trimmed.Split('=');
                if (parts.Length == 2)
                {
                    var value = parts[1].Trim().Trim('"', '\'');
                    return value;
                }
            }
        }
        return null;
    }

    #endregion
}
