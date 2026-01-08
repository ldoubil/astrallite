using System.Text.Json;
using AstralLite.Models.Network;
using System.Text.RegularExpressions;

namespace AstralLite.Services;

/// <summary>
/// 网络信息解析服务
/// </summary>
public static class NetworkInfoParser
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    /// <summary>
    /// 解析网络信息字典
    /// </summary>
    /// <param name="networkInfoDict">从 AstralNat.GetNetworkInfo() 获取的字典</param>
    /// <returns>解析后的网络信息对象字典</returns>
    public static Dictionary<string, NetworkInfo> ParseNetworkInfoDict(Dictionary<string, string> networkInfoDict)
    {
        var result = new Dictionary<string, NetworkInfo>();

        foreach (var (networkName, jsonString) in networkInfoDict)
        {
            try
            {
                var networkInfo = ParseNetworkInfo(jsonString);
                if (networkInfo != null)
                {
                    result[networkName] = networkInfo;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[NetworkInfoParser] Failed to parse network '{networkName}': {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[NetworkInfoParser] JSON that failed:\n{jsonString}");
            }
        }

        return result;
    }

    /// <summary>
    /// 解析单个网络信息 JSON
    /// </summary>
    /// <param name="json">JSON 字符串</param>
    /// <returns>网络信息对象</returns>
    public static NetworkInfo? ParseNetworkInfo(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            // 预处理：移除 events 数组内容
            var cleanedJson = RemoveEventsArray(json);
            
            return JsonSerializer.Deserialize<NetworkInfo>(cleanedJson, JsonOptions);
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NetworkInfoParser] JSON parse error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[NetworkInfoParser] Full JSON that caused error:");
            System.Diagnostics.Debug.WriteLine(json);
            return null;
        }
    }

    /// <summary>
    /// 暴力移除 events 数组内容，替换为空数组
    /// </summary>
    private static string RemoveEventsArray(string json)
    {
        // 使用正则表达式找到 "events":[...] 并替换为 "events":[]
        var pattern = "\"events\"\\s*:\\s*\\[(?:[^\\[\\]]|\\[(?:[^\\[\\]]|\\[[^\\]]*\\])*\\])*\\]";
        var replacement = "\"events\":[]";
        
        return Regex.Replace(json, pattern, replacement, RegexOptions.Singleline);
    }

    /// <summary>
    /// 获取格式化的网络状态摘要
    /// </summary>
    public static string GetNetworkSummary(NetworkInfo networkInfo)
    {
        var summary = new System.Text.StringBuilder();

        summary.AppendLine($"设备: {networkInfo.DeviceName}");
        summary.AppendLine($"状态: {(networkInfo.Running ? "运行中" : "已停止")}");

        if (networkInfo.MyNodeInfo != null)
        {
            var node = networkInfo.MyNodeInfo;
            summary.AppendLine($"主机名: {node.Hostname}");
            summary.AppendLine($"版本: {node.Version}");

            if (node.Ips?.InterfaceIpv4s.Count > 0)
            {
                var ip = node.Ips.InterfaceIpv4s[0].ToIpString();
                summary.AppendLine($"本地IP: {ip}");
            }

            if (node.StunInfo?.PublicIp.Count > 0)
            {
                summary.AppendLine($"公网IP: {string.Join(", ", node.StunInfo.PublicIp)}");
            }
        }

        summary.AppendLine($"对等节点: {networkInfo.Peers.Count} 个");
        summary.AppendLine($"路由: {networkInfo.Routes.Count} 条");

        if (networkInfo.Peers.Count > 0)
        {
            foreach (var peer in networkInfo.Peers)
            {
                foreach (var conn in peer.Connections)
                {
                    if (conn.Stats != null && !conn.IsClosed)
                    {
                        summary.AppendLine($"  连接延迟: {conn.Stats.LatencyMs:F1}ms");
                    }
                }
            }
        }

        return summary.ToString();
    }

    /// <summary>
    /// 获取详细的对等节点信息
    /// </summary>
    public static List<string> GetPeerDetails(NetworkInfo networkInfo)
    {
        var details = new List<string>();

        foreach (var pair in networkInfo.PeerRoutePairs)
        {
            if (pair.Route != null)
            {
                var info = $"{pair.Route.Hostname} (ID: {pair.Route.PeerId})";
                if (pair.Route.PathLatency > 0)
                {
                    info += $" - 延迟: {pair.Route.PathLatency}ms";
                }
                details.Add(info);
            }
        }

        return details;
    }

    /// <summary>
    /// 获取连接统计信息
    /// </summary>
    public static string GetConnectionStats(NetworkInfo networkInfo)
    {
        var stats = new System.Text.StringBuilder();
        long totalRx = 0;
        long totalTx = 0;

        foreach (var peer in networkInfo.Peers)
        {
            foreach (var conn in peer.Connections)
            {
                if (conn.Stats != null)
                {
                    totalRx += conn.Stats.RxBytes;
                    totalTx += conn.Stats.TxBytes;
                }
            }
        }

        stats.AppendLine($"总接收: {FormatBytes(totalRx)}");
        stats.AppendLine($"总发送: {FormatBytes(totalTx)}");

        return stats.ToString();
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
