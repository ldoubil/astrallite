using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AstralLite.Models;
using ThreadingTimer = System.Threading.Timer;

namespace AstralLite.Services;

/// <summary>
/// 进程监听服务，定时检测指定进程并发送开启/关闭事件
/// </summary>
public class ProcessMonitorService : IDisposable
{
    private readonly Dictionary<string, bool> _processStatus = new();
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
    }

    private void CheckProcesses(object? state)
    {
        foreach (var cfg in _configs)
        {
            bool isRunning = Process.GetProcessesByName(cfg.ProcessName).Any();
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

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
