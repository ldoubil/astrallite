using System.Configuration;
using System.Data;
using AstralLite.Services;
using AstralLite.Models;
using System.Diagnostics;

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

            // 直接从配置类加载进程监听配置
            var configs = ProcessMonitorConfigurationList.Processes;
            if (configs.Count > 0)
            {
                _processMonitorService = new ProcessMonitorService(configs);
                _processMonitorService.ProcessStarted += cfg => Debug.WriteLine($"[ProcessMonitor] {cfg.DisplayName}({cfg.ProcessName}) 已启动");
                _processMonitorService.ProcessStopped += cfg => Debug.WriteLine($"[ProcessMonitor] {cfg.DisplayName}({cfg.ProcessName}) 已关闭");
            }
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            // 确保应用退出时断开网络连接
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
                System.Diagnostics.Debug.WriteLine($"清理网络时出错: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}
