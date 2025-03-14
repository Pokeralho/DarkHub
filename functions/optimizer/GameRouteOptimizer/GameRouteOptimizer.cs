using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DarkHub.UI
{
    public class GameRouteOptimizer
    {
        private Window _progressWindow;
        private TextBox _progressTextBox;
        private readonly Button _button;

        public GameRouteOptimizer(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.OptimizingGameRouteTitle);
            _progressWindow.Owner = owner;
        }

        public async Task OptimizeGameRouteAsync()
        {
            _button.IsEnabled = false;

            try
            {
                string gameServerIP = await ShowIpInputDialogAsync();
                if (string.IsNullOrEmpty(gameServerIP))
                {
                    _button.IsEnabled = true;
                    return;
                }

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingGameDNSOptimization);

                var dnsList = new[]
                {
                    new { Name = "Cloudflare Gaming", Primary = "1.1.1.1", Secondary = "1.0.0.1" },
                    new { Name = "Google DNS", Primary = "8.8.8.8", Secondary = "8.8.4.4" },
                    new { Name = "OpenDNS Gaming", Primary = "208.67.222.222", Secondary = "208.67.220.220" },
                    new { Name = "Quad9 Gaming", Primary = "9.9.9.9", Secondary = "149.112.112.112" },
                    new { Name = "Level3 Gaming", Primary = "209.244.0.3", Secondary = "209.244.0.4" },
                    new { Name = "Verisign DNS", Primary = "64.6.64.6", Secondary = "64.6.65.6" },
                    new { Name = "DNS.Watch", Primary = "84.200.69.80", Secondary = "84.200.70.40" },
                    new { Name = "Comodo Secure DNS", Primary = "8.26.56.26", Secondary = "8.20.247.20" },
                    new { Name = "Norton ConnectSafe", Primary = "199.85.126.10", Secondary = "199.85.127.10" },
                    new { Name = "GreenTeamDNS", Primary = "81.218.119.11", Secondary = "209.88.198.133" },
                    new { Name = "SafeDNS", Primary = "195.46.39.39", Secondary = "195.46.39.40" },
                    new { Name = "OpenNIC", Primary = "69.195.152.204", Secondary = "23.94.18.178" },
                    new { Name = "SmartViper", Primary = "208.76.50.50", Secondary = "208.76.51.51" },
                    new { Name = "Dyn", Primary = "216.146.35.35", Secondary = "216.146.36.36" },
                    new { Name = "FreeDNS", Primary = "37.235.1.174", Secondary = "37.235.1.177" },
                    new { Name = "Alternate DNS", Primary = "198.101.242.72", Secondary = "23.253.163.53" },
                    new { Name = "Yandex.DNS", Primary = "77.88.8.8", Secondary = "77.88.8.1" },
                    new { Name = "UncensoredDNS", Primary = "91.239.100.100", Secondary = "89.233.43.71" },
                    new { Name = "Hurricane Electric", Primary = "74.82.42.42", Secondary = "204.9.214.118" },
                    new { Name = "puntCAT", Primary = "109.69.8.51", Secondary = "205.171.3.66" },
                    new { Name = "Neustar", Primary = "156.154.70.1", Secondary = "156.154.71.1" },
                    new { Name = "DNS Brasil 1", Primary = "177.128.247.77", Secondary = "189.2.9.181" },
                    new { Name = "DNS Brasil 2", Primary = "181.217.154.102", Secondary = "186.209.180.156" },
                    new { Name = "DNS Brasil 3", Primary = "45.65.173.61", Secondary = "177.99.206.131" },
                    new { Name = "DNS Brasil 4", Primary = "187.102.222.46", Secondary = "168.195.243.35" },
                    new { Name = "DNS Brasil 5", Primary = "181.191.162.239", Secondary = "177.103.231.245" },
                    new { Name = "DNS Brasil 6", Primary = "187.44.169.62", Secondary = "186.215.192.243" },
                    new { Name = "DNS EUA 1", Primary = "172.64.36.143", Secondary = "75.85.76.64" },
                    new { Name = "DNS EUA 2", Primary = "172.64.37.210", Secondary = "76.72.180.231" },
                    new { Name = "DNS EUA 3", Primary = "66.92.224.2", Secondary = "8.20.247.148" },
                    new { Name = "DNS EUA 4", Primary = "73.99.97.167", Secondary = "65.220.16.3" },
                    new { Name = "DNS EUA 5", Primary = "5.183.101.189", Secondary = "209.37.92.138" },
                    new { Name = "DNS EUA 6", Primary = "66.42.158.238", Secondary = "45.90.28.108" },
                    new { Name = "DNS EUA 7", Primary = "161.97.239.94", Secondary = "198.12.71.224" },
                    new { Name = "DNS EUA 8", Primary = "4.15.7.161", Secondary = "47.32.37.109" },
                    new { Name = "DNS EUA 9", Primary = "38.111.51.157", Secondary = "12.219.147.122" },
                    new { Name = "DNS EUA 10", Primary = "34.192.110.149", Secondary = "68.106.134.45" },
                    new { Name = "DNS EUA 11", Primary = "45.33.45.43", Secondary = "50.248.90.249" },
                    new { Name = "DNS EUA 12", Primary = "96.78.147.162", Secondary = "209.244.104.183" },
                    new { Name = "DNS EUA 13", Primary = "96.249.1.153", Secondary = "66.170.134.30" },
                    new { Name = "DNS EUA 14", Primary = "134.122.123.3", Secondary = "98.43.240.178" },
                    new { Name = "DNS EUA 15", Primary = "50.224.158.130", Secondary = "63.171.232.38" },
                    new { Name = "DNS EUA 16", Primary = "162.251.163.98", Secondary = "148.77.107.17" },
                    new { Name = "DNS EUA 17", Primary = "156.154.71.41", Secondary = "8.26.56.216" },
                    new { Name = "DNS EUA 18", Primary = "167.71.182.60", Secondary = "8.8.8.8" }
                };

                var results = new List<DnsResult>();
                var semaphore = new SemaphoreSlim(5);
                var tasks = dnsList.Select(async dns =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.TestingDNS, dns.Name));
                        double latency = await TestDNSLatency(dns.Primary);
                        if (latency != double.MaxValue)
                        {
                            lock (results)
                            {
                                results.Add(new DnsResult
                                {
                                    Name = dns.Name,
                                    Primary = dns.Primary,
                                    Secondary = dns.Secondary,
                                    Latency = latency
                                });
                            }
                            WindowFactory.AppendProgress(_progressTextBox, $"Concluído {dns.Name}: {latency:F2} ms");
                        }
                        else
                        {
                            WindowFactory.AppendProgress(_progressTextBox, $"{dns.Name} falhou ou atingiu timeout");
                        }
                    }
                    catch (Exception ex)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, $"Erro ao testar {dns.Name}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Close()));

                await ShowResultsWindowAsync(results);
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorOptimizingGameRoute, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorOptimizingGameRoute, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _button.IsEnabled = true;
            }
        }

        private async Task<string> ShowIpInputDialogAsync()
        {
            var owner = _progressWindow.Owner;

            WindowState ownerState = WindowState.Normal;
            bool isOwnerVisible = false;

            var tcs = new TaskCompletionSource<string>();

            await Task.Run(() =>
            {
                _progressWindow.Dispatcher.Invoke(() =>
                {
                    if (owner != null && owner.IsLoaded)
                    {
                        ownerState = owner.WindowState;
                        isOwnerVisible = owner.IsVisible;
                    }

                    var dialogWindow = WindowFactory.CreateWindow(
                        title: ResourceManagerHelper.Instance.GameServerIPTitle,
                        width: 400,
                        height: 220,
                        owner: owner,
                        isModal: true,
                        resizable: false
                    );
                    dialogWindow.WindowStyle = WindowStyle.ToolWindow;

                    var dialogGrid = new Grid { Margin = new Thickness(15) };
                    dialogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    dialogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    dialogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var label = new TextBlock
                    {
                        Text = ResourceManagerHelper.Instance.EnterGameServerIP,
                        Foreground = WindowFactory.DefaultTextForeground,
                        Margin = new Thickness(0, 0, 0, 10),
                        FontSize = WindowFactory.DefaultFontSize
                    };

                    var textBox = new TextBox
                    {
                        Height = 30,
                        FontSize = WindowFactory.DefaultFontSize,
                        Background = new SolidColorBrush(Color.FromRgb(45, 47, 52)),
                        Foreground = WindowFactory.DefaultTextForeground,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                        Margin = new Thickness(0, 0, 0, 15)
                    };

                    var dialogButtonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 5, 0, 0)
                    };

                    RoutedEventHandler emptyHandler = (s, args) => { };

                    var okButton = WindowFactory.CreateStyledButton(ResourceManagerHelper.Instance.OKButton, 80, emptyHandler);
                    var cancelButton = WindowFactory.CreateStyledButton(ResourceManagerHelper.Instance.CancelButton, 80, emptyHandler);

                    dialogButtonPanel.Children.Add(okButton);
                    dialogButtonPanel.Children.Add(cancelButton);

                    Grid.SetRow(label, 0);
                    Grid.SetRow(textBox, 1);
                    Grid.SetRow(dialogButtonPanel, 2);

                    dialogGrid.Children.Add(label);
                    dialogGrid.Children.Add(textBox);
                    dialogGrid.Children.Add(dialogButtonPanel);

                    dialogWindow.Content = dialogGrid;

                    string gameServerIP = "";
                    okButton.Click -= emptyHandler;
                    okButton.Click += (s, args) =>
                    {
                        gameServerIP = textBox.Text.Trim();
                        if (string.IsNullOrEmpty(gameServerIP))
                        {
                            MessageBox.Show(ResourceManagerHelper.Instance.InvalidIPWarning,
                                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        dialogWindow.DialogResult = true;
                        dialogWindow.Close();
                    };

                    cancelButton.Click -= emptyHandler;
                    cancelButton.Click += (s, args) =>
                    {
                        dialogWindow.DialogResult = false;
                        dialogWindow.Close();
                    };

                    dialogWindow.Closed += (s, args) =>
                    {
                        if (dialogWindow.DialogResult == true)
                            tcs.SetResult(gameServerIP);
                        else
                            tcs.SetResult(null);

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

                    dialogWindow.ShowDialog();
                });
            });

            return await tcs.Task;
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
                        title: ResourceManagerHelper.Instance.DNSResultsTitle,
                        width: 800,
                        height: 600,
                        owner: owner,
                        isModal: false,
                        resizable: true
                    );

                    var resultsGrid = new Grid();
                    resultsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    resultsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    resultsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    var resultsLabel = new TextBlock
                    {
                        Text = ResourceManagerHelper.Instance.DNSResultsLabel,
                        Foreground = WindowFactory.DefaultTextForeground,
                        Margin = new Thickness(10),
                        FontSize = WindowFactory.DefaultFontSize
                    };

                    var dataGrid = new DataGrid
                    {
                        AutoGenerateColumns = false,
                        IsReadOnly = true,
                        Background = new SolidColorBrush(Color.FromRgb(45, 47, 52)),
                        Foreground = WindowFactory.DefaultTextForeground,
                        BorderBrush = WindowFactory.DefaultBorderBrush,
                        GridLinesVisibility = DataGridGridLinesVisibility.All,
                        HorizontalGridLinesBrush = WindowFactory.DefaultBorderBrush,
                        VerticalGridLinesBrush = WindowFactory.DefaultBorderBrush,
                        RowBackground = WindowFactory.DefaultBackground,
                        AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(45, 47, 52)),
                        ColumnHeaderStyle = WindowFactory.CreateDataGridHeaderStyle(),
                        CellStyle = WindowFactory.CreateDataGridCellStyle(),
                        RowStyle = WindowFactory.CreateDataGridRowStyle()
                    };

                    dataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = ResourceManagerHelper.Instance.DNSNameColumn,
                        Binding = new Binding("Name"),
                        Width = 200
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
                        Width = 100
                    });

                    var resultsButtonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(10)
                    };

                    RoutedEventHandler emptyHandler = (s, args) => { };

                    var configureButton = WindowFactory.CreateStyledButton(
                        ResourceManagerHelper.Instance.ConfigureSelectedDNSButton,
                        200,
                        emptyHandler
                    );

                    configureButton.Click -= emptyHandler;
                    configureButton.Click += async (s, e) =>
                    {
                        if (dataGrid.SelectedItem is DnsResult selectedItem)
                        {
                            var dnsBenchmarker = new DNSBenchmarker(owner, _button);
                            await dnsBenchmarker.ConfigureDNS(selectedItem.Primary, selectedItem.Secondary);
                        }
                        else
                        {
                            MessageBox.Show(ResourceManagerHelper.Instance.SelectDNSWarning,
                                ResourceManagerHelper.Instance.WarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    };

                    resultsButtonPanel.Children.Add(configureButton);

                    Grid.SetRow(resultsLabel, 0);
                    Grid.SetRow(dataGrid, 1);
                    Grid.SetRow(resultsButtonPanel, 2);

                    resultsGrid.Children.Add(resultsLabel);
                    resultsGrid.Children.Add(dataGrid);
                    resultsGrid.Children.Add(resultsButtonPanel);

                    resultsWindow.Content = resultsGrid;

                    var sortedResults = results.OrderBy(r => r.Latency).ToList();
                    dataGrid.ItemsSource = sortedResults;

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

        private async Task<double> TestDNSLatency(string dnsServer)
        {
            var owner = _progressWindow.Owner;
            var dnsBenchmarker = new DNSBenchmarker(owner, _button);
            return await dnsBenchmarker.TestDNSLatency(dnsServer);
        }
    }
}