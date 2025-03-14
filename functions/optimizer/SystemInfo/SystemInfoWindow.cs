using System.Management;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class SystemInfoWindow
    {
        private readonly Window _infoWindow;

        public SystemInfoWindow(Window owner)
        {
            _infoWindow = WindowFactory.CreateWindow(
                title: ResourceManagerHelper.Instance.SystemInfoTitle,
                width: 600,
                height: 500,
                owner: owner,
                isModal: true,
                resizable: false
            );

            InitializeUI();
        }

        private void InitializeUI()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var titleBlock = WindowFactory.CreateTitleText(ResourceManagerHelper.Instance.SystemInfoTitle);
            titleBlock.FontSize = 20;
            titleBlock.FontWeight = FontWeights.Bold;
            titleBlock.HorizontalAlignment = HorizontalAlignment.Center;
            titleBlock.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(titleBlock, 0);
            grid.Children.Add(titleBlock);

            var textBox = new TextBox
            {
                Text = GetSystemInfo() ?? ResourceManagerHelper.Instance.NoDataAvailable,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = WindowFactory.DefaultControlBackground,
                Foreground = WindowFactory.DefaultTextForeground,
                Margin = new Thickness(10),
                Padding = new Thickness(10),
                FontSize = 14,
                BorderBrush = WindowFactory.DefaultBorderBrush,
                BorderThickness = WindowFactory.DefaultBorderThickness
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            _infoWindow.Content = new Border
            {
                CornerRadius = new CornerRadius(10),
                Background = WindowFactory.DefaultBackground,
                Child = grid,
                Margin = new Thickness(2)
            };
        }

        public Task ShowDialogAsync()
        {
            return Task.Run(() => _infoWindow.Dispatcher.Invoke(() => _infoWindow.ShowDialog()));
        }

        private static string GetSystemInfo()
        {
            try
            {
                var systemInfo = new StringBuilder();

                var os = Environment.OSVersion;
                systemInfo.AppendLine($"OS: {os.VersionString}");

                var processorQuery = new ManagementObjectSearcher("SELECT Name, L2CacheSize, L3CacheSize FROM Win32_Processor");
                var processor = processorQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                systemInfo.AppendLine($"CPU: {processor?["Name"]?.ToString() ?? "Não encontrado"}");
                systemInfo.AppendLine($"Cache L2: {processor?["L2CacheSize"]?.ToString() ?? "Não encontrado"}");
                systemInfo.AppendLine($"Cache L3: {processor?["L3CacheSize"]?.ToString() ?? "Não encontrado"}");

                long totalMemory = 0;
                var memorySpeeds = new List<string>();
                var memoryQuery = new ManagementObjectSearcher("SELECT Capacity, Speed FROM Win32_PhysicalMemory");
                foreach (var mem in memoryQuery.Get().Cast<ManagementObject>())
                {
                    totalMemory += Convert.ToInt64(mem["Capacity"] ?? 0);
                    string? speed = mem["Speed"]?.ToString();
                    if (!string.IsNullOrEmpty(speed))
                    {
                        memorySpeeds.Add(speed);
                    }
                }
                var memorySpeedDisplay = memorySpeeds.Any() ? string.Join(", ", memorySpeeds.Distinct()) + " MHz" : "Não encontrado";
                systemInfo.AppendLine($"RAM Total: {totalMemory / (1024 * 1024 * 1024)} GB");
                systemInfo.AppendLine($"Ram Frenquency: {memorySpeedDisplay}");

                var gpuQuery = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
                var gpu = gpuQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                if (gpu != null)
                {
                    var gpuName = gpu["Name"]?.ToString() ?? "Não encontrado";
                    var gpuVram = gpu["AdapterRAM"];
                    string vramDisplay = gpuVram != null ? $"{Convert.ToInt64(gpuVram) / (1024 * 512):F2} MB" : "Não encontrado";
                    systemInfo.AppendLine($"GPU: {gpuName}");
                    systemInfo.AppendLine($"VRAM: {vramDisplay}");
                }
                else
                {
                    systemInfo.AppendLine("GPU: Não encontrada");
                }

                var motherboardQuery = new ManagementObjectSearcher("SELECT Product FROM Win32_BaseBoard");
                var motherboard = motherboardQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                systemInfo.AppendLine($"Mother Board: {motherboard?["Product"]?.ToString() ?? "Não encontrado"}");

                var biosQuery = new ManagementObjectSearcher("SELECT Version FROM Win32_BIOS");
                var bios = biosQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                systemInfo.AppendLine($"BIOS: {bios?["Version"]?.ToString() ?? "Não encontrado"}");

                return systemInfo.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorGettingSystemInfo, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}