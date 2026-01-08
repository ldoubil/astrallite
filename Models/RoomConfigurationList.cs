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
            GroupName = "PAYDAY2",
            RoomName = "公共房间",
            TestIp = "100.100.1.1",
            ServerConfig = """
                dhcp = true

                listeners = [
                    "tcp://0.0.0.0:0",
                    "udp://0.0.0.0:0",
                ]

                tcp_whitelist = ["0"]
                udp_whitelist = ["7777-8000"]

                [network_identity]
                network_name = "墌훍疊쭵ギữバὪ"
                network_secret = "夔Ж黽Иネѷ몪ぱ뫫Ӄ"

                [[peer]]
                uri = "tcp://pd2.629957.xyz:39647"

                [flags]
                default_protocol = "tcp"
                multi_thread = true
                dev_name = "AstralPD2"
                disable_sym_hole_punching = true
                disable_kcp_input = true
                disable_quic_input = true
                
                """
        },
        
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
