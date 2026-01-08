using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AstralLite.Models;

namespace AstralLite.Views.Controls
{
    public partial class PlayerListControl : System.Windows.Controls.UserControl
    {
        public PlayerListControl()
        {
            InitializeComponent();
        }

        private void PingText_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is TextBlock textBlock && textBlock.DataContext is Player player)
            {
                System.Diagnostics.Debug.WriteLine($"[PlayerList] 鼠标移入 '{player.Name}' 的延迟");
                
                // 检查是否有 NAT 信息
                bool hasUdpNat = !string.IsNullOrEmpty(player.UdpNatType);
                bool hasTcpNat = !string.IsNullOrEmpty(player.TcpNatType);
                
                if (hasUdpNat || hasTcpNat)
                {
                    // 设置标题
                    NatInfoTitle.Text = $"{player.Name} - {player.Ping}";
                    
                    // 设置 UDP NAT 信息
                    if (hasUdpNat)
                    {
                        UdpNatText.Text = player.UdpNatType;
                        UdpNatPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        UdpNatPanel.Visibility = Visibility.Collapsed;
                    }
                    
                    // 设置 TCP NAT 信息
                    if (hasTcpNat)
                    {
                        TcpNatText.Text = player.TcpNatType;
                        TcpNatPanel.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        TcpNatPanel.Visibility = Visibility.Collapsed;
                    }
                    
                    // 设置 Popup 的位置目标
                    NatInfoPopup.PlacementTarget = textBlock;
                    NatInfoPopup.HorizontalOffset = 15;
                    NatInfoPopup.VerticalOffset = 10;
                    
                    // 显示 Popup
                    NatInfoPopup.IsOpen = true;
                    
                    System.Diagnostics.Debug.WriteLine("  NAT 信息 Popup 已显示");
                }
            }
        }

        private void PingText_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // 隐藏 Popup
            NatInfoPopup.IsOpen = false;
            System.Diagnostics.Debug.WriteLine("[PlayerList] 鼠标离开，隐藏 NAT 信息 Popup");
        }

        private void PingText_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // 保持 Popup 跟随鼠标（可选）
            // 如果不需要跟随鼠标，可以删除这个方法和 XAML 中的 MouseMove 绑定
        }
    }
}
