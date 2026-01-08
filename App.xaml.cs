using System.Configuration;
using System.Data;
using AstralLite.Services;
using AstralLite.Models;
using System.Diagnostics;
using System.Windows;
namespace AstralLite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ProcessMonitorService? _processMonitorService;
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);
            // Load process monitor configuration.
            var configs = ProcessMonitorConfigurationList.Processes;
            if (configs.Count > 0)
            {
                _processMonitorService = new ProcessMonitorService(configs);
                _processMonitorService.ProcessStarted += cfg =>
                    Debug.WriteLine($"[ProcessMonitor] {cfg.DisplayName}({cfg.ProcessName}) started");
                _processMonitorService.ProcessStopped += cfg =>
                    Debug.WriteLine($"[ProcessMonitor] {cfg.DisplayName}({cfg.ProcessName}) stopped");
            }
        }
        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            // Ensure network disconnect on exit.
            try
            {
                if (NetworkService.Instance.IsConnected)
                {
                    NetworkService.Instance.Disconnect();
                }
                _processMonitorService?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cleanup error: {ex.Message}");
            }
            base.OnExit(e);
        }
    }
}