using AstralLite.Core;
using AstralLite.Models;
using AstralLite.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace AstralLite.ViewModels;

public class RuleStatusItem : ObservableObject
{
    public string ProcessName { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Protocol { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string IpVersion { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string RemoteAddress { get; set; } = string.Empty;
    public int Weight { get; set; }
    
    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (SetProperty(ref _isActive, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }
    
    public string StatusText => IsActive ? "✓ 生效中" : "✗ 已关闭";
    public string StatusColor => IsActive ? "#4CAF50" : "#F44336";
}

public class ProcessStatusItem : ObservableObject
{
    public string ProcessName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    
    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }
    
    private bool _rulesApplied;
    public bool RulesApplied
    {
        get => _rulesApplied;
        set
        {
            if (SetProperty(ref _rulesApplied, value))
            {
                OnPropertyChanged(nameof(RulesText));
            }
        }
    }
    
    public string StatusText => IsRunning ? "● 运行中" : "○ 未运行";
    public string StatusColor => IsRunning ? "#4CAF50" : "#9E9E9E";
    public string RulesText => RulesApplied ? "规则已应用" : "规则未应用";
}

public class DebugViewModel : ObservableObject
{
    private readonly DispatcherTimer _refreshTimer;
    private ProcessMonitorService? _monitorService;
    private IReadOnlyList<ProcessMonitorConfiguration>? _configs;
    
    public ObservableCollection<ProcessStatusItem> Processes { get; } = new();
    public ObservableCollection<RuleStatusItem> Rules { get; } = new();
    public ObservableCollection<string> Logs { get; } = new();
    
    private int _activeRuleCount;
    public int ActiveRuleCount
    {
        get => _activeRuleCount;
        set => SetProperty(ref _activeRuleCount, value);
    }
    
    private bool _wfpEnabled;
    public bool WfpEnabled
    {
        get => _wfpEnabled;
        set => SetProperty(ref _wfpEnabled, value);
    }
    
    public RelayCommand RefreshCommand { get; }
    public RelayCommand ClearLogsCommand { get; }
    
    public DebugViewModel()
    {
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;
        
        RefreshCommand = new RelayCommand(Refresh);
        ClearLogsCommand = new RelayCommand(ClearLogs);
    }
    
    public void Initialize()
    {
        var app = System.Windows.Application.Current as App;
        _monitorService = app?.ProcessMonitorService;
        _configs = ProcessMonitorConfigurationList.Processes;
        
        if (_monitorService != null)
        {
            _monitorService.ProcessStarted += OnProcessStarted;
            _monitorService.ProcessStopped += OnProcessStopped;
            _monitorService.RulesApplied += OnRulesApplied;
            _monitorService.RulesRemoved += OnRulesRemoved;
            _monitorService.FilterCountChanged += OnFilterCountChanged;
            _monitorService.OnLogMessage += OnLogMessage;
        }
        
        RefreshStatus();
        _refreshTimer.Start();
    }
    
    public void Cleanup()
    {
        _refreshTimer.Stop();
        if (_monitorService != null)
        {
            _monitorService.ProcessStarted -= OnProcessStarted;
            _monitorService.ProcessStopped -= OnProcessStopped;
            _monitorService.RulesApplied -= OnRulesApplied;
            _monitorService.RulesRemoved -= OnRulesRemoved;
            _monitorService.FilterCountChanged -= OnFilterCountChanged;
            _monitorService.OnLogMessage -= OnLogMessage;
        }
    }
    
    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        RefreshStatus();
    }
    
    private void RefreshStatus()
    {
        if (_configs == null || _monitorService == null) return;
        
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            Processes.Clear();
            foreach (var cfg in _configs)
            {
                var isRunning = _monitorService.IsProcessRunning(cfg.ProcessName);
                var rulesApplied = _monitorService.AreRulesApplied(cfg);
                
                Processes.Add(new ProcessStatusItem
                {
                    ProcessName = cfg.ProcessName,
                    DisplayName = cfg.DisplayName,
                    IsRunning = isRunning,
                    RulesApplied = rulesApplied
                });
            }
            
            Rules.Clear();
            foreach (var cfg in _configs)
            {
                var isActive = _monitorService.AreRulesApplied(cfg);
                foreach (var rule in cfg.Rules)
                {
                    Rules.Add(new RuleStatusItem
                    {
                        ProcessName = cfg.ProcessName,
                        RuleName = rule.Name,
                        Action = rule.Action,
                        Protocol = rule.Protocol,
                        Port = rule.Port,
                        IpVersion = rule.IpVersion,
                        Direction = rule.Direction,
                        RemoteAddress = rule.RemoteAddress ?? "",
                        Weight = rule.Weight,
                        IsActive = isActive
                    });
                }
            }
            
            ActiveRuleCount = _monitorService.FilterCount;
            WfpEnabled = _monitorService.IsEnabled;
        });
    }
    
    private void OnProcessStarted(ProcessMonitorConfiguration cfg)
    {
        AddLog($"[进程启动] {cfg.DisplayName} ({cfg.ProcessName})");
    }
    
    private void OnProcessStopped(ProcessMonitorConfiguration cfg)
    {
        AddLog($"[进程关闭] {cfg.DisplayName} ({cfg.ProcessName})");
    }
    
    private void OnRulesApplied(ProcessMonitorConfiguration cfg)
    {
        AddLog($"[规则应用] {cfg.DisplayName} - {cfg.Rules.Count} 条规则已生效");
    }
    
    private void OnRulesRemoved(ProcessMonitorConfiguration cfg)
    {
        AddLog($"[规则移除] {cfg.DisplayName} - 规则已关闭");
    }
    
    private void OnFilterCountChanged(object? sender, int count)
    {
        ActiveRuleCount = count;
    }
    
    private void OnLogMessage(string message)
    {
        AddLog(message);
    }
    
    private void AddLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            Logs.Insert(0, $"[{timestamp}] {message}");
            if (Logs.Count > 200)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }
        });
    }
    
    public void Refresh()
    {
        RefreshStatus();
        AddLog("[手动刷新] 状态已更新");
    }
    
    public void ClearLogs()
    {
        Logs.Clear();
    }
}
