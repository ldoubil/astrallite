namespace AstralLite.Models;

/// <summary>
/// 房间配置信息
/// </summary>
public class RoomConfiguration
{
    /// <summary>
    /// 分组名称
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 房间名称
    /// </summary>
    public string RoomName { get; set; } = string.Empty;

    /// <summary>
    /// 房间测试 IP 地址
    /// </summary>
    public string TestIp { get; set; } = string.Empty;

    /// <summary>
    /// 房间服务器配置字符串（TOML 格式）
    /// </summary>
    public string ServerConfig { get; set; } = string.Empty;
}
