using System.Management;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;


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
            LoadDevices();
            StartDeviceWatcher();
        }

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


        // Sets the window size and position
        private void SetWindowProperties()
        {
            this.Width = 800;
            this.Height = 600;

            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void LoadDevices()
        {
            // Query and get the list of connected devices
            List<DeviceInfo> devices = GetConnectedDevices();

            DeviceListBox.Items.Clear();
            foreach (var device in devices)
            {
                DeviceListBox.Items.Add(new ListBoxItem { Content = device.ToString(), Tag = device });
            }
        }


        private List<DeviceInfo> GetConnectedDevices()
        {
            List<DeviceInfo> devices = new List<DeviceInfo>();

            // Query for connected USB devices
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

        private void StartDeviceWatcher()
        {
            // Watch for device insertions
            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity' AND TargetInstance.DeviceID LIKE 'USB%'"));
            insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInserted);
            insertWatcher.Start();

            // Watch for device removals
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
                    if (((DeviceInfo)((ListBoxItem)DeviceListBox.Items[i]).Tag).Port == port)
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
