using System;
using System.Management;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.SQLite;
using System.IO;

namespace AssignShortcuts
{
    public partial class MainWindow : Window
    {
        private DeviceInfo selectedDevice;
        private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string DatabaseFileName = "device_names.db";
        private static readonly string DatabaseFullPath = Path.Combine(AppDataPath, DatabaseFileName);
        private static readonly string DatabaseConnectionString = "Data Source=" + DatabaseFullPath;

        public MainWindow()
        {
            InitializeComponent();
            SetWindowProperties();
            InitializeDatabase();
            LoadDevices();
            StartDeviceWatcher();
        }

        // Sets the window size and position
        private void SetWindowProperties()
        {
            this.Width = 800;
            this.Height = 600;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        // Loads the connected devices
        private void LoadDevices()
        {
            List<DeviceInfo> devices = GetConnectedDevices();
            DeviceListBox.Items.Clear();

            foreach (var device in devices)
            {
                device.Name = GetCustomDeviceName(device.Port) ?? device.Name;
                DeviceListBox.Items.Add(new ListBoxItem { Content = device.ToString(), Tag = device });
            }
        }

        // Queries the connected USB devices on the system
        private List<DeviceInfo> GetConnectedDevices()
        {
            List<DeviceInfo> devices = new List<DeviceInfo>();
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"Select * From Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");

            foreach (ManagementObject device in searcher.Get())
            {
                string deviceType = GetDeviceType(device["Description"]?.ToString());
                DeviceInfo deviceInfo = new DeviceInfo
                {
                    Name = device["Name"]?.ToString(),
                    Description = device["Description"]?.ToString(),
                    Port = device["PNPDeviceID"]?.ToString(),
                    DeviceType = deviceType
                };
                devices.Add(deviceInfo);
            }

            return devices;
        }

        // Determine the device type based on the description
        private string GetDeviceType(string description)
        {
            if (description.Contains("Keyboard"))
                return "Keyboard";
            if (description.Contains("Mouse"))
                return "Mouse";
            if (description.Contains("Printer"))
                return "Printer";

            return "Unknown";
        }

        // Start watching for device insertions and removals
        private void StartDeviceWatcher()
        {
            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.DeviceID LIKE 'USB%'"));
            insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInserted);
            insertWatcher.Start();

            ManagementEventWatcher removeWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.DeviceID LIKE 'USB%'"));
            removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemoved);
            removeWatcher.Start();
        }

        private void DeviceInserted(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            string description = instance["Description"]?.ToString();
            string deviceType = GetDeviceType(description);

            DeviceInfo deviceInfo = new DeviceInfo
            {
                Name = instance["Name"]?.ToString(),
                Description = description,
                Port = instance["PNPDeviceID"]?.ToString(),
                DeviceType = deviceType
            };

            Dispatcher.Invoke(() =>
            {
                DeviceListBox.Items.Add(new ListBoxItem { Content = deviceInfo.ToString(), Tag = deviceInfo });
                MessageBox.Show($"Device: {deviceInfo.ToString()} has been plugged in");
            });
        }

        private void DeviceRemoved(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            string port = instance["PNPDeviceID"]?.ToString();

            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < DeviceListBox.Items.Count; i++)
                {
                    ListBoxItem item = (ListBoxItem)DeviceListBox.Items[i];
                    DeviceInfo device = (DeviceInfo)item.Tag;
                    if (device.Port == port)
                    {
                        DeviceListBox.Items.RemoveAt(i);
                        break;
                    }
                }
            });
        }

        // Event handler for device selection change
        private void DeviceListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBoxItem selectedItem = (ListBoxItem)DeviceListBox.SelectedItem;
            selectedDevice = selectedItem?.Tag as DeviceInfo;
        }

        private void AddShortcut_Click(object sender, RoutedEventArgs e)
        {
            // Logic to handle adding a shortcut
        }

        private void RemoveShortcut_Click(object sender, RoutedEventArgs e)
        {
            // Logic to handle removing a shortcut
        }

        private void UpdateShortcut_Click(object sender, RoutedEventArgs e)
        {
            // Logic to handle updating a shortcut
        }

        private void ListenForInput_Click(object sender, RoutedEventArgs e)
        {
            ListeningStatusLabel.Visibility = Visibility.Visible;
            this.KeyDown += MainWindow_KeyDown;
            this.MouseMove += MainWindow_MouseMove;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            IdentifyDevice("Keyboard");
        }

        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            IdentifyDevice("Mouse");
        }

        private void IdentifyDevice(string deviceType)
        {
            this.KeyDown -= MainWindow_KeyDown;
            this.MouseMove -= MainWindow_MouseMove;
            ListeningStatusLabel.Visibility = Visibility.Collapsed;
            IdentifiedDeviceLabel.Text = $"Identified Device: {deviceType}";
            SelectDeviceButton.Visibility = Visibility.Visible;
            RenameDeviceButton.Visibility = Visibility.Visible;
        }

        private void SelectDevice_Click(object sender, RoutedEventArgs e)
        {
            DeviceListBox.SelectedItem = selectedDevice;
        }

        private void RenameDevice_Click(object sender, RoutedEventArgs e)
        {
            RenameDeviceLabel.Text = $"Renaming: {selectedDevice}";
            RenameDeviceTextBox.Text = string.Empty;
        }

        private void SaveRename_Click(object sender, RoutedEventArgs e)
        {
            string newName = RenameDeviceTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(newName) && selectedDevice != null)
            {
                SaveCustomDeviceName(selectedDevice.Port, newName);
                LoadDevices();
            }
        }

        // Initialize the SQLite database
        private void InitializeDatabase()
        {
            if (!File.Exists(DatabaseFullPath))
            {
                SQLiteConnection.CreateFile(DatabaseFullPath);
            }

            using (SQLiteConnection connection = new SQLiteConnection(DatabaseConnectionString))
            {
                connection.Open();
                string createTableQuery = "CREATE TABLE IF NOT EXISTS DeviceNames (Port TEXT PRIMARY KEY, Name TEXT)";
                using (SQLiteCommand command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // Save custom device name to the database
        private void SaveCustomDeviceName(string port, string name)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DatabaseConnectionString))
            {
                connection.Open();
                string insertQuery = "INSERT OR REPLACE INTO DeviceNames (Port, Name) VALUES (@Port, @Name)";
                using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                {
                    command.Parameters.AddWithValue("@Port", port);
                    command.Parameters.AddWithValue("@Name", name);
                    command.ExecuteNonQuery();
                }
            }
        }

        // Get custom device name from the database
        private string GetCustomDeviceName(string port)
        {
            using (SQLiteConnection connection = new SQLiteConnection(DatabaseConnectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Name FROM DeviceNames WHERE Port = @Port";
                using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Port", port);
                    var result = command.ExecuteScalar();
                    return result?.ToString();
                }
            }
        }
    }

    // Class to hold detailed device information
    public class DeviceInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Port { get; set; }
        public string DeviceType { get; set; }

        public override string ToString()
        {
            return $"{Name} | {Description} | {DeviceType}";
        }
    }
}
