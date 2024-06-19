using System.Windows;

namespace AssignShortcuts
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetWindowProperties();
        }

        // Sets the window size and position
        private void SetWindowProperties()
        {
            this.Width = 800;
            this.Height = 600;

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        // Event handler for device selection change
        private void DeviceListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Logic to handle device selection change
        }

        // Event handler for adding a shortcut
        private void AddShortcut_Click(object sender, RoutedEventArgs e)
        {
            // Logic to handle adding a shortcut
        }

        // Event handler for removing a shortcut
        private void RemoveShortcut_Click(object sender, RoutedEventArgs e)
        {
            // Logic to handle removing a shortcut
        }

        // Event handler for updating a shortcut
        private void UpdateShortcut_Click(object sender, RoutedEventArgs e)
        {
            // Logic to handle updating a shortcut
        }
    }
}
