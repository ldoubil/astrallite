using System.Configuration;
using System.Data;
using AstralLite.Services;
using AstralLite.Models;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using System.Threading;
using System.Threading.Tasks;
using MessageBox = System.Windows.MessageBox;
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
        private int _handlingFatalException;
        private const string CrashLogFileName = "crash.log";
        internal ProcessMonitorService? ProcessMonitorService => _processMonitorService;
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            RegisterGlobalExceptionHandlers();
            if (!EnsureSingleInstance())
            {
                Shutdown();
                return;
            }
            base.OnStartup(e);
            var configs = ProcessMonitorConfigurationList.Processes;
            if (configs.Count > 0)
            {
                _processMonitorService = new ProcessMonitorService(configs);
                _processMonitorService.ProcessStarted += cfg =>
                    Debug.WriteLine($"[ProcessMonitor] {cfg.DisplayName}({cfg.ProcessName}) started");
                _processMonitorService.ProcessStopped += cfg =>
                    Debug.WriteLine($"[ProcessMonitor] {cfg.DisplayName}({cfg.ProcessName}) stopped");
            }

            _ = NetworkInterfaceMetricService.EnsureMetricZeroWithWatcherAsync(
                "AstralPD2",
                10,
                TimeSpan.FromMilliseconds(500));
        }
        
        public void OpenDebugWindow()
        {
            var debugWindow = new Views.DebugWindow();
            debugWindow.Show();
        }
        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            // Ensure network disconnect on exit.
            try
            {
                StopActivationListener();
                NetworkInterfaceMetricService.StopAllWatchers();
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

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            ReportFatalException(e.Exception, "UI");
            e.Handled = true;
            Shutdown(1);
        }

        private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReportFatalException(e.ExceptionObject as Exception, "AppDomain");
            if (e.IsTerminating)
            {
                Environment.Exit(1);
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            ReportFatalException(e.Exception, "Task");
            e.SetObserved();
            Shutdown(1);
        }

        private void ReportFatalException(Exception? exception, string source)
        {
            if (Interlocked.Exchange(ref _handlingFatalException, 1) == 1)
            {
                return;
            }

            try
            {
                var logPath = WriteCrashLog(exception, source);
                var message = BuildCrashMessage(exception, logPath);
                ShowCrashDialog(message);
            }
            catch
            {
                // Last resort: avoid throwing inside the crash handler.
            }
        }

        private void ShowCrashDialog(string message)
        {
            if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
            {
                return;
            }

            if (Dispatcher.CheckAccess())
            {
                MessageBox.Show(message, "AstralLite Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                Dispatcher.Invoke(() =>
                    MessageBox.Show(message, "AstralLite Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private static string BuildCrashMessage(Exception? exception, string? logPath)
        {
            var summary = exception == null
                ? "Unknown error."
                : $"{exception.GetType().Name}: {exception.Message}";

            var logLine = string.IsNullOrWhiteSpace(logPath)
                ? "Log: <not available>"
                : $"Log: {logPath}";

            var builder = new StringBuilder();
            builder.AppendLine("AstralLite encountered an unexpected error and needs to close.");
            builder.AppendLine();
            builder.AppendLine(summary);
            builder.AppendLine();
            builder.AppendLine(logLine);
            return builder.ToString();
        }

        private static string? WriteCrashLog(Exception? exception, string source)
        {
            try
            {
                var baseDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AstralLite");
                Directory.CreateDirectory(baseDir);

                var logPath = Path.Combine(baseDir, CrashLogFileName);
                var builder = new StringBuilder();
                builder.AppendLine($"TimeUtc: {DateTime.UtcNow:O}");
                builder.AppendLine($"Source: {source}");
                builder.AppendLine($"Version: {GetAppVersion() ?? "unknown"}");
                builder.AppendLine($"OS: {Environment.OSVersion}");
                builder.AppendLine();
                builder.AppendLine(exception?.ToString() ?? "Exception: <null>");
                builder.AppendLine(new string('-', 80));

                File.AppendAllText(logPath, builder.ToString());
                return logPath;
            }
            catch
            {
                return null;
            }
        }

        private static string? GetAppVersion()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly()
                    .GetName()
                    .Version?
                    .ToString();
            }
            catch
            {
                return null;
            }
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
