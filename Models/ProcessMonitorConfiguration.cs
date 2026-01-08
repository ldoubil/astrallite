using System.Collections.Generic;

namespace AstralLite.Models;

/// <summary>
/// 进程监听配置，支持WFP自动管理和端口规则
/// </summary>
public class ProcessMonitorConfiguration
{
    /// <summary>
    /// 分组名（可选）
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// 配置显示名
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 要监听的进程名（不带.exe）
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 需要自动管理的本地端口规则
    /// </summary>
    public List<PortRule> LocalPortRules { get; set; } = new();

    /// <summary>
    /// 需要自动管理的远程端口规则
    /// </summary>
    public List<PortRule> RemotePortRules { get; set; } = new();
}

/// <summary>
/// 端口规则（支持TCP/UDP、本地/远程、端口范围等）
/// </summary>
public class PortRule
{
    /// <summary>
    /// 协议（tcp/udp）
    /// </summary>
    public string Protocol { get; set; } = "tcp";

    /// <summary>
    /// 端口号或范围（如 80 或 1000-2000）
    /// </summary>
    public string Port { get; set; } = string.Empty;

    /// <summary>
    /// 远程地址（可选）
    /// </summary>
    public string? RemoteAddress { get; set; }
}
