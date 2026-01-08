using System.Collections.ObjectModel;

namespace AstralLite.Models;

/// <summary>
/// 房间列表配置管理类
/// </summary>
public static class RoomConfigurationList
{
    /// <summary>
    /// 预定义的房间配置列表
    /// </summary>
    public static ObservableCollection<RoomConfiguration> Rooms { get; } = new()
    {
        new RoomConfiguration
        {
            GroupName = "测试组",
            RoomName = "测试房间1",
            TestIp = "100.100.0.1",
            ServerConfig = """
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
                """
        },
        new RoomConfiguration
        {
            GroupName = "测试组",
            RoomName = "测试房间2",
            TestIp = "100.100.0.2",
            ServerConfig = """
                dhcp = true
                listeners = [
                    "tcp://0.0.0.0:11010",
                    "udp://0.0.0.0:11010",
                ]

                [network_identity]
                network_name = "test-network-2"
                network_secret = "test-secret-2"

                [[peer]]
                uri = "tcp://public.easytier.top:11010"

                [flags]
                """
        },
        new RoomConfiguration
        {
            GroupName = "生产组",
            RoomName = "生产房间122222222",
            TestIp = "100.100.1.1",
            ServerConfig = """
                dhcp = true
                listeners = [
                    "tcp://0.0.0.0:11010",
                    "udp://0.0.0.0:11010",
                ]

                [network_identity]
                network_name = "墌훍疊쭵ギữバὪ"
                network_secret = "夔Ж黽Иネѷ몪ぱ뫫Ӄ"

                [[peer]]
                uri = "tcp://pd2.629957.xyz:39647"

                [flags]
                """
        },
        new RoomConfiguration
        {
            GroupName = "生产组",
            RoomName = "生产房间2",
            TestIp = "100.100.1.2",
            ServerConfig = """
                ipv4 = "100.100.0.0/20"
                dhcp = false
                listeners = [
                    "tcp://0.0.0.0:11020",
                    "udp://0.0.0.0:11020",
                ]

                [network_identity]
                network_name = "prod-network-2"
                network_secret = "prod-secret-2"

                [[peer]]
                uri = "tcp://prod-server.example.com:11010"

                [flags]
                """
        }
    };

    /// <summary>
    /// 根据房间名称获取配置
    /// </summary>
    /// <param name="roomName">房间名称</param>
    /// <returns>房间配置，如果未找到则返回 null</returns>
    public static RoomConfiguration? GetRoomByName(string roomName)
    {
        return Rooms.FirstOrDefault(r => r.RoomName.Equals(roomName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 根据分组名称获取所有房间
    /// </summary>
    /// <param name="groupName">分组名称</param>
    /// <returns>该分组下的所有房间配置</returns>
    public static IEnumerable<RoomConfiguration> GetRoomsByGroup(string groupName)
    {
        return Rooms.Where(r => r.GroupName.Equals(groupName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取所有分组名称
    /// </summary>
    /// <returns>所有唯一的分组名称列表</returns>
    public static IEnumerable<string> GetAllGroups()
    {
        return Rooms.Select(r => r.GroupName).Distinct();
    }
}
