using System.Configuration;
using System.Data;
using AstralLite.Services;
using AstralLite.Models;
using System.Diagnostics;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;
namespace AstralLite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ProcessMonitorService? _processMonitorService;
        private const string MutexName = "AstralLite.SingleInstance";
        private const string ActivateEventName = "AstralLite.Activate";
        private Mutex? _singleInstanceMutex;
        private EventWaitHandle? _activateEvent;
        private CancellationTokenSource? _activateCts;
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            if (!EnsureSingleInstance())
            {
                Shutdown();
                return;
            }
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
                StopActivationListener();
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

        private bool EnsureSingleInstance()
        {
            _singleInstanceMutex = new Mutex(true, MutexName, out var createdNew);
            if (!createdNew)
            {
                TrySignalExistingInstance();
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                return false;
            }

            _activateEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
            _activateCts = new CancellationTokenSource();
            Task.Run(() => ActivationLoop(_activateCts.Token));
            return true;
        }

        private void StopActivationListener()
        {
            try
            {
                _activateCts?.Cancel();
                _activateEvent?.Set();
            }
            catch
            {
                // ignore shutdown signaling failures
            }
            finally
            {
                _activateEvent?.Dispose();
                _activateEvent = null;
                _singleInstanceMutex?.ReleaseMutex();
                _singleInstanceMutex?.Dispose();
                _singleInstanceMutex = null;
                _activateCts?.Dispose();
                _activateCts = null;
            }
        }

        private void ActivationLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                _activateEvent?.WaitOne();
                if (token.IsCancellationRequested)
                {
                    break;
                }

                Dispatcher.Invoke(ActivateMainWindow);
            }
        }

        private void ActivateMainWindow()
        {
            var window = Current.MainWindow;
            if (window == null)
            {
                foreach (Window w in Current.Windows)
                {
                    if (w is MainWindow)
                    {
                        window = w;
                        break;
                    }
                }
            }

            if (window == null)
            {
                return;
            }

            if (!window.IsVisible)
            {
                window.Show();
            }

            if (window.WindowState == WindowState.Minimized)
            {
                window.WindowState = WindowState.Normal;
            }

            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
            window.Focus();
        }

        private static void TrySignalExistingInstance()
        {
            try
            {
                using var existing = EventWaitHandle.OpenExisting(ActivateEventName);
                existing.Set();
            }
            catch
            {
                try
                {
                    using var created = new EventWaitHandle(false, EventResetMode.AutoReset, ActivateEventName);
                    created.Set();
                }
                catch
                {
                    // ignore if signaling fails
                }
            }
        }
    }
}
