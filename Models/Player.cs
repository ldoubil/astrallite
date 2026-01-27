using System;
using System.Collections.Generic;
using AstralLite.Core;

namespace AstralLite.Models
{
    public class Player : ObservableObject
    {
        private string _instanceId = string.Empty;
        private string _name = string.Empty;
        private string _ping = string.Empty;
        private string _connectionType = string.Empty;
        private string _udpNatType = string.Empty;
        private string _tcpNatType = string.Empty;
        private string _transportSummary = string.Empty;
        private string _lossRate = string.Empty;

        /// <summary>
        /// ?????????InstanceId?
        /// </summary>
        public string InstanceId
        {
            get => _instanceId;
            set => SetProperty(ref _instanceId, value);
        }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string Ping
        {
            get => _ping;
            set => SetProperty(ref _ping, value);
        }

        /// <summary>
        /// ??????? ? ??
        /// </summary>
        public string ConnectionType
        {
            get => _connectionType;
            set => SetProperty(ref _connectionType, value);
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

        public string TransportSummary
        {
            get => _transportSummary;
            set
            {
                if (SetProperty(ref _transportSummary, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public string LossRate
        {
            get => _lossRate;
            set => SetProperty(ref _lossRate, value);
        }

        public string DisplayName
        {
            get
            {
                return Name;
            }
        }

        /// <summary>
        /// ????????
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

                return string.Join("", parts);
            }
        }
    }
}
