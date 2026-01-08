using System.Configuration;
using System.Data;

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

            // 在后台线程测试 Example1_SimpleP2P
            Task.Run(() =>
            {
                try
                {
                    AstralNatExamples.Example1_SimpleP2P();
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        System.Windows.MessageBox.Show(
                            $"测试 Example1_SimpleP2P 时出错:\n{ex.Message}",
                            "测试错误",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Error
                        );
                    });
                }
            });
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            // 清理所有网络连接
            try
            {
                Core.AstralNat.StopAllNetworks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"清理网络时出错: {ex.Message}");
            }

            base.OnExit(e);
        }
    }
}
