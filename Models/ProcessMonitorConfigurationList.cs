using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace AstralLite.Models;

public static class ProcessMonitorConfigurationList
{
    public static ObservableCollection<ProcessMonitorConfiguration> Processes { get; } = new()
    {
        new ProcessMonitorConfiguration
        {
            GroupName = "1",
            DisplayName = "规则组1 - Payday 2",
            ProcessName = "payday2_win32_release",
            ExcludeProcesses = new List<string> { "payday_win32_release" },
            Rules = new List<PortRule>
            {
                new PortRule
                {
                    Name = "允许走AS",
                    Action = "allow",
                    Protocol = "udp",
                    Direction = "both",
                    RemoteAddress = "100.100.0.0/20",
                    IpVersion = "ipv4",
                    Weight = 2
                },
                new PortRule
                {
                    Name = "阻止中继",
                    Action = "block",
                    Protocol = "udp",
                    Direction = "both",
                    Port = "1024-65535",
                    IpVersion = "ipv4",
                    Weight = 1
                },
                new PortRule
                {
                    Name = "阻止中继-IPv6",
                    Action = "block",
                    Protocol = "udp",
                    Direction = "both",
                    Port = "1024-65535",
                    IpVersion = "ipv6",
                    Weight = 1
                }
            }
        },

        new ProcessMonitorConfiguration
        {
            GroupName = "2",
            DisplayName = "规则组2 - Steam",
            ProcessName = "steam",
            RequireAnyProcesses = new List<string>
            {
                "payday_win32_release",
                "payday2_win32_release"
            },
            Rules = new List<PortRule>
            {
                new PortRule
                {
                    Name = "允许走AS",
                    Action = "allow",
                    Protocol = "udp",
                    Direction = "both",
                    RemoteAddress = "100.100.0.0/20",
                    IpVersion = "ipv4",
                    Weight = 2
                },
                new PortRule
                {
                    Name = "阻止中继",
                    Action = "block",
                    Protocol = "udp",
                    Direction = "both",
                    Port = "1024-65535",
                    IpVersion = "ipv4",
                    Weight = 1
                },
                new PortRule
                {
                    Name = "阻止中继-IPv6",
                    Action = "block",
                    Protocol = "udp",
                    Direction = "both",
                    Port = "1024-65535",
                    IpVersion = "ipv6",
                    Weight = 1
                }
            }
        },
    };

    public static ProcessMonitorConfiguration? GetByDisplayName(string displayName)
    {
        return Processes.FirstOrDefault(p => p.DisplayName.Equals(displayName, System.StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<string> GetAllGroups()
    {
        return Processes.Select(p => p.GroupName).Distinct();
    }
}
