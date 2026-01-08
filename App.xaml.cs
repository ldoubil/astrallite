using System.Configuration;
using System.Data;
using AstralLite.Services;

namespace AstralLite
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(System.Windows.StartupEventArgs e)
        {
            base.OnStartup(e);

            // 不再自动启动测试示例
            // 现在由用户通过 UI 手动加入房间
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理网络时出错: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}
