using AstralLite.Core;
using AstralLite.Models;
using AstralLite.Models.Network;
using AstralLite.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace AstralLite.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private string _playerName = "Player";
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

        public MainViewModel()
        {
            InitializeCommands();
            
            // 订阅解析后的网络信息更新事件
            NetworkService.Instance.ParsedNetworkInfoUpdated += OnParsedNetworkInfoUpdated;
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

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterRooms();
                }
            }
        }

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

        private void JoinRoom(RoomConfiguration? room)
        {
            if (room == null || string.IsNullOrWhiteSpace(PlayerName))
            {
                MessageBox.Show("请选择房间并输入你的名字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 使用 NetworkService 连接（会自动处理已连接的情况）
                NetworkService.Instance.Connect(room.ServerConfig);

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

                MessageBox.Show($"正在连接到房间: {room.RoomName}", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var result = MessageBox.Show("确定要离开房间吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

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
                    
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] Network info is empty, status: 连接中");
                }
                else
                {
                    // 收到网络信息，显示"已连接"
                    if (!_isNetworkInfoReceived)
                    {
                        ConnectionStatus = "已连接";
                        _isNetworkInfoReceived = true;
                        System.Diagnostics.Debug.WriteLine("[MainViewModel] Network info received, status: 已连接");
                    }

                    // 更新玩家列表（使用 peers）
                    UpdatePlayerList(parsedInfo);

                    // 更新调试信息
                    var status = new System.Text.StringBuilder();
                    status.AppendLine($"[{DateTime.Now:HH:mm:ss}] 网络状态:");
                    
                    foreach (var (networkName, info) in parsedInfo)
                    {
                        status.AppendLine($"网络: {networkName}");
                        status.AppendLine($"  对等节点: {info.Peers.Count} 个");
                        
                        if (info.MyNodeInfo != null)
                        {
                            status.AppendLine($"  主机: {info.MyNodeInfo.Hostname}");
                            status.AppendLine($"  版本: {info.MyNodeInfo.Version}");
                        }
                    }
                    
                    NetworkStatus = status.ToString();
                    System.Diagnostics.Debug.WriteLine(NetworkStatus);
                }
            });
        }

        /// <summary>
        /// 根据网络信息中的 peers 更新玩家列表
        /// </summary>
        private void UpdatePlayerList(Dictionary<string, NetworkInfo> parsedInfo)
        {
            Players.Clear();

            // 首先添加本地玩家
            Players.Add(new Player 
            { 
                Name = PlayerName, 
                Ping = "0ms" 
            });

            foreach (var (networkName, info) in parsedInfo)
            {
                if (info.Peers == null || info.Peers.Count == 0)
                {
                    continue;
                }

                foreach (var peer in info.Peers)
                {
                    // 从 peer_route_pairs 中查找对应的路由信息以获取主机名
                    var route = info.PeerRoutePairs
                        .FirstOrDefault(p => p.Route?.PeerId == peer.PeerId)?.Route;

                    string playerName = route?.Hostname ?? $"Peer-{peer.PeerId}";
                    
                    // 跳过名字包含 "server" 的节点（不区分大小写）
                    if (playerName.Contains("server", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainViewModel] Skipping peer {peer.PeerId} - hostname contains 'server': {playerName}");
                        continue;
                    }

                    string ping = "N/A";

                    // 获取延迟信息
                    if (peer.Connections.Count > 0)
                    {
                        var conn = peer.Connections.FirstOrDefault(c => !c.IsClosed);
                        if (conn?.Stats != null)
                        {
                            ping = $"{conn.Stats.LatencyMs:F0}ms";
                        }
                    }
                    else if (route != null && route.PathLatency > 0)
                    {
                        ping = $"{route.PathLatency}ms";
                    }

                    Players.Add(new Player
                    {
                        Name = playerName,
                        Ping = ping
                    });
                }
            }

            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Updated player list: {Players.Count} players");
        }
    }
}
