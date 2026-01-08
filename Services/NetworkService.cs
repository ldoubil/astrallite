using AstralLite.Core;
using AstralLite.Models.Network;
using System.Diagnostics;

namespace AstralLite.Services;

/// <summary>
/// 网络连接管理服务（单例）
/// </summary>
public class NetworkService
{
    private static readonly Lazy<NetworkService> _instance = new(() => new NetworkService());
    private const string GlobalInstanceName = "astrallite-global";
    
    private System.Threading.Timer? _networkMonitor;
    private bool _isConnected;
    private string? _currentRoomConfig;

    private NetworkService()
    {
    }

    public static NetworkService Instance => _instance.Value;

    /// <summary>
    /// 是否已连接
    /// </summary>
    public bool IsConnected => _isConnected;

    /// <summary>
    /// 网络信息更新事件（原始字典格式）
    /// </summary>
    public event EventHandler<Dictionary<string, string>>? NetworkInfoUpdated;

    /// <summary>
    /// 解析后的网络信息更新事件
    /// </summary>
    public event EventHandler<Dictionary<string, NetworkInfo>>? ParsedNetworkInfoUpdated;

    /// <summary>
    /// 连接到房间
    /// </summary>
    /// <param name="serverConfig">房间配置</param>
    public void Connect(string serverConfig)
    {
        // 如果已经连接，先断开
        if (_isConnected)
        {
            Debug.WriteLine("[NetworkService] Already connected, disconnecting first...");
            Disconnect();
            System.Threading.Thread.Sleep(500); // 等待清理完成
        }

        try
        {
            // 确保使用全局唯一的 instance_name
            var config = EnsureGlobalInstanceName(serverConfig);
            
            Debug.WriteLine($"[NetworkService] Starting network with config:\n{config}");
            AstralNat.StartNetwork(config);
            
            _isConnected = true;
            _currentRoomConfig = config;

            // 启动网络信息监控（每秒更新一次）
            StartNetworkMonitor();
            
            Debug.WriteLine("[NetworkService] Connected successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NetworkService] Connect failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 断开连接
    /// </summary>
    public void Disconnect()
    {
        if (!_isConnected)
        {
            return;
        }

        try
        {
            // 停止监控
            StopNetworkMonitor();

            // 停止所有网络实例
            Debug.WriteLine("[NetworkService] Disconnecting...");
            AstralNat.StopAllNetworks();
            
            _isConnected = false;
            _currentRoomConfig = null;
            
            Debug.WriteLine("[NetworkService] Disconnected successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NetworkService] Disconnect failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 启动网络信息监控
    /// </summary>
    private void StartNetworkMonitor()
    {
        StopNetworkMonitor(); // 确保没有重复的监控

        _networkMonitor = new System.Threading.Timer(_ =>
        {
            if (!_isConnected)
            {
                return;
            }

            try
            {
                var infoDict = AstralNat.GetNetworkInfo();
                
                // 触发原始字典事件
                NetworkInfoUpdated?.Invoke(this, infoDict);

                // 解析并触发解析后的事件
                if (infoDict.Count > 0)
                {
                    var parsedInfo = NetworkInfoParser.ParseNetworkInfoDict(infoDict);
                    ParsedNetworkInfoUpdated?.Invoke(this, parsedInfo);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NetworkService] Failed to get network info: {ex.Message}");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        Debug.WriteLine("[NetworkService] Network monitor started");
    }

    /// <summary>
    /// 停止网络信息监控
    /// </summary>
    private void StopNetworkMonitor()
    {
        if (_networkMonitor != null)
        {
            _networkMonitor.Dispose();
            _networkMonitor = null;
            Debug.WriteLine("[NetworkService] Network monitor stopped");
        }
    }

    /// <summary>
    /// 确保配置使用全局唯一的 instance_name
    /// </summary>
    private string EnsureGlobalInstanceName(string config)
    {
        var lines = config.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new System.Text.StringBuilder();
        bool instanceNameFound = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("instance_name", StringComparison.OrdinalIgnoreCase))
            {
                // 替换为全局实例名
                result.AppendLine($"instance_name = \"{GlobalInstanceName}\"");
                instanceNameFound = true;
            }
            else
            {
                result.AppendLine(line);
            }
        }

        // 如果配置中没有 instance_name，在开头添加
        if (!instanceNameFound)
        {
            return $"instance_name = \"{GlobalInstanceName}\"\n{config}";
        }

        return result.ToString();
    }

    /// <summary>
    /// 立即获取当前网络信息（原始字典）
    /// </summary>
    public Dictionary<string, string>? GetCurrentNetworkInfo()
    {
        if (!_isConnected)
        {
            return null;
        }

        try
        {
            return AstralNat.GetNetworkInfo();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NetworkService] Failed to get network info: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 立即获取解析后的网络信息
    /// </summary>
    public Dictionary<string, NetworkInfo>? GetParsedNetworkInfo()
    {
        var rawInfo = GetCurrentNetworkInfo();
        if (rawInfo == null || rawInfo.Count == 0)
        {
            return null;
        }

        try
        {
            return NetworkInfoParser.ParseNetworkInfoDict(rawInfo);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NetworkService] Failed to parse network info: {ex.Message}");
            return null;
        }
    }
}
