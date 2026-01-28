using System.Collections.ObjectModel;
using System.Collections.Generic;
namespace AstralLite.Models;
/// <summary>
/// ????????
/// </summary>
public static class ProcessMonitorConfigurationList
{
    /// <summary>
    /// ????????????
    /// </summary>
    public static ObservableCollection<ProcessMonitorConfiguration> Processes { get; } = new()
    {
        new ProcessMonitorConfiguration
        {
            GroupName = "game",
            DisplayName = "Payday 2",
            ProcessName = "payday2_win32_release",
            LocalPortRules = new List<PortRule>(),
            RemotePortRules = new List<PortRule>
            {
                new PortRule { Protocol = "udp", Port = "3478", RemoteAddress = "0.0.0.0/0" }
            }
        },

        new ProcessMonitorConfiguration
        {
            GroupName = "game",
            DisplayName = "Steam",
            ProcessName = "steam",
            RequireAnyProcesses = new List<string>
            {
                "payday_win32_release",
                "payday2_win32_release"
            },
            LocalPortRules = new List<PortRule>(),
            RemotePortRules = new List<PortRule>
            {
                new PortRule { Protocol = "udp", Port = "3478", RemoteAddress = "0.0.0.0/0" },
                new PortRule { Protocol = "udp", Port = "4379-4380", RemoteAddress = "0.0.0.0/0" }
            }
        },

    };
    /// <summary>
    /// ?????????
    /// </summary>
    public static ProcessMonitorConfiguration? GetByDisplayName(string displayName)
    {
        return Processes.FirstOrDefault(p => p.DisplayName.Equals(displayName, System.StringComparison.OrdinalIgnoreCase));
    }
    /// <summary>
    /// ????????
    /// </summary>
    public static IEnumerable<string> GetAllGroups()
    {
        return Processes.Select(p => p.GroupName).Distinct();
    }
}