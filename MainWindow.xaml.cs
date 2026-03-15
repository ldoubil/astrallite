using System.ComponentModel;
using System.Windows;
using Application = System.Windows.Application;

namespace AstralLite
{
    public partial class MainWindow : Window
    {
        private NotifyIcon? notifyIcon;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            (Application.Current as App)?.OpenDebugWindow();
        }

        private void InitializeTrayIcon()
        {
            notifyIcon = new NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "Astral-收获日联机工具",
                Visible = true
            };

            notifyIcon.DoubleClick += (s, e) => ShowWindow();

            var contextMenu = new ContextMenuStrip();
            
            var showMenuItem = new ToolStripMenuItem("显示窗口");
            showMenuItem.Click += (s, e) => ShowWindow();
            contextMenu.Items.Add(showMenuItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var exitMenuItem = new ToolStripMenuItem("退出");
            exitMenuItem.Click += (s, e) => ExitApplication();
            contextMenu.Items.Add(exitMenuItem);

            notifyIcon.ContextMenuStrip = contextMenu;
        }

        private static Icon LoadTrayIcon()
        {
            var streamInfo = Application.GetResourceStream(new Uri("pack://application:,,,/icon.ico"));
            if (streamInfo?.Stream != null)
            {
                return new Icon(streamInfo.Stream);
            }

            return new Icon("icon.ico");
        }

        private void TitleBar_MinimizeRequested(object? sender, EventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TitleBar_CloseRequested(object? sender, EventArgs e)
        {
            HideToTray();
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
        }

        private void HideToTray()
        {
            this.Hide();
            if (notifyIcon != null)
            {
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(1000, "Astral-收获日联机工具", "程序已最小化到系统托盘", ToolTipIcon.Info);
            }
        }

        private void ExitApplication()
        {
            if (notifyIcon != null)
            {
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
                notifyIcon = null; // 防止后续访问
            }
            Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;
            HideToTray();
        }
    }
}
