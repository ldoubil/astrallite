using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AstralLite.Models;
using ThreadingTimer = System.Threading.Timer;
namespace AstralLite.Services;
/// <summary>
/// Process monitor service that emits start/stop events for configured processes.
/// </summary>
public class ProcessMonitorService : IDisposable
{
    private readonly Dictionary<string, bool> _processStatus = new();
    private readonly Dictionary<string, FirewallRule> _firewallRules = new();
    private readonly object _firewallLock = new();
    private bool _enabled = true;
    private ThreadingTimer? _timer;
    private readonly int _intervalMs;
    private readonly IEnumerable<ProcessMonitorConfiguration> _configs;
    public event Action<ProcessMonitorConfiguration>? ProcessStarted;
    public event Action<ProcessMonitorConfiguration>? ProcessStopped;
    public ProcessMonitorService(IEnumerable<ProcessMonitorConfiguration> configs, int intervalMs = 1000)
    {
        _configs = configs;
        _intervalMs = intervalMs;
        foreach (var cfg in configs)
        {
            _processStatus[cfg.ProcessName] = false;
        }
        _timer = new ThreadingTimer(CheckProcesses, null, 0, _intervalMs);
        // Wire up handlers.
        this.ProcessStarted += OnProcessStarted;
        this.ProcessStopped += OnProcessStopped;
    }
    private void CheckProcesses(object? state)
    {
        foreach (var cfg in _configs)
        {
            bool isRunning = Process.GetProcessesByName(cfg.ProcessName).Any();

            if (!_enabled)
            {
                _processStatus[cfg.ProcessName] = isRunning;
                continue;
            }

            if (!_processStatus[cfg.ProcessName] && isRunning)
            {
                _processStatus[cfg.ProcessName] = true;
                ProcessStarted?.Invoke(cfg);
            }
            else if (_processStatus[cfg.ProcessName] && !isRunning)
            {
                _processStatus[cfg.ProcessName] = false;
                ProcessStopped?.Invoke(cfg);
            }
        }
    }
    private void OnProcessStarted(ProcessMonitorConfiguration cfg)
    {
        if (!_enabled)
        {
            return;
        }

        // Add firewall rules.
        foreach (var rule in cfg.LocalPortRules)
        {
            AddFirewallRule(cfg, rule, true);
        }
        foreach (var rule in cfg.RemotePortRules)
        {
            AddFirewallRule(cfg, rule, false);
        }
    }
    private void OnProcessStopped(ProcessMonitorConfiguration cfg)
    {
        if (!_enabled)
        {
            return;
        }

        // Remove firewall rules.
        foreach (var rule in cfg.LocalPortRules)
        {
            RemoveFirewallRule(cfg, rule, true);
        }
        foreach (var rule in cfg.RemotePortRules)
        {
            RemoveFirewallRule(cfg, rule, false);
        }
    }
    private void AddFirewallRule(ProcessMonitorConfiguration cfg, PortRule rule, bool isLocal)
    {
        var name = $"WFP_{cfg.ProcessName}_{rule.Protocol}_{rule.Port}_{(isLocal ? "local" : "remote")}";
        var applicationPath = ResolveApplicationPath(cfg);
        var (ports, isAnyPort) = ParsePorts(rule.Port);
        try
        {
            if (ports.Count == 0 && !isAnyPort)
            {
                Debug.WriteLine($"[WFP] Port parsing failed: {name}");
                return;
            }
            var firewall = FirewallRule.CreateBlockRule(
                name,
                applicationPath,
                rule.Protocol,
                ports,
                isAnyPort,
                isLocal,
                rule.RemoteAddress);
            lock (_firewallLock)
            {
                if (_firewallRules.TryGetValue(name, out var existing))
                {
                    existing.Dispose();
                }
                _firewallRules[name] = firewall;
            }
            Debug.WriteLine($"[WFP] Rule added: {name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WFP] Rule add failed: {name} - {ex.Message}");
        }
    }
    private void RemoveFirewallRule(ProcessMonitorConfiguration cfg, PortRule rule, bool isLocal)
    {
        var name = $"WFP_{cfg.ProcessName}_{rule.Protocol}_{rule.Port}_{(isLocal ? "local" : "remote")}";
        try
        {
            FirewallRule? firewall = null;
            lock (_firewallLock)
            {
                if (_firewallRules.TryGetValue(name, out var existing))
                {
                    firewall = existing;
                    _firewallRules.Remove(name);
                }
            }
            firewall?.Dispose();
            Debug.WriteLine($"[WFP] Rule removed: {name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WFP] Rule remove failed: {name} - {ex.Message}");
        }
    }
    public void SetEnabled(bool enabled)
    {
        if (_enabled == enabled)
        {
            return;
        }

        _enabled = enabled;

        if (!enabled)
        {
            ClearFirewallRules();
            return;
        }

        ApplyRulesForRunningProcesses();
    }
    private void ApplyRulesForRunningProcesses()
    {
        foreach (var cfg in _configs)
        {
            var isRunning = Process.GetProcessesByName(cfg.ProcessName).Any();
            _processStatus[cfg.ProcessName] = isRunning;
            if (isRunning)
            {
                OnProcessStarted(cfg);
            }
        }
    }
    private void ClearFirewallRules()
    {
        lock (_firewallLock)
        {
            foreach (var firewall in _firewallRules.Values)
            {
                firewall.Dispose();
            }
            _firewallRules.Clear();
        }
    }
    private static (IReadOnlyCollection<ushort> Ports, bool IsAny) ParsePorts(string portText)
    {
        if (string.IsNullOrWhiteSpace(portText))
        {
            return (Array.Empty<ushort>(), false);
        }
        var trimmed = portText.Trim();
        if (trimmed == "*" || trimmed.Equals("any", StringComparison.OrdinalIgnoreCase))
        {
            return (Array.Empty<ushort>(), true);
        }
        var ports = new HashSet<ushort>();
        var segments = portText.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in segments)
        {
            var part = raw.Trim();
            if (part.Length == 0)
            {
                continue;
            }
            var dashIndex = part.IndexOf('-');
            if (dashIndex > 0 && dashIndex < part.Length - 1)
            {
                if (int.TryParse(part[..dashIndex], out var start) &&
                    int.TryParse(part[(dashIndex + 1)..], out var end))
                {
                    if (start > end)
                    {
                        (start, end) = (end, start);
                    }
                    if (start <= 1 && end >= ushort.MaxValue)
                    {
                        return (Array.Empty<ushort>(), true);
                    }
                    for (var port = start; port <= end; port++)
                    {
                        if (port is > 0 and <= ushort.MaxValue)
                        {
                            ports.Add((ushort)port);
                        }
                    }
                }
            }
            else if (int.TryParse(part, out var single) && single is > 0 and <= ushort.MaxValue)
            {
                ports.Add((ushort)single);
            }
        }
        if (ports.Count == ushort.MaxValue)
        {
            return (Array.Empty<ushort>(), true);
        }
        return (ports.ToArray(), false);
    }
    private static string ResolveApplicationPath(ProcessMonitorConfiguration cfg)
    {
        try
        {
            var process = Process.GetProcessesByName(cfg.ProcessName).FirstOrDefault();
            var path = process?.MainModule?.FileName;
            if (!string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[WFP] Failed to resolve process path: {cfg.ProcessName} - {ex.Message}");
        }
        return $"{cfg.ProcessName}.exe";
    }
    public void Dispose()
    {
        _timer?.Dispose();
        ClearFirewallRules();
    }
}
