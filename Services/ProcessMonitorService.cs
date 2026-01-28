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
    private readonly Dictionary<string, bool> _processStatus = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FirewallRule> _firewallRules = new();
    private readonly Dictionary<ProcessMonitorConfiguration, bool> _rulesApplied = new();
    private readonly HashSet<string> _trackedProcessNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _firewallLock = new();
    private bool _enabled = true;
    private ThreadingTimer? _timer;
    private readonly int _intervalMs;
    private readonly IReadOnlyList<ProcessMonitorConfiguration> _configs;
    public event Action<ProcessMonitorConfiguration>? ProcessStarted;
    public event Action<ProcessMonitorConfiguration>? ProcessStopped;
    public ProcessMonitorService(IEnumerable<ProcessMonitorConfiguration> configs, int intervalMs = 1000)
    {
        _configs = configs.ToList();
        _intervalMs = intervalMs;
        foreach (var cfg in _configs)
        {
            NormalizeConfig(cfg);
        }
        foreach (var name in BuildTrackedProcessNames(_configs))
        {
            _trackedProcessNames.Add(name);
            _processStatus[name] = false;
        }
        foreach (var cfg in _configs)
        {
            _rulesApplied[cfg] = false;
        }
        _timer = new ThreadingTimer(CheckProcesses, null, 0, _intervalMs);
    }
    private void CheckProcesses(object? state)
    {
        if (!_enabled)
        {
            UpdateProcessStatuses(false);
            return;
        }

        if (UpdateProcessStatuses(true))
        {
            EvaluateRules();
        }
    }
    private bool UpdateProcessStatuses(bool fireEvents)
    {
        var changed = false;
        var changedNames = fireEvents ? new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase) : null;
        foreach (var processName in _trackedProcessNames)
        {
            var isRunning = Process.GetProcessesByName(processName).Any();
            if (!_processStatus.TryGetValue(processName, out var wasRunning) || wasRunning != isRunning)
            {
                _processStatus[processName] = isRunning;
                changed = true;
                changedNames?.Add(processName, isRunning);
            }
        }
        if (fireEvents && changedNames?.Count > 0)
        {
            foreach (var cfg in _configs)
            {
                if (changedNames.TryGetValue(cfg.ProcessName, out var isRunning))
                {
                    if (isRunning)
                    {
                        ProcessStarted?.Invoke(cfg);
                    }
                    else
                    {
                        ProcessStopped?.Invoke(cfg);
                    }
                }
            }
        }
        return changed;
    }
    private void EvaluateRules()
    {
        foreach (var cfg in _configs)
        {
            var shouldApply = ShouldApplyRules(cfg);
            var isApplied = _rulesApplied.TryGetValue(cfg, out var applied) && applied;
            if (shouldApply == isApplied)
            {
                continue;
            }
            if (shouldApply)
            {
                ApplyRules(cfg);
            }
            else
            {
                RemoveRules(cfg);
            }
            _rulesApplied[cfg] = shouldApply;
        }
    }
    private bool ShouldApplyRules(ProcessMonitorConfiguration cfg)
    {
        if (!IsProcessRunning(cfg.ProcessName))
        {
            return false;
        }
        if (cfg.RequireAllProcesses.Count > 0 && cfg.RequireAllProcesses.Any(name => !IsProcessRunning(name)))
        {
            return false;
        }
        if (cfg.RequireAnyProcesses.Count > 0 && !cfg.RequireAnyProcesses.Any(IsProcessRunning))
        {
            return false;
        }
        return true;
    }
    private bool IsProcessRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }
        return _processStatus.TryGetValue(processName, out var isRunning) && isRunning;
    }
    private void ApplyRules(ProcessMonitorConfiguration cfg)
    {
        foreach (var rule in cfg.LocalPortRules)
        {
            AddFirewallRule(cfg, rule, true);
        }
        foreach (var rule in cfg.RemotePortRules)
        {
            AddFirewallRule(cfg, rule, false);
        }
    }
    private void RemoveRules(ProcessMonitorConfiguration cfg)
    {
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
            foreach (var cfg in _configs)
            {
                _rulesApplied[cfg] = false;
            }
            return;
        }
        UpdateProcessStatuses(false);
        EvaluateRules();
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
    private static HashSet<string> BuildTrackedProcessNames(IEnumerable<ProcessMonitorConfiguration> configs)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cfg in configs)
        {
            if (!string.IsNullOrWhiteSpace(cfg.ProcessName))
            {
                names.Add(cfg.ProcessName);
            }
            foreach (var name in cfg.RequireAllProcesses)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }
            foreach (var name in cfg.RequireAnyProcesses)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }
            }
        }
        return names;
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
    private static void NormalizeConfig(ProcessMonitorConfiguration cfg)
    {
        cfg.ProcessName = NormalizeProcessName(cfg.ProcessName);
        cfg.RequireAllProcesses = NormalizeProcessList(cfg.RequireAllProcesses);
        cfg.RequireAnyProcesses = NormalizeProcessList(cfg.RequireAnyProcesses);
    }
    private static string NormalizeProcessName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }
        var trimmed = name.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }
    private static List<string> NormalizeProcessList(List<string>? names)
    {
        if (names == null || names.Count == 0)
        {
            return new List<string>();
        }
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var normalized = NormalizeProcessName(name);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                set.Add(normalized);
            }
        }
        return set.ToList();
    }
    public void Dispose()
    {
        _timer?.Dispose();
        ClearFirewallRules();
    }
}
