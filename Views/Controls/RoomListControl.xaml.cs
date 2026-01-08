using AstralLite.Models;
using System.Windows;
using System.Windows.Input;
using WpfListBox = System.Windows.Controls.ListBox;

namespace AstralLite.Views.Controls
{
    public partial class RoomListControl : System.Windows.Controls.UserControl
    {
        public RoomListControl()
        {
            InitializeComponent();
        }

        private void RoomList_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not WpfListBox listBox || listBox.SelectedItem is not RoomConfiguration room) return;
            
            if (DataContext is ViewModels.MainViewModel viewModel)
            {
                viewModel.JoinRoomCommand?.Execute(room);
            }
        }
    }
}
