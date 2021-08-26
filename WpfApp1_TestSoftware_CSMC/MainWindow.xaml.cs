using System.Windows;
using System.Windows.Controls;

namespace WpfApp1_TestSoftware_CSMC
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (true)
            {
                softWareMainWindow.Width = 800;
                softWareMainWindow.Height = 600;
            }
        }

        private void SerialBaseUserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }
}
