using Microsoft.Win32;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DarkHub.UI
{
    public class DnsResult
    {
        public string Name { get; set; }
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public double Latency { get; set; }
    }

    public class DNSBenchmarker
    {
        private Window _progressWindow;
        private TextBox _progressTextBox;
        private readonly Button _button;

        public DNSBenchmarker(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.DNSBenchmarkTitle);
            _progressWindow.Owner = owner;
        }

        public async Task BenchmarkDNSAsync()
        {
            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingDNSTest);

                var dnsList = new List<(string Name, string Primary, string Secondary)>
                {
                    ("Google DNS", "8.8.8.8", "8.8.4.4"),
                    ("Cloudflare", "1.1.1.1", "1.0.0.1"),
                    ("OpenDNS", "208.67.222.222", "208.67.220.220"),
                    ("Quad9", "9.9.9.9", "149.112.112.112"),
                    ("DNS Brasil 1", "177.128.247.77", "189.2.9.181"),
                    ("DNS Brasil 2", "181.217.154.102", "186.209.180.156"),
                    ("DNS Brasil 3", "45.65.173.61", "177.99.206.131"),
                    ("DNS Brasil 4", "187.102.222.46", "168.195.243.35"),
                    ("DNS Brasil 5", "181.191.162.239", "177.103.231.245"),
                    ("DNS Brasil 6", "187.44.169.62", "186.215.192.243"),
                    ("DNS EUA 1", "205.171.3.66", "204.9.214.118"),
                    ("DNS EUA 2", "172.64.36.143", "75.85.76.64"),
                    ("DNS EUA 3", "172.64.37.210", "76.72.180.231"),
                    ("DNS EUA 4", "66.92.224.2", "8.20.247.148"),
                    ("DNS EUA 5", "73.99.97.167", "65.220.16.3"),
                    ("DNS EUA 6", "5.183.101.189", "209.37.92.138"),
                    ("DNS EUA 7", "66.42.158.238", "45.90.28.108"),
                    ("DNS EUA 8", "161.97.239.94", "198.12.71.224"),
                    ("DNS EUA 9", "4.15.7.161", "47.32.37.109"),
                    ("DNS EUA 10", "38.111.51.157", "12.219.147.122"),
                    ("DNS EUA 11", "34.192.110.149", "68.106.134.45"),
                    ("DNS EUA 12", "45.33.45.43", "50.248.90.249"),
                    ("DNS EUA 13", "96.78.147.162", "209.244.104.183"),
                    ("DNS EUA 14", "96.249.1.153", "66.170.134.30"),
                    ("DNS EUA 15", "134.122.123.3", "98.43.240.178"),
                    ("DNS EUA 16", "50.224.158.130", "63.171.232.38"),
                    ("DNS EUA 17", "162.251.163.98", "148.77.107.17"),
                    ("DNS EUA 18", "156.154.71.41", "8.26.56.216"),
                    ("DNS EUA 19", "167.71.182.60", "8.8.8.8")
                };

                var results = new List<DnsResult>();
                var tasks = dnsList.Select(async dns =>
                {
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.TestingDNS, dns.Name));
                    double latency = await TestDNSLatency(dns.Primary);
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.LatencyResult, latency.ToString("F2")));
                    return new DnsResult
                    {
                        Name = dns.Name,
                        Primary = dns.Primary,
                        Secondary = dns.Secondary,
                        Latency = latency
                    };
                });

                results = (await Task.WhenAll(tasks)).OrderBy(r => r.Latency).ToList();

                await ShowResultsWindowAsync(results);
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorRunningDNSBenchmark, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorRunningDNSBenchmark, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _button.IsEnabled = true;
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Close()));
            }
        }

        private async Task ShowResultsWindowAsync(List<DnsResult> results)
        {
            var owner = _progressWindow.Owner;

            WindowState ownerState = WindowState.Normal;
            bool isOwnerVisible = false;

            await Task.Run(() =>
            {
                _progressWindow.Dispatcher.Invoke(() =>
                {
                    if (owner != null && owner.IsLoaded)
                    {
                        ownerState = owner.WindowState;
                        isOwnerVisible = owner.IsVisible;
                    }

                    var resultsWindow = WindowFactory.CreateWindow(
                        title: ResourceManagerHelper.Instance.DNSBenchmarkResultsTitle,
                        width: 800,
                        height: 600,
                        owner: owner,
                        isModal: false,
                        resizable: true
                    );

                    var grid = new Grid();
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                    var dataGrid = new DataGrid
                    {
                        AutoGenerateColumns = false,
                        IsReadOnly = true,
                        Background = WindowFactory.DefaultBackground,
                        Foreground = WindowFactory.DefaultTextForeground,
                        BorderBrush = WindowFactory.DefaultBorderBrush,
                        GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                        HorizontalGridLinesBrush = WindowFactory.DefaultBorderBrush,
                        VerticalGridLinesBrush = WindowFactory.DefaultBorderBrush,
                        AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(63, 65, 70)),
                        RowBackground = WindowFactory.DefaultBackground,
                        CanUserSortColumns = true,
                        CanUserResizeColumns = true,
                        HeadersVisibility = DataGridHeadersVisibility.Column,
                        RowHeaderWidth = 0,
                        SelectionMode = DataGridSelectionMode.Single,
                        SelectionUnit = DataGridSelectionUnit.FullRow,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden
                    };

                    dataGrid.ColumnHeaderStyle = WindowFactory.CreateDataGridHeaderStyle();
                    dataGrid.CellStyle = WindowFactory.CreateDataGridCellStyle();
                    dataGrid.RowStyle = WindowFactory.CreateDataGridRowStyle();

                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = ResourceManagerHelper.Instance.DNSNameColumn,
                        Binding = new Binding("Name"),
                        Width = 150
                    });
                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = ResourceManagerHelper.Instance.DNSPrimaryColumn,
                        Binding = new Binding("Primary"),
                        Width = 150
                    });
                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = ResourceManagerHelper.Instance.DNSSecondaryColumn,
                        Binding = new Binding("Secondary"),
                        Width = 150
                    });
                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = ResourceManagerHelper.Instance.DNSLatencyColumn,
                        Binding = new Binding("Latency") { StringFormat = "F2" },
                        Width = 150
                    });

                    dataGrid.ItemsSource = results;

                    var buttonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(10)
                    };

                    var configureButton = WindowFactory.CreateStyledButton(
                        ResourceManagerHelper.Instance.ConfigureSelectedDNSButton,
                        220,
                        async (s, e) =>
                        {
                            if (dataGrid.SelectedItem is DnsResult selectedItem)
                            {
                                await ConfigureDNS(selectedItem.Primary, selectedItem.Secondary);
                            }
                        }
                    );

                    buttonPanel.Children.Add(configureButton);
                    Grid.SetRow(buttonPanel, 0);
                    Grid.SetRow(dataGrid, 1);

                    grid.Children.Add(buttonPanel);
                    grid.Children.Add(dataGrid);

                    resultsWindow.Content = grid;

                    resultsWindow.Closed += (s, args) =>
                    {
                        if (owner != null && owner.IsLoaded)
                        {
                            if (isOwnerVisible)
                            {
                                owner.WindowState = ownerState;
                                owner.Activate();
                                owner.Focus();
                            }
                        }
                    };

                    resultsWindow.Show();
                });
            });
        }

        public async Task<double> TestDNSLatency(string dnsServer)
        {
            try
            {
                using var client = new UdpClient();
                var endpoint = new IPEndPoint(IPAddress.Parse(dnsServer), 53);
                var query = new byte[] { 0x00, 0x00, 0x01, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x77, 0x77, 0x77, 0x06, 0x67, 0x6f, 0x6f, 0x67, 0x6c, 0x65, 0x03, 0x63, 0x6f, 0x6d, 0x00, 0x00, 0x01, 0x00, 0x01 };

                var sw = Stopwatch.StartNew();
                await client.SendAsync(query, query.Length, endpoint);

                var receiveTask = client.ReceiveAsync();
                var timeoutTask = Task.Delay(5000);
                var completedTask = await Task.WhenAny(receiveTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    sw.Stop();
                    return double.MaxValue;
                }

                await receiveTask;
                sw.Stop();

                return sw.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao testar DNS {dnsServer}: {ex.Message}");
                return double.MaxValue;
            }
        }

        public async Task ConfigureDNS(string primaryDNS, string secondaryDNS)
        {
            try
            {
                string command = "netsh interface show interface";
                string output = await WindowFactory.ExecuteCommandWithOutputAsync(command, _progressTextBox);

                string adapterName = "";
                string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    if (line.Contains("Enabled") || line.Contains("Conectado"))
                    {
                        string[] parts = line.Split(new[] { "  ", "\t" }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            string name = parts[1].Trim();

                            if (name.Contains("Wi-Fi") ||
                                name.Contains("Ethernet") ||
                                name.Contains("Wireless") ||
                                name.Contains("Rede") ||
                                name.Contains("Network") ||
                                name.Contains("Conectado"))
                            {
                                adapterName = name;
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(adapterName))
                {
                    throw new Exception($"Não foi possível encontrar um adaptador de rede ativo.\nSaída do comando:\n{output}");
                }

                await WindowFactory.ExecuteCommandWithOutputAsync($"netsh interface ip set address name=\"{adapterName}\" dhcp", _progressTextBox);
                await WindowFactory.ExecuteCommandWithOutputAsync($"netsh interface ip set dns name=\"{adapterName}\" dhcp", _progressTextBox);
                await Task.Delay(2000);

                await WindowFactory.ExecuteCommandWithOutputAsync($"netsh interface ip set dns name=\"{adapterName}\" static {primaryDNS}", _progressTextBox);
                await Task.Delay(2000);

                await WindowFactory.ExecuteCommandWithOutputAsync($"netsh interface ip add dns name=\"{adapterName}\" {secondaryDNS} index=2", _progressTextBox);
                await Task.Delay(2000);

                await WindowFactory.ExecuteCommandWithOutputAsync("ipconfig /flushdns", _progressTextBox);

                command = $"netsh interface ip show dns name=\"{adapterName}\"";
                output = await WindowFactory.ExecuteCommandWithOutputAsync(command, _progressTextBox);

                command = "ipconfig /all";
                output = await WindowFactory.ExecuteCommandWithOutputAsync(command, _progressTextBox);

                if (!output.Contains(primaryDNS) || !output.Contains(secondaryDNS))
                {
                    string registryPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces";
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath, true))
                    {
                        if (key != null)
                        {
                            foreach (string subKeyName in key.GetSubKeyNames())
                            {
                                using (RegistryKey subKey = key.OpenSubKey(subKeyName, true))
                                {
                                    if (subKey != null)
                                    {
                                        string nameServer = $"{primaryDNS},{secondaryDNS}";
                                        subKey.SetValue("NameServer", nameServer, RegistryValueKind.String);
                                    }
                                }
                            }
                        }
                    }

                    await WindowFactory.ExecuteCommandWithOutputAsync("ipconfig /flushdns", _progressTextBox);
                }

                await Task.Run(() => MessageBox.Show(string.Format(ResourceManagerHelper.Instance.DNSConfiguredSuccess, adapterName, primaryDNS, secondaryDNS),
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(ResourceManagerHelper.Instance.ErrorConfiguringDNS, ex.Message);
                await Task.Run(() => MessageBox.Show(errorMessage, ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern int GetAdaptersInfo(IntPtr pAdapterInfo, ref int pOutBufLen);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern int SetInterfaceDnsSettings(string adapterName, string primaryDNS, string secondaryDNS);

        [StructLayout(LayoutKind.Sequential)]
        private struct IP_ADAPTER_INFO
        {
            public IntPtr Next;
            public int ComboIndex;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string AdapterName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 132)]
            public string Description;

            public uint AddressLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Address;

            public int Index;
            public uint Type;
            public uint DhcpEnabled;
            public IntPtr CurrentIpAddress;
            public IP_ADDR_STRING IpAddressList;
            public IP_ADDR_STRING GatewayList;
            public IP_ADDR_STRING DhcpServer;
            public bool HaveWins;
            public IP_ADDR_STRING PrimaryWinsServer;
            public IP_ADDR_STRING SecondaryWinsServer;
            public int LeaseObtained;
            public int LeaseExpires;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IP_ADDR_STRING
        {
            public IntPtr Next;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string IpAddress;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string IpMask;

            public int Context;
        }
    }
}