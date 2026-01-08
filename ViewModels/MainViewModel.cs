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
                ConnectionStatusVisibility = Visibility.Visible;
                ActionButtonText = "离开";
                ActionButtonVisibility = Visibility.Visible;
                RoomListVisibility = Visibility.Collapsed;
                PlayerListVisibility = Visibility.Visible;
                PlayerNameEnabled = false;
                SelectedRoom = room;

                // 模拟玩家列表
                _allPlayers.Clear();
                _allPlayers.Add(new Player { Name = PlayerName, Ping = "0ms" });
                _allPlayers.Add(new Player { Name = "Player2", Ping = "15ms" });
                _allPlayers.Add(new Player { Name = "Player3", Ping = "23ms" });

                Players.Clear();
                foreach (var player in _allPlayers) Players.Add(player);

                MessageBox.Show($"成功加入房间: {room.RoomName}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加入房间失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
                NetworkStatus = string.Empty;
                ConnectionStatusVisibility = Visibility.Collapsed;
                ActionButtonVisibility = Visibility.Collapsed;
                RoomListVisibility = Visibility.Visible;
                PlayerListVisibility = Visibility.Collapsed;
                PlayerNameEnabled = true;
                SelectedRoom = null;

                Players.Clear();
                _allPlayers.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"离开房间失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnNetworkInfoUpdated(object? sender, Dictionary<string, string> info)
        {
            // 在 UI 线程上更新网络状态
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                if (info.Count == 0)
                {
                    NetworkStatus = "正在连接...";
                }
                else
                {
                    var status = new System.Text.StringBuilder();
                    status.AppendLine($"[{DateTime.Now:HH:mm:ss}] 网络状态:");
                    foreach (var kv in info)
                    {
                        status.AppendLine($"  {kv.Key}: {kv.Value}");
                    }
                    NetworkStatus = status.ToString();
                    
                    // 输出到调试窗口
                    System.Diagnostics.Debug.WriteLine(NetworkStatus);
                }
            });
        }
    }
}
