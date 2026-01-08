using AstralLite.Core;
using AstralLite.Models;
using AstralLite.Models.Network;
using AstralLite.Services;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace AstralLite.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private string _playerName = GetDefaultPlayerName();
        private string _ipAddress = "未连接";
        private bool _isConnected;
        private Visibility _connectionStatusVisibility = Visibility.Collapsed;
        private Visibility _actionButtonVisibility = Visibility.Collapsed;
        private string _actionButtonText = "加入";
        private Visibility _roomListVisibility = Visibility.Visible;
        private Visibility _playerListVisibility = Visibility.Collapsed;
        private bool _playerNameEnabled = true;
        private string _searchText = string.Empty;
        private RoomConfiguration? _selectedRoom;
        private string _networkStatus = string.Empty;
        private string _connectionStatus = "未连接";
        private bool _isNetworkInfoReceived = false;
        private bool _firewallEnabled;
        private bool _isFirewallUpdating = false;

        public MainViewModel()
        {
            InitializeCommands();
            
            // 订阅解析后的网络信息更新事件
            NetworkService.Instance.ParsedNetworkInfoUpdated += OnParsedNetworkInfoUpdated;
            RefreshFirewallStatus();
        }
        private static string GetDefaultPlayerName()
        {
            var name = Environment.UserName;
            return string.IsNullOrWhiteSpace(name) ? "Player" : name;
        }

        private void RefreshFirewallStatus()
        {
            try
            {
                _isFirewallUpdating = true;
                var enabled = FirewallStatusService.IsFirewallEnabled();
                SetProperty(ref _firewallEnabled, enabled);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Firewall status check failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isFirewallUpdating = false;
            }
        }

        private void ApplyFirewallSetting(bool enabled)
        {
            try
            {
                _isFirewallUpdating = true;
                FirewallStatusService.SetFirewallEnabled(enabled);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Firewall update failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isFirewallUpdating = false;
                RefreshFirewallStatus();
            }
        }

        private enum TransportKind
        {
            Udp4,
            Udp6,
            Tcp4,
            Tcp6
        }

        private static string BuildTransportSummary(PeerInfo? peer)
        {
            if (peer == null || peer.Connections == null || peer.Connections.Count == 0)
            {
                return string.Empty;
            }

            var kinds = new HashSet<TransportKind>();
            foreach (var conn in peer.Connections)
            {
                if (conn.IsClosed)
                {
                    continue;
                }

                var kind = DetectTransport(conn);
                if (kind.HasValue)
                {
                    kinds.Add(kind.Value);
                }
            }

            if (kinds.Count == 0)
            {
                return string.Empty;
            }

            var ordered = kinds.OrderBy(GetTransportWeight).ToList();
            var primary = ordered[0];
            if (ordered.Count == 1)
            {
                return TransportToShort(primary);
            }

            var backups = string.Join(" ", ordered.Skip(1).Select(TransportToShort));
            return $"{TransportToShort(primary)} / {backups}";
        }

        private static TransportKind? DetectTransport(ConnectionInfo conn)
        {
            if (conn.Tunnel == null)
            {
                return null;
            }

            var tunnelType = conn.Tunnel.TunnelType?.ToLowerInvariant();
            if (tunnelType != "udp" && tunnelType != "tcp")
            {
                return null;
            }

            var localHost = GetHost(conn.Tunnel.LocalAddr?.Url);
            var remoteHost = GetHost(conn.Tunnel.RemoteAddr?.Url);
            var host = PickBestHost(remoteHost, localHost);

            if (!IPAddress.TryParse(host, out var ip))
            {
                if (!IPAddress.TryParse(localHost, out ip))
                {
                    return tunnelType == "udp" ? TransportKind.Udp4 : TransportKind.Tcp4;
                }
            }

            var isV6 = ip.AddressFamily == AddressFamily.InterNetworkV6;
            if (tunnelType == "udp")
            {
                return isV6 ? TransportKind.Udp6 : TransportKind.Udp4;
            }

            return isV6 ? TransportKind.Tcp6 : TransportKind.Tcp4;
        }

        private static string GetHost(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                ? uri.Host
                : string.Empty;
        }

        private static string PickBestHost(string remoteHost, string localHost)
        {
            if (!IsWildcardHost(remoteHost))
            {
                return remoteHost;
            }

            if (!IsWildcardHost(localHost))
            {
                return localHost;
            }

            return string.IsNullOrWhiteSpace(remoteHost) ? localHost : remoteHost;
        }

        private static bool IsWildcardHost(string host)
        {
            return string.IsNullOrWhiteSpace(host) || host == "0.0.0.0" || host == "::";
        }

        private static int GetTransportWeight(TransportKind kind)
        {
            return kind switch
            {
                TransportKind.Udp4 => 0,
                TransportKind.Udp6 => 1,
                TransportKind.Tcp4 => 2,
                TransportKind.Tcp6 => 3,
                _ => 100
            };
        }

        private static string TransportToShort(TransportKind kind)
        {
            return kind switch
            {
                TransportKind.Udp4 => "udp4",
                TransportKind.Udp6 => "udp6",
                TransportKind.Tcp4 => "tcp4",
                TransportKind.Tcp6 => "tcp6",
                _ => kind.ToString().ToLowerInvariant()
            };
        }



        #region Properties

        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public Visibility ConnectionStatusVisibility
        {
            get => _connectionStatusVisibility;
            set => SetProperty(ref _connectionStatusVisibility, value);
        }

        public Visibility ActionButtonVisibility
        {
            get => _actionButtonVisibility;
            set => SetProperty(ref _actionButtonVisibility, value);
        }

        public string ActionButtonText
        {
            get => _actionButtonText;
            set => SetProperty(ref _actionButtonText, value);
        }

        public Visibility RoomListVisibility
        {
            get => _roomListVisibility;
            set => SetProperty(ref _roomListVisibility, value);
        }

        public Visibility PlayerListVisibility
        {
            get => _playerListVisibility;
            set => SetProperty(ref _playerListVisibility, value);
        }

        public bool PlayerNameEnabled
        {
            get => _playerNameEnabled;
            set => SetProperty(ref _playerNameEnabled, value);
        }

        
        public bool FirewallEnabled
        {
            get => _firewallEnabled;
            set
            {
                if (SetProperty(ref _firewallEnabled, value) && !_isFirewallUpdating)
                {
                    ApplyFirewallSetting(value);
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    if (PlayerListVisibility == Visibility.Visible)
                    {
                        FilterPlayers();
                    }
                }
            }
        }

        private ObservableCollection<Player> _filteredPlayers = new();
        public ObservableCollection<Player> FilteredPlayers => _filteredPlayers;

        public string NetworkStatus
        {
            get => _networkStatus;
            set => SetProperty(ref _networkStatus, value);
        }

        /// <summary>
        /// 连接状态文本（连接中/已连接）
        /// </summary>
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }

        /// <summary>
        /// 所有房间列表（直接绑定到 RoomConfigurationList）
        /// </summary>
        public ObservableCollection<RoomConfiguration> AllRooms => RoomConfigurationList.Rooms;

        /// <summary>
        /// 过滤后的房间列表
        /// </summary>
        public ObservableCollection<RoomConfiguration> FilteredRooms { get; } = new();

        /// <summary>
        /// 当前选中的房间
        /// </summary>
        public RoomConfiguration? SelectedRoom
        {
            get => _selectedRoom;
            set => SetProperty(ref _selectedRoom, value);
        }

        /// <summary>
        /// 所有分组列表
        /// </summary>
        public IEnumerable<string> Groups => RoomConfigurationList.GetAllGroups();

        public ObservableCollection<Player> Players { get; } = new();

        #endregion

        #region Commands

        public ICommand? JoinRoomCommand { get; private set; }
        public ICommand? LeaveRoomCommand { get; private set; }

        #endregion

        private void InitializeCommands()
        {
            JoinRoomCommand = new RelayCommand<RoomConfiguration>(JoinRoom, _ => !IsConnected && !string.IsNullOrWhiteSpace(PlayerName));
            LeaveRoomCommand = new RelayCommand(LeaveRoom, () => IsConnected);
            
            // 初始化房间列表
            FilterRooms();
        }

        private void FilterRooms()
        {
            FilteredRooms.Clear();

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var room in AllRooms)
                {
                    FilteredRooms.Add(room);
                }
            }
            else
            {
                var search = SearchText.ToLower();
                foreach (var room in AllRooms.Where(r =>
                    r.RoomName.ToLower().Contains(search) ||
                    r.GroupName.ToLower().Contains(search) ||
                    r.TestIp.ToLower().Contains(search)))
                {
                    FilteredRooms.Add(room);
                }
            }
        }

        private void FilterPlayers()
        {
            _filteredPlayers.Clear();
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                foreach (var p in Players)
                    _filteredPlayers.Add(p);
            }
            else
            {
                var search = SearchText.ToLower();
                foreach (var p in Players.Where(p => p.Name.ToLower().Contains(search)))
                    _filteredPlayers.Add(p);
            }
        }

        private void JoinRoom(RoomConfiguration? room)
        {
            if (room == null || string.IsNullOrWhiteSpace(PlayerName))
            {
                MessageBox.Show("请选择房间并输入你的名字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 动态拼接 hostname 配置
                var configWithHostname = $"hostname = \"{PlayerName}\"\n{room.ServerConfig}";

                // 使用 NetworkService 连接（会自动处理已连接的情况）
                NetworkService.Instance.Connect(configWithHostname);

                IsConnected = true;
                IpAddress = room.TestIp;
                ConnectionStatus = "连接中...";
                _isNetworkInfoReceived = false;
                ConnectionStatusVisibility = Visibility.Visible;
                ActionButtonText = "离开";
                ActionButtonVisibility = Visibility.Visible;
                RoomListVisibility = Visibility.Collapsed;
                PlayerListVisibility = Visibility.Visible;
                PlayerNameEnabled = false;
                SelectedRoom = room;

                // 清空玩家列表，等待网络信息更新
                Players.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加入房间失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // 恢复状态
                IsConnected = false;
                ConnectionStatus = "未连接";
                ConnectionStatusVisibility = Visibility.Collapsed;
            }
        }

        private void LeaveRoom()
        {
            try
            {
                // 使用 NetworkService 断开连接（会自动停止监控）
                NetworkService.Instance.Disconnect();

                IsConnected = false;
                IpAddress = "未连接";
                ConnectionStatus = "未连接";
                NetworkStatus = string.Empty;
                _isNetworkInfoReceived = false;
                ConnectionStatusVisibility = Visibility.Collapsed;
                ActionButtonVisibility = Visibility.Collapsed;
                RoomListVisibility = Visibility.Visible;
                PlayerListVisibility = Visibility.Collapsed;
                PlayerNameEnabled = true;
                SelectedRoom = null;

                Players.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"离开房间失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnParsedNetworkInfoUpdated(object? sender, Dictionary<string, NetworkInfo> parsedInfo)
        {
            // 在 UI 线程上更新网络状态和玩家列表
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (parsedInfo.Count == 0)
                {
                    // 网络信息为空，显示"连接中"
                    ConnectionStatus = "连接中...";
                    _isNetworkInfoReceived = false;
                }
                else
                {
                    // 收到网络信息，显示"已连接"
                    if (!_isNetworkInfoReceived)
                    {
                        ConnectionStatus = "已连接";
                        _isNetworkInfoReceived = true;
                    }

                    // 更新玩家列表
                    UpdatePlayerList(parsedInfo);

                    // 更新调试信息（简化）
                    var status = new System.Text.StringBuilder();
                    
                    foreach (var (networkName, info) in parsedInfo)
                    {
                        if (info.MyNodeInfo != null)
                        {
                            status.AppendLine($"主机: {info.MyNodeInfo.Hostname}");
                        }
                        status.AppendLine($"玩家数: {Players.Count}");
                    }
                    
                    NetworkStatus = status.ToString();
                }
            });
        }

        /// <summary>
        /// 根据网络信息中的 peer_route_pairs 动态更新玩家列表（增量更新）
        /// </summary>
        private void UpdatePlayerList(Dictionary<string, NetworkInfo> parsedInfo)
        {
            // 收集当前在线的玩家 InstanceId 集合
            var currentInstanceIds = new HashSet<string>();
            
            // 确保本地玩家存在（InstanceId = "local" 表示本地玩家）
            const string localInstanceId = "local";
            var localPlayer = Players.FirstOrDefault(p => p.InstanceId == localInstanceId);
            if (localPlayer == null)
            {
                Players.Insert(0, new Player
                {
                    InstanceId = localInstanceId,
                    Name = "本机",
                    Ping = "0ms",
                    UdpNatType = string.Empty,
                    TcpNatType = string.Empty,
                    TransportSummary = string.Empty
                });
            }
            else
            {
                // 更新本地玩家信息（名称可能改变）
                localPlayer.Name = "本机";
                localPlayer.Ping = "0ms";
                localPlayer.TransportSummary = string.Empty;
            }
            currentInstanceIds.Add(localInstanceId); // 本地玩家标记为在线

            // 遍历网络信息，更新或添加远程玩家
            foreach (var (networkName, info) in parsedInfo)
            {
                if (info.PeerRoutePairs == null || info.PeerRoutePairs.Count == 0)
                {
                    continue;
                }

                foreach (var peerRoutePair in info.PeerRoutePairs)
                {
                    var route = peerRoutePair.Route;
                    var peer = peerRoutePair.Peer;
                    var transportSummary = BuildTransportSummary(peer);

                    // 跳过没有路由信息的节点
                    if (route == null)
                    {
                        continue;
                    }

                    // 跳过 ipv4_addr 为空的节点
                    if (route.Ipv4Addr == null)
                    {
                        continue;
                    }

                    string instanceId = route.InstanceId;
                    if (string.IsNullOrEmpty(instanceId))
                    {
                        continue; // 跳过没有实例ID的节点
                    }
                    
                    currentInstanceIds.Add(instanceId); // 标记为在线

                    string playerName = route.Hostname ?? $"Peer-{route.PeerId}";
                    string ping = "N/A";

                    // 优先从 peer 的连接信息获取延迟
                    if (peer?.Connections != null && peer.Connections.Count > 0)
                    {
                        var conn = peer.Connections.FirstOrDefault(c => !c.IsClosed);
                        if (conn?.Stats != null)
                        {
                            ping = $"{conn.Stats.LatencyMs:F0}ms";
                        }
                    }
                    // 如果没有连接信息，使用路由的延迟
                    else if (route.PathLatency > 0)
                    {
                        ping = $"{route.PathLatency}ms";
                    }

                    // 根据 route.cost 判断连接类型
                    string connectionType = route.Cost == 1 ? "直连" : "中转";

                    // 获取 NAT 类型信息
                    string udpNatType = string.Empty;
                    string tcpNatType = string.Empty;
                    
                    if (route.StunInfo != null)
                    {
                        if (route.StunInfo.UdpNatType > 0)
                        {
                            udpNatType = NatTypeHelper.GetNatTypeName(route.StunInfo.UdpNatType);
                        }
                        
                        if (route.StunInfo.TcpNatType > 0)
                        {
                            tcpNatType = NatTypeHelper.GetNatTypeName(route.StunInfo.TcpNatType);
                        }
                    }

                    // 查找是否已存在该玩家
                    var existingPlayer = Players.FirstOrDefault(p => p.InstanceId == instanceId);
                    
                    if (existingPlayer != null)
                    {
                        // 玩家已存在，只更新属性（不重绘）
                        existingPlayer.Name = playerName;
                        existingPlayer.Ping = ping;
                        existingPlayer.ConnectionType = connectionType;
                        existingPlayer.UdpNatType = udpNatType;
                        existingPlayer.TcpNatType = tcpNatType;
                        existingPlayer.TransportSummary = transportSummary;
                    }
                    else
                    {
                        // 新玩家，添加到列表
                        Players.Add(new Player
                        {
                            InstanceId = instanceId,
                            Name = playerName,
                            Ping = ping,
                            ConnectionType = connectionType,
                            UdpNatType = udpNatType,
                            TcpNatType = tcpNatType,
                            TransportSummary = transportSummary
                        });
                    }
                }
            }

            // 移除已离线的玩家（不在 currentInstanceIds 中的）
            for (int i = Players.Count - 1; i >= 0; i--)
            {
                if (!currentInstanceIds.Contains(Players[i].InstanceId))
                {
                    Players.RemoveAt(i);
                }
            }
        }
    }
}

