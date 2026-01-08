using System.Text.Json.Serialization;

namespace AstralLite.Models.Network;

public class NetworkInfo
{
    [JsonPropertyName("dev_name")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("my_node_info")]
    public MyNodeInfo? MyNodeInfo { get; set; }

    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();

    [JsonPropertyName("routes")]
    public List<RouteInfo> Routes { get; set; } = new();

    [JsonPropertyName("peers")]
    public List<PeerInfo> Peers { get; set; } = new();

    [JsonPropertyName("peer_route_pairs")]
    public List<PeerRoutePair> PeerRoutePairs { get; set; } = new();

    [JsonPropertyName("running")]
    public bool Running { get; set; }

    [JsonPropertyName("error_msg")]
    public string? ErrorMessage { get; set; }

    [JsonPropertyName("foreign_network_summary")]
    public ForeignNetworkSummary? ForeignNetworkSummary { get; set; }
}

public class MyNodeInfo
{
    [JsonPropertyName("virtual_ipv4")]
    public InterfaceIp? VirtualIpv4 { get; set; }

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("ips")]
    public IpInfo? Ips { get; set; }

    [JsonPropertyName("stun_info")]
    public StunInfo? StunInfo { get; set; }

    [JsonPropertyName("listeners")]
    public List<ListenerInfo> Listeners { get; set; } = new();

    [JsonPropertyName("vpn_portal_cfg")]
    public string VpnPortalConfig { get; set; } = string.Empty;
}

public class IpInfo
{
    [JsonPropertyName("public_ipv4")]
    public InterfaceIp? PublicIpv4 { get; set; }

    [JsonPropertyName("interface_ipv4s")]
    public List<InterfaceIp> InterfaceIpv4s { get; set; } = new();

    [JsonPropertyName("public_ipv6")]
    public InterfaceIpv6? PublicIpv6 { get; set; }

    [JsonPropertyName("interface_ipv6s")]
    public List<InterfaceIpv6> InterfaceIpv6s { get; set; } = new();

    [JsonPropertyName("listeners")]
    public List<object> Listeners { get; set; } = new();
}

public class InterfaceIp
{
    [JsonPropertyName("addr")]
    public long Addr { get; set; }

    public string ToIpString()
    {
        var bytes = BitConverter.GetBytes((uint)Addr);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return $"{bytes[0]}.{bytes[1]}.{bytes[2]}.{bytes[3]}";
    }
}

public class InterfaceIpv6
{
    [JsonPropertyName("part1")]
    public long Part1 { get; set; }

    [JsonPropertyName("part2")]
    public long Part2 { get; set; }

    [JsonPropertyName("part3")]
    public long Part3 { get; set; }

    [JsonPropertyName("part4")]
    public long Part4 { get; set; }
}

public class ConnIdParts
{
    [JsonPropertyName("part1")]
    public uint Part1 { get; set; }

    [JsonPropertyName("part2")]
    public uint Part2 { get; set; }

    [JsonPropertyName("part3")]
    public uint Part3 { get; set; }

    [JsonPropertyName("part4")]
    public uint Part4 { get; set; }
}

public class StunInfo
{
    [JsonPropertyName("udp_nat_type")]
    public int UdpNatType { get; set; }

    [JsonPropertyName("tcp_nat_type")]
    public int TcpNatType { get; set; }

    [JsonPropertyName("last_update_time")]
    public long LastUpdateTime { get; set; }

    [JsonPropertyName("public_ip")]
    public List<string> PublicIp { get; set; } = new();

    [JsonPropertyName("min_port")]
    public int MinPort { get; set; }

    [JsonPropertyName("max_port")]
    public int MaxPort { get; set; }
}

public class ListenerInfo
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class RouteInfo
{
    [JsonPropertyName("peer_id")]
    public long PeerId { get; set; }

    [JsonPropertyName("ipv4_addr")]
    public InterfaceIp? Ipv4Addr { get; set; }

    [JsonPropertyName("ipv6_addr")]
    public InterfaceIpv6? Ipv6Addr { get; set; }

    [JsonPropertyName("next_hop_peer_id")]
    public long NextHopPeerId { get; set; }

    [JsonPropertyName("cost")]
    public int Cost { get; set; }

    [JsonPropertyName("path_latency")]
    public int PathLatency { get; set; }

    [JsonPropertyName("proxy_cidrs")]
    public List<string> ProxyCidrs { get; set; } = new();

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("stun_info")]
    public StunInfo? StunInfo { get; set; }

    [JsonPropertyName("inst_id")]
    public string InstanceId { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("feature_flag")]
    public FeatureFlag? FeatureFlag { get; set; }

    [JsonPropertyName("next_hop_peer_id_latency_first")]
    public long NextHopPeerIdLatencyFirst { get; set; }

    [JsonPropertyName("cost_latency_first")]
    public int CostLatencyFirst { get; set; }

    [JsonPropertyName("path_latency_latency_first")]
    public int PathLatencyLatencyFirst { get; set; }
}

public class FeatureFlag
{
    [JsonPropertyName("is_public_server")]
    public bool IsPublicServer { get; set; }

    [JsonPropertyName("avoid_relay_data")]
    public bool AvoidRelayData { get; set; }

    [JsonPropertyName("kcp_input")]
    public bool KcpInput { get; set; }

    [JsonPropertyName("no_relay_kcp")]
    public bool NoRelayKcp { get; set; }

    [JsonPropertyName("support_conn_list_sync")]
    public bool SupportConnListSync { get; set; }
}

public class PeerInfo
{
    [JsonPropertyName("peer_id")]
    public long PeerId { get; set; }

    [JsonPropertyName("conns")]
    public List<ConnectionInfo> Connections { get; set; } = new();

    [JsonPropertyName("default_conn_id")]
    public ConnIdParts? DefaultConnId { get; set; }

    [JsonPropertyName("directly_connected_conns")]
    public List<ConnIdParts> DirectlyConnectedConns { get; set; } = new();
}

public class ConnectionInfo
{
    [JsonPropertyName("conn_id")]
    public string ConnectionId { get; set; } = string.Empty;

    [JsonPropertyName("my_peer_id")]
    public long MyPeerId { get; set; }

    [JsonPropertyName("peer_id")]
    public long PeerId { get; set; }

    [JsonPropertyName("features")]
    public List<string> Features { get; set; } = new();

    [JsonPropertyName("tunnel")]
    public TunnelInfo? Tunnel { get; set; }

    [JsonPropertyName("stats")]
    public ConnectionStats? Stats { get; set; }

    [JsonPropertyName("loss_rate")]
    public double LossRate { get; set; }

    [JsonPropertyName("is_client")]
    public bool IsClient { get; set; }

    [JsonPropertyName("network_name")]
    public string NetworkName { get; set; } = string.Empty;

    [JsonPropertyName("is_closed")]
    public bool IsClosed { get; set; }
}

public class TunnelInfo
{
    [JsonPropertyName("tunnel_type")]
    public string TunnelType { get; set; } = string.Empty;

    [JsonPropertyName("local_addr")]
    public AddressInfo? LocalAddr { get; set; }

    [JsonPropertyName("remote_addr")]
    public AddressInfo? RemoteAddr { get; set; }
}

public class AddressInfo
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;
}

public class ConnectionStats
{
    [JsonPropertyName("rx_bytes")]
    public long RxBytes { get; set; }

    [JsonPropertyName("tx_bytes")]
    public long TxBytes { get; set; }

    [JsonPropertyName("rx_packets")]
    public long RxPackets { get; set; }

    [JsonPropertyName("tx_packets")]
    public long TxPackets { get; set; }

    [JsonPropertyName("latency_us")]
    public long LatencyUs { get; set; }

    public double LatencyMs => LatencyUs / 1000.0;
}

public class PeerRoutePair
{
    [JsonPropertyName("route")]
    public RouteInfo? Route { get; set; }

    [JsonPropertyName("peer")]
    public PeerInfo? Peer { get; set; }
}

public class ForeignNetworkSummary
{
    [JsonPropertyName("info_map")]
    public Dictionary<string, object> InfoMap { get; set; } = new();
}
