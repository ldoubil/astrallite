using AstralLite.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AstralLite.Views.Controls
{
    public partial class RoomListControl : System.Windows.Controls.UserControl
    {
        public RoomListControl()
        {
            InitializeComponent();
        }

        private void GameHeader_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;

            var tag = border.Tag?.ToString();
            System.Windows.Controls.ListBox? targetListBox = tag switch
            {
                "CSGO" => CSGOListBox,
                "PD2" => PD2ListBox,
                "MC" => MCListBox,
                _ => null
            };

            if (targetListBox == null) return;

            var collapseIcon = FindCollapseIcon(border);
            if (collapseIcon == null) return;

            if (targetListBox.Visibility == Visibility.Visible)
            {
                targetListBox.Visibility = Visibility.Collapsed;
                collapseIcon.Text = "?";
            }
            else
            {
                targetListBox.Visibility = Visibility.Visible;
                collapseIcon.Text = "¨‹";
            }
        }

        private TextBlock? FindCollapseIcon(Border border)
        {
            if (border.Child is not Grid grid) return null;

            foreach (var child in grid.Children)
            {
                if (child is TextBlock tb && tb.Tag?.ToString() == "CollapseIcon")
                    return tb;
            }
            return null;
        }

        private void RoomList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not System.Windows.Controls.ListBox listBox || listBox.SelectedItem is not Room room) return;
            
            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.JoinRoomCommand?.Execute(room);
            }
        }
    }
}
