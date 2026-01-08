using AstralLite.Core;

namespace AstralLite.Models
{
    public class Player : ObservableObject
    {
        private string _name = string.Empty;
        private string _ping = string.Empty;
        private string _udpNatType = string.Empty;
        private string _tcpNatType = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Ping
        {
            get => _ping;
            set => SetProperty(ref _ping, value);
        }

        public string UdpNatType
        {
            get => _udpNatType;
            set
            {
                if (SetProperty(ref _udpNatType, value))
                {
                    OnPropertyChanged(nameof(TooltipText));
                }
            }
        }

        public string TcpNatType
        {
            get => _tcpNatType;
            set
            {
                if (SetProperty(ref _tcpNatType, value))
                {
                    OnPropertyChanged(nameof(TooltipText));
                }
            }
        }
        
        /// <summary>
        /// 获取工具提示文本
        /// </summary>
        public string TooltipText
        {
            get
            {
                if (string.IsNullOrEmpty(UdpNatType) && string.IsNullOrEmpty(TcpNatType))
                {
                    return string.Empty;
                }
                
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(UdpNatType))
                {
                    parts.Add($"UDP NAT: {UdpNatType}");
                }
                if (!string.IsNullOrEmpty(TcpNatType))
                {
                    parts.Add($"TCP NAT: {TcpNatType}");
                }
                
                return string.Join("\n", parts);
            }
        }
    }
}
