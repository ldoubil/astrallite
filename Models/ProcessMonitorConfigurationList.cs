using System.Collections.ObjectModel;

namespace AstralLite.Models;

/// <summary>
/// 进程监听配置列表
/// </summary>
public static class ProcessMonitorConfigurationList
{
    /// <summary>
    /// 预定义的进程监听配置列表
    /// </summary>
    public static ObservableCollection<ProcessMonitorConfiguration> Processes { get; } = new()
    {
        new ProcessMonitorConfiguration
        {
            GroupName = "浏览器",
            DisplayName = "Chrome浏览器",
            ProcessName = "chrome",
            LocalPortRules = new List<PortRule>
            {
                new PortRule { Protocol = "tcp", Port = "80" },
                new PortRule { Protocol = "udp", Port = "53" }
            },
            RemotePortRules = new List<PortRule>
            {
                new PortRule { Protocol = "tcp", Port = "443", RemoteAddress = "0.0.0.0/0" }
            }
        }
    };

    /// <summary>
    /// 根据显示名获取配置
    /// </summary>
    public static ProcessMonitorConfiguration? GetByDisplayName(string displayName)
    {
        return Processes.FirstOrDefault(p => p.DisplayName.Equals(displayName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取所有分组名称
    /// </summary>
    public static IEnumerable<string> GetAllGroups()
    {
        return Processes.Select(p => p.GroupName).Distinct();
    }
}
