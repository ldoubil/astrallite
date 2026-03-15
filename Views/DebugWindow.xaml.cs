using AstralLite.ViewModels;
using System.Windows;

namespace AstralLite.Views
{
    public partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            InitializeComponent();
            Loaded += DebugWindow_Loaded;
            Closed += DebugWindow_Closed;
        }

        private void DebugWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is DebugViewModel vm)
            {
                vm.Initialize();
            }
        }

        private void DebugWindow_Closed(object? sender, System.EventArgs e)
        {
            if (DataContext is DebugViewModel vm)
            {
                vm.Cleanup();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
