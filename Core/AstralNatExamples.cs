using AstralLite.Core;

namespace AstralLite;

/// <summary>
/// AstralNat 静态类的使用示例，用于 EasyTier 网络管理
/// </summary>
public static class AstralNatExamples
{
    /// <summary>
    /// 示例 1: 简单的 P2P 组网连接
    /// </summary>
    public static void Example1_SimpleP2P()
    {
        try
        {



            // 启动网络
            AstralNat.StartNetwork("""
                hostname = "hostname222"
                dhcp = true
                listeners = [
                    "tcp://0.0.0.0:11010",
                    "udp://0.0.0.0:11010",
                ]

                [network_identity]
                network_name = "123qwe"
                network_secret = "123qwe"

                [[peer]]
                uri = "tcp://public.easytier.top:11010"

                [flags]
                """);

            System.Diagnostics.Debug.WriteLine("\n网络正在运行中... (应用退出时会自动停止)");

            while (true)
            {
                // 等待 1 秒
                Thread.Sleep(1000);

                // 获取网络信息
                var info = AstralNat.GetNetworkInfo();
                System.Diagnostics.Debug.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] 网络信息:");
                foreach (var kv in info)
                {
                    System.Diagnostics.Debug.WriteLine($"  {kv.Key}: {kv.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? 错误: {ex.Message}");
            throw; // 重新抛出异常以便在 App.xaml.cs 中捕获
        }
    }

  
}
