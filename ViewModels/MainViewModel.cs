using AstralLite.Core;
using AstralLite.Models;
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

        public MainViewModel()
        {
            InitializeCommands();
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

        private ObservableCollection<Player> _allPlayers = new();

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
                // 使用房间配置启动网络
                AstralNat.StartNetwork(room.ServerConfig);

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
                // 停止网络连接
                AstralNat.StopAllNetworks();

                IsConnected = false;
                IpAddress = "未连接";
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
    }
}
