using System.Collections.Generic;

namespace AstralLite.Models;

/// <summary>
/// 进程监控配置，支持WFP自动添加和端口过滤
/// </summary>
public class ProcessMonitorConfiguration
{
    /// <summary>
    /// 分组名称（可选）
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 配置显示名
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 要监控的进程名（不含.exe）
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 规则生效需同时运行的多进程（全部运行才生效）
    /// </summary>
    public List<string> RequireAllProcesses { get; set; } = new();

    /// <summary>
    /// 规则生效需运行的多进程（任一运行即可）
    /// </summary>
    public List<string> RequireAnyProcesses { get; set; } = new();

    /// <summary>
    /// 排除条件：当列表中的任何进程运行时，该规则组不启用
    /// </summary>
    public List<string> ExcludeProcesses { get; set; } = new();

    /// <summary>
    /// 要自动添加的防火墙规则列表
    /// </summary>
    public List<PortRule> Rules { get; set; } = new();
}

/// <summary>
/// 端口规则，支持TCP/UDP、本地/远程、端口范围等
/// </summary>
public class PortRule
{
    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 动作（允许/阻止）
    /// </summary>
    public string Action { get; set; } = "block";

    /// <summary>
    /// 协议（tcp/udp）
    /// </summary>
    public string Protocol { get; set; } = "tcp";

    /// <summary>
    /// 方向（入站/出站/双向）
    /// </summary>
    public string Direction { get; set; } = "both";

    /// <summary>
    /// 端口号或范围，如 80 或 1000-2000
    /// </summary>
    public string Port { get; set; } = string.Empty;

    /// <summary>
    /// 本地地址（可选）
    /// </summary>
    public string? LocalAddress { get; set; }

    /// <summary>
    /// 远程地址（可选）
    /// </summary>
    public string? RemoteAddress { get; set; }

    /// <summary>
    /// 权重值，权重值越大，优先级越高
    /// </summary>
    public int Weight { get; set; } = 0;

    /// <summary>
    /// IP 版本：ipv4、ipv6、both（默认 both）
    /// </summary>
    public string IpVersion { get; set; } = "both";

    /// <summary>
    /// 是否排除 DNS 端口（53）
    /// </summary>
    public bool ExcludeDns { get; set; } = false;
}
