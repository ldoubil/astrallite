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

        public MainViewModel()
        {
            InitializeRooms();
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
                    FilterRoomsAndPlayers();
                }
            }
        }

        public ObservableCollection<Room> CSGORooms { get; } = new();
        public ObservableCollection<Room> PD2Rooms { get; } = new();
        public ObservableCollection<Room> MCRooms { get; } = new();
        public ObservableCollection<Player> Players { get; } = new();

        private ObservableCollection<Room> _allCSGORooms = new();
        private ObservableCollection<Room> _allPD2Rooms = new();
        private ObservableCollection<Room> _allMCRooms = new();
        private ObservableCollection<Player> _allPlayers = new();

        #endregion

        #region Commands

        public ICommand? JoinRoomCommand { get; private set; }
        public ICommand? LeaveRoomCommand { get; private set; }
        public ICommand? MinimizeCommand { get; private set; }
        public ICommand? CloseCommand { get; private set; }

        #endregion

        private void InitializeCommands()
        {
            JoinRoomCommand = new RelayCommand<Room>(JoinRoom, _ => !IsConnected && !string.IsNullOrWhiteSpace(PlayerName));
            LeaveRoomCommand = new RelayCommand(LeaveRoom, () => IsConnected);
        }

        private void InitializeRooms()
        {
            _allCSGORooms = new ObservableCollection<Room>
            {
                new Room { Name = "Dust2 - Competitive", PlayerCount = "8/10", Ping = "15ms", IsHost = false, GameType = "CSGO" },
                new Room { Name = "Mirage - Casual", PlayerCount = "12/16", Ping = "22ms", IsHost = true, GameType = "CSGO" },
                new Room { Name = "Inferno - Deathmatch", PlayerCount = "6/20", Ping = "18ms", IsHost = false, GameType = "CSGO" },
                new Room { Name = "Nuke - Wingman", PlayerCount = "2/4", Ping = "12ms", IsHost = false, GameType = "CSGO" },
            };

            _allPD2Rooms = new ObservableCollection<Room>
            {
                new Room { Name = "Bank Heist - Overkill", PlayerCount = "3/4", Ping = "25ms", IsHost = true, GameType = "PD2" },
                new Room { Name = "Jewelry Store - Normal", PlayerCount = "2/4", Ping = "31ms", IsHost = false, GameType = "PD2" },
                new Room { Name = "Hoxton Breakout - Hard", PlayerCount = "4/4", Ping = "28ms", IsHost = false, GameType = "PD2" },
            };

            _allMCRooms = new ObservableCollection<Room>
            {
                new Room { Name = "Survival - 1.20.1", PlayerCount = "15/20", Ping = "8ms", IsHost = true, GameType = "MC" },
                new Room { Name = "Creative Build Server", PlayerCount = "8/30", Ping = "12ms", IsHost = false, GameType = "MC" },
                new Room { Name = "Skyblock Challenge", PlayerCount = "5/10", Ping = "10ms", IsHost = false, GameType = "MC" },
                new Room { Name = "Modded Adventure", PlayerCount = "3/8", Ping = "45ms", IsHost = false, GameType = "MC" },
            };

            RefreshRoomLists();
        }

        private void RefreshRoomLists()
        {
            CSGORooms.Clear();
            foreach (var room in _allCSGORooms) CSGORooms.Add(room);

            PD2Rooms.Clear();
            foreach (var room in _allPD2Rooms) PD2Rooms.Add(room);

            MCRooms.Clear();
            foreach (var room in _allMCRooms) MCRooms.Add(room);
        }

        private void JoinRoom(Room? room)
        {
            if (room == null || string.IsNullOrWhiteSpace(PlayerName))
            {
                MessageBox.Show("请输入你的名字", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsConnected = true;
            IpAddress = "10.0.0.1";
            ConnectionStatusVisibility = Visibility.Visible;
            ActionButtonText = "离开";
            ActionButtonVisibility = Visibility.Visible;
            RoomListVisibility = Visibility.Collapsed;
            PlayerListVisibility = Visibility.Visible;
            PlayerNameEnabled = false;

            _allPlayers.Clear();
            _allPlayers.Add(new Player { Name = PlayerName, Ping = "0ms" });
            _allPlayers.Add(new Player { Name = "ProGamer123", Ping = "15ms" });
            _allPlayers.Add(new Player { Name = "SnipeKing", Ping = "23ms" });
            _allPlayers.Add(new Player { Name = "NoobMaster", Ping = "8ms" });

            Players.Clear();
            foreach (var player in _allPlayers) Players.Add(player);
        }

        private void LeaveRoom()
        {
            var result = MessageBox.Show("确定要离开房间吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            IsConnected = false;
            IpAddress = "未连接";
            ConnectionStatusVisibility = Visibility.Collapsed;
            ActionButtonVisibility = Visibility.Collapsed;
            RoomListVisibility = Visibility.Visible;
            PlayerListVisibility = Visibility.Collapsed;
            PlayerNameEnabled = true;

            Players.Clear();
            _allPlayers.Clear();
        }

        private void FilterRoomsAndPlayers()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                RefreshRoomLists();
                Players.Clear();
                foreach (var player in _allPlayers) Players.Add(player);
                return;
            }

            var search = SearchText.ToLower();

            if (IsConnected)
            {
                var filtered = _allPlayers.Where(p => p.Name.ToLower().Contains(search)).ToList();
                Players.Clear();
                foreach (var player in filtered) Players.Add(player);
            }
            else
            {
                CSGORooms.Clear();
                foreach (var room in _allCSGORooms.Where(r => r.Name.ToLower().Contains(search)))
                    CSGORooms.Add(room);

                PD2Rooms.Clear();
                foreach (var room in _allPD2Rooms.Where(r => r.Name.ToLower().Contains(search)))
                    PD2Rooms.Add(room);

                MCRooms.Clear();
                foreach (var room in _allMCRooms.Where(r => r.Name.ToLower().Contains(search)))
                    MCRooms.Add(room);
            }
        }
    }
}
