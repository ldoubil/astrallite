namespace AstralLite.Models;

/// <summary>
/// 进程监听配置
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
}
