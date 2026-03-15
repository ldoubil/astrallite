using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AstralLite.Models;
using ThreadingTimer = System.Threading.Timer;

namespace AstralLite.Services;

public class ProcessMonitorService : IDisposable
{
    private readonly Dictionary<string, bool> _processStatus = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ProcessMonitorConfiguration, List<string>> _appliedRuleIds = new();
    private readonly HashSet<string> _trackedProcessNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private bool _enabled = true;
    private ThreadingTimer? _timer;
    private readonly int _intervalMs;
    private readonly IReadOnlyList<ProcessMonitorConfiguration> _configs;
    private MgWall? _mgWall;

    public event Action<ProcessMonitorConfiguration>? ProcessStarted;
    public event Action<ProcessMonitorConfiguration>? ProcessStopped;
    public event Action<ProcessMonitorConfiguration>? RulesApplied;
    public event Action<ProcessMonitorConfiguration>? RulesRemoved;
    public event EventHandler<int>? FilterCountChanged;
    public event Action<string>? OnLogMessage;

    public int FilterCount
    {
        get
        {
            var status = _mgWall?.GetStatus();
            return status?.ActiveRules ?? 0;
        }
    }

    public bool IsEnabled => _enabled;

    public bool IsProcessRunning(string processName)
    {
        return _processStatus.TryGetValue(processName, out var isRunning) && isRunning;
    }

    public bool AreRulesApplied(ProcessMonitorConfiguration cfg)
    {
        lock (_lock)
        {
            return _appliedRuleIds.TryGetValue(cfg, out var ids) && ids.Count > 0;
        }
    }

    public IReadOnlyList<ProcessMonitorConfiguration> GetConfigurations() => _configs;

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
            _appliedRuleIds[cfg] = new List<string>();
        }

        InitializeMgWall();
        _timer = new ThreadingTimer(CheckProcesses, null, 0, _intervalMs);
    }

    private void InitializeMgWall()
    {
        try
        {
            _mgWall = new MgWall();
            _mgWall.Start();
            OnLogMessage?.Invoke("[WFP] MgWall 引擎已启动");
        }
        catch (Exception ex)
        {
            OnLogMessage?.Invoke($"[WFP] MgWall 启动失败: {ex.Message}");
            _mgWall = null;
        }
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
        var applicableConfigs = _configs.Where(ShouldApplyRules).ToList();

        foreach (var cfg in _configs)
        {
            var shouldApply = applicableConfigs.Contains(cfg);
            var isApplied = AreRulesApplied(cfg);

            if (!shouldApply && isApplied)
            {
                RemoveRules(cfg);
            }
        }

        foreach (var cfg in applicableConfigs)
        {
            if (!AreRulesApplied(cfg))
            {
                ApplyRules(cfg);
            }
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

        if (cfg.ExcludeProcesses.Count > 0 && cfg.ExcludeProcesses.Any(IsProcessRunning))
        {
            return false;
        }

        if (cfg.RequireAnyProcesses.Count > 0 && !cfg.RequireAnyProcesses.Any(IsProcessRunning))
        {
            return false;
        }

        return true;
    }

    private void ApplyRules(ProcessMonitorConfiguration cfg)
    {
        if (_mgWall == null)
        {
            OnLogMessage?.Invoke("[WFP] MgWall 未初始化");
            return;
        }

        var appPath = ResolveApplicationPath(cfg);
        var ruleIds = new List<string>();

        foreach (var rule in cfg.Rules)
        {
            var ruleId = $"mg_{cfg.ProcessName}_{rule.Name}_{rule.Protocol}_{rule.Port}";
            try
            {
                var mgRule = new MgWallRule
                {
                    Id = ruleId,
                    Name = $"MG_{cfg.DisplayName}_{rule.Name}",
                    Enabled = true,
                    Action = rule.Action.ToLowerInvariant(),
                    Protocol = rule.Protocol.ToLowerInvariant(),
                    Direction = rule.Direction.ToLowerInvariant(),
                    IpVersion = rule.IpVersion.ToLowerInvariant(),
                    AppPath = appPath,
                    RemoteIp = rule.RemoteAddress,
                    LocalIp = rule.LocalAddress,
                    RemotePort = rule.Port,
                    Weight = (byte)rule.Weight
                };

                _mgWall.AddRule(mgRule);
                ruleIds.Add(ruleId);
                OnLogMessage?.Invoke($"[WFP] 规则已添加: {ruleId}");
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"[WFP] 规则添加失败: {ruleId} - {ex.Message}");
            }
        }

        lock (_lock)
        {
            _appliedRuleIds[cfg] = ruleIds;
        }

        if (ruleIds.Count > 0)
        {
            RulesApplied?.Invoke(cfg);
            OnFilterCountChanged();
        }
    }

    private void RemoveRules(ProcessMonitorConfiguration cfg)
    {
        if (_mgWall == null) return;

        List<string> ruleIds;
        lock (_lock)
        {
            if (!_appliedRuleIds.TryGetValue(cfg, out var ids) || ids.Count == 0)
            {
                return;
            }
            ruleIds = new List<string>(ids);
            _appliedRuleIds[cfg] = new List<string>();
        }

        foreach (var ruleId in ruleIds)
        {
            try
            {
                _mgWall.RemoveRule(ruleId);
                OnLogMessage?.Invoke($"[WFP] 规则已移除: {ruleId}");
            }
            catch (Exception ex)
            {
                OnLogMessage?.Invoke($"[WFP] 规则移除失败: {ruleId} - {ex.Message}");
            }
        }

        RulesRemoved?.Invoke(cfg);
        OnFilterCountChanged();
    }

    public void SetEnabled(bool enabled)
    {
        if (_enabled == enabled) return;

        _enabled = enabled;

        if (!enabled)
        {
            foreach (var cfg in _configs.ToList())
            {
                RemoveRules(cfg);
            }
            return;
        }

        UpdateProcessStatuses(false);
        EvaluateRules();
    }

    private static string ResolveApplicationPath(ProcessMonitorConfiguration cfg)
    {
        try
        {
            var processes = Process.GetProcessesByName(cfg.ProcessName);
            if (processes.Length == 0)
            {
                return $"{cfg.ProcessName}.exe";
            }

            var process = processes[0];
            var path = process.MainModule?.FileName;

            if (!string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
        }
        catch
        {
        }
        return $"{cfg.ProcessName}.exe";
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
                if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
            }
            foreach (var name in cfg.RequireAnyProcesses)
            {
                if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
            }
            foreach (var name in cfg.ExcludeProcesses)
            {
                if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
            }
        }
        return names;
    }

    private static void NormalizeConfig(ProcessMonitorConfiguration cfg)
    {
        cfg.ProcessName = NormalizeProcessName(cfg.ProcessName);
        cfg.RequireAllProcesses = NormalizeProcessList(cfg.RequireAllProcesses);
        cfg.RequireAnyProcesses = NormalizeProcessList(cfg.RequireAnyProcesses);
    }

    private static string NormalizeProcessName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;
        var trimmed = name.Trim();
        return trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
            ? trimmed[..^4]
            : trimmed;
    }

    private static List<string> NormalizeProcessList(List<string>? names)
    {
        if (names == null || names.Count == 0) return new List<string>();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var normalized = NormalizeProcessName(name);
            if (!string.IsNullOrWhiteSpace(normalized)) set.Add(normalized);
        }
        return set.ToList();
    }

    private void OnFilterCountChanged()
    {
        FilterCountChanged?.Invoke(this, FilterCount);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        
        foreach (var cfg in _configs.ToList())
        {
            RemoveRules(cfg);
        }

        _mgWall?.Dispose();
        _mgWall = null;
    }
}
