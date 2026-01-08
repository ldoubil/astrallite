using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AstralLite.Views.Controls
{
    public partial class TitleBarControl : System.Windows.Controls.UserControl
    {
        public event EventHandler? MinimizeRequested;
        public event EventHandler? CloseRequested;

        public TitleBarControl()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                Window.GetWindow(this)?.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            MinimizeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
