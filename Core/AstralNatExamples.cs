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
            // 创建简单配置
            string config = AstralNat.CreateSimpleConfig(
                instanceName: "astral-client",
                networkName: "astral-network",
                networkSecret: "your-secret-password",
                peerUrl: null, // 留空则作为服务器
                listenPort: 11010
            );

            // 验证配置
            if (AstralNat.ValidateConfig(config))
            {
                System.Diagnostics.Debug.WriteLine("? 配置有效");
            }

            // 启动网络
            AstralNat.StartNetwork(config);
            System.Diagnostics.Debug.WriteLine("? 网络已启动");

            // 等待初始化
            Thread.Sleep(3000);

            // 获取网络信息
            var info = AstralNat.GetNetworkInfo();
            System.Diagnostics.Debug.WriteLine("\n网络信息:");
            foreach (var kv in info)
            {
                System.Diagnostics.Debug.WriteLine($"  {kv.Key}: {kv.Value}");
            }

            System.Diagnostics.Debug.WriteLine("\n网络正在运行中... (应用退出时会自动停止)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"? 错误: {ex.Message}");
            throw; // 重新抛出异常以便在 App.xaml.cs 中捕获
        }
    }

    /// <summary>
    /// 示例 2: 具有多个对等节点的高级配置
    /// </summary>
    public static void Example2_AdvancedConfig()
    {
        try
        {
            // 创建高级配置
            string config = AstralNat.CreateAdvancedConfig(
                instanceName: "astral-node",
                networkName: "astral-mesh",
                networkSecret: "super-secret-123",
                peerUrls: new[]
                {
                    "tcp://peer1.example.com:11010",
                    "udp://peer2.example.com:11010"
                },
                listeners: new[]
                {
                    "tcp://0.0.0.0:11010",
                    "udp://0.0.0.0:11010",
                    "wg://0.0.0.0:11011"
                },
                enableEncryption: true,
                enableCompression: true,
                enableIpv6: false,
                logLevel: "info"
            );

            // 启动网络
            AstralNat.StartNetwork(config);
            Console.WriteLine("高级网络已启动");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 示例 3: 管理多个网络实例
    /// </summary>
    public static void Example3_MultipleNetworks()
    {
        try
        {
            // 启动第一个网络
            var config1 = AstralNat.CreateSimpleConfig(
                instanceName: "network-1",
                networkName: "network-1",
                networkSecret: "secret-1",
                listenPort: 11010
            );
            AstralNat.StartNetwork(config1);

            // 启动第二个网络
            var config2 = AstralNat.CreateSimpleConfig(
                instanceName: "network-2",
                networkName: "network-2",
                networkSecret: "secret-2",
                listenPort: 11020
            );
            AstralNat.StartNetwork(config2);

            // 列出活动实例
            var instances = AstralNat.GetActiveInstances();
            Console.WriteLine($"活动实例: {string.Join(", ", instances)}");

            // 停止特定网络
            AstralNat.StopNetwork("network-1");
            Console.WriteLine("已停止 network-1");

            // 停止所有剩余网络
            AstralNat.StopAllNetworks();
            Console.WriteLine("所有网络已停止");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 示例 4: 自定义 TOML 配置
    /// </summary>
    public static void Example4_CustomConfig()
    {
        try
        {
            // 编写自定义 TOML 配置
            string customConfig = @"
instance_name = ""astral-custom""
dhcp = true
network_name = ""astral-game""
network_secret = ""game-secret-123""

listeners = [
    ""tcp://0.0.0.0:11010"",
    ""udp://0.0.0.0:11010""
]

peer_urls = [
    ""tcp://game-server.example.com:11010""
]

enable_encryption = true
enable_compression = true
log_level = ""info""
";

            // 验证并启动
            AstralNat.ValidateConfig(customConfig);
            AstralNat.StartNetwork(customConfig);
            Console.WriteLine("自定义网络已启动");

            // 监控网络状态
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(2000);
                var info = AstralNat.GetNetworkInfo();
                Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] 网络状态:");
                foreach (var kv in info)
                {
                    Console.WriteLine($"  {kv.Key}: {kv.Value}");
                }
            }

            AstralNat.StopAllNetworks();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 示例 5: 使用前检查 DLL 可用性
    /// </summary>
    public static void Example5_CheckDlls()
    {
        if (!AstralNat.CheckDllsAvailable())
        {
            Console.WriteLine("? EasyTier DLL 不可用!");
            Console.WriteLine("请确保以下文件存在:");
            Console.WriteLine("  - easytier_ffi.dll");
            Console.WriteLine("  - wintun.dll");
            Console.WriteLine("  - Packet.dll");
            return;
        }

        Console.WriteLine("? EasyTier DLL 可用");

        // 继续网络操作...
        var config = AstralNat.CreateSimpleConfig(
            instanceName: "test-network",
            networkName: "test",
            networkSecret: "test123"
        );

        AstralNat.StartNetwork(config);
        Console.WriteLine("网络启动成功");

        Thread.Sleep(2000);
        AstralNat.StopAllNetworks();
        Console.WriteLine("网络已停止");
    }

    /// <summary>
    /// 示例 6: 异步网络管理
    /// </summary>
    public static async Task Example6_AsyncUsage()
    {
        try
        {
            // 异步启动网络
            await Task.Run(() =>
            {
                var config = AstralNat.CreateSimpleConfig(
                    instanceName: "async-network",
                    networkName: "async-network",
                    networkSecret: "async-secret"
                );
                AstralNat.StartNetwork(config);
            });

            Console.WriteLine("网络已异步启动");

            // 后台监控
            var monitorTask = Task.Run(async () =>
            {
                for (int i = 0; i < 10; i++)
                {
                    await Task.Delay(1000);
                    var info = AstralNat.GetNetworkInfo();
                    Console.WriteLine($"[{i}] 活动实例: {AstralNat.GetActiveInstances().Count}");
                }
            });

            await monitorTask;

            // 清理
            await Task.Run(() => AstralNat.StopAllNetworks());
            Console.WriteLine("网络已异步停止");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"错误: {ex.Message}");
        }
    }
}
