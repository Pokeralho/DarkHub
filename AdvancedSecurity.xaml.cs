using iText.IO.Font.Constants;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DarkHub
{
    public partial class AdvancedSecurity : Page
    {
        private Button _activeButton;
        private bool _isScanning;
        private bool _isMonitoring;
        private bool _isExploitMonitoring;
        private readonly List<string> _blockedDomainsHistory = new List<string>();
        private string _originalHostsBackupPath;
        private CancellationTokenSource _monitoringCancellationTokenSource;
        private CancellationTokenSource _exploitCancellationTokenSource;
        private readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public AdvancedSecurity()
        {
            InitializeComponent();
            _activeButton = btnMalwareDetector;
            NavigateToSection(btnMalwareDetector, null);
        }

        private static bool IsProcessAccessible(Process process)
        {
            try
            {
                var module = process.MainModule;
                return !string.IsNullOrEmpty(module?.FileName);
            }
            catch (Win32Exception)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool IsRunningAsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private void NavigateToSection(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            InitialMessagePanel.Visibility = Visibility.Collapsed;
            MalwarePanel.Visibility = Visibility.Collapsed;
            BehaviorPanel.Visibility = Visibility.Collapsed;
            LatencyPanel.Visibility = Visibility.Collapsed;
            TrackingPanel.Visibility = Visibility.Collapsed;
            PhishingPanel.Visibility = Visibility.Collapsed;
            ExploitPanel.Visibility = Visibility.Collapsed;

            switch (button.Tag.ToString())
            {
                case "Malware":
                    MalwarePanel.Visibility = Visibility.Visible;
                    break;

                case "Behavior":
                    BehaviorPanel.Visibility = Visibility.Visible;
                    break;

                case "Latency":
                    LatencyPanel.Visibility = Visibility.Visible;
                    LoadPeripherals();
                    break;

                case "Tracking":
                    TrackingPanel.Visibility = Visibility.Visible;
                    break;

                case "Phishing":
                    PhishingPanel.Visibility = Visibility.Visible;
                    break;

                case "Exploit":
                    ExploitPanel.Visibility = Visibility.Visible;
                    break;
            }

            if (_activeButton != null)
            {
                _activeButton.Style = (System.Windows.Style)FindResource("NavButtonStyle");
            }
            _activeButton = button;
            button.Style = (System.Windows.Style)FindResource("NavButtonStyle");
        }

        private List<string> GetProcessConnections(int pid)
        {
            var connections = new List<string>();
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("TCP") || line.Trim().StartsWith("UDP"))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && int.TryParse(parts[parts.Length - 1], out int connectionPid) && connectionPid == pid)
                        {
                            connections.Add($"{parts[0]} {parts[1]} -> {parts[2]} ({parts[3]})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting connections: {ex.Message}");
            }
            return connections;
        }

        #region Detector de Malware Fantasma

        private List<string> knownSafeLocations = new List<string>
        {
            @"c:\windows\",
        };

        private List<string> trustedPublishers = new List<string>
        {
            "Microsoft Corporation", "Microsoft Windows", "Discord Inc.", "Opera Software", "Google LLC",
            "Valve Corporation", "ASUSTeK COMPUTER INC.", "DarkHub Inc.", "Riot Games, Inc.", "Adobe Systems Incorporated",
            "Apple Inc.", "NVIDIA Corporation", "Intel Corporation", "Oracle Corporation", "Electronic Arts Inc.",
            "Ubisoft Entertainment", "Rockstar Games, Inc.", "Epic Games, Inc.", "GitHub, Inc.", "Mozilla Corporation",
            "Brave Software Inc.", "Sony Interactive Entertainment LLC", "Lenovo (Beijing) Limited",
            "Samsung Electronics Co., Ltd.", "Amazon Services LLC"
        };

        private class ProcessInfo
        {
            public string Name { get; set; }
            public int PID { get; set; }
            public string Details { get; set; }
            public bool IsSelected { get; set; }
        }

        private async void StartMalwareScan(object sender, RoutedEventArgs e)
        {
            if (_isScanning) return;
            _isScanning = true;
            StatusText.Text = ResourceManagerHelper.Instance.Scanning;
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
            MalwareResults.Items.Clear();

            await Task.Run(() =>
            {
                try
                {
                    var processes = Process.GetProcesses();
                    foreach (var process in processes)
                    {
                        try
                        {
                            string fileName = process.MainModule?.FileName;
                            if (string.IsNullOrEmpty(fileName)) continue;

                            if (process.ProcessName.ToLower().Contains("darkhub")) continue;

                            var analysis = AnalyzeProcess(fileName, process);
                            if (analysis.IsSuspicious)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    MalwareResults.Items.Add(new ProcessInfo
                                    {
                                        Name = process.ProcessName,
                                        PID = process.Id,
                                        Details = analysis.Reason,
                                        IsSelected = false
                                    });
                                });
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }

                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE Name = 'powershell.exe' OR Name = 'pwsh.exe'"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            var pid = Convert.ToInt32(obj["ProcessId"]);
                            var commandLine = obj["CommandLine"]?.ToString();
                            if (!string.IsNullOrEmpty(commandLine) && IsSuspiciousPowerShellCommand(commandLine))
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    MalwareResults.Items.Add(new ProcessInfo
                                    {
                                        Name = "PowerShell",
                                        PID = pid,
                                        Details = $"{ResourceManagerHelper.Instance.SuspectCmd} {commandLine}",
                                        IsSelected = false
                                    });
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = $"Error during scanning: {ex.Message}";
                        StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    });
                }
            });

            _isScanning = false;
            UpdateScanStatus();
        }

        private (bool IsSuspicious, string Reason) AnalyzeProcess(string filePath, Process process)
        {
            try
            {
                int suspicionScore = 0;
                string reason = "";

                bool hasValidSig = false;
                string publisher = "";
                try
                {
                    var cert = X509Certificate.CreateFromSignedFile(filePath);
                    var cert2 = new X509Certificate2(cert);
                    hasValidSig = cert2.Verify();
                    publisher = cert2.GetNameInfo(X509NameType.SimpleName, false);
                }
                catch
                {
                    suspicionScore++;
                    reason += ResourceManagerHelper.Instance.NoPfx;
                }

                if (hasValidSig && trustedPublishers.Any(tp => publisher.Contains(tp)))
                    return (false, ResourceManagerHelper.Instance.ValidPfx);

                string dir = Path.GetDirectoryName(filePath).ToLower();
                bool isInSafeLocation = knownSafeLocations.Any(loc => dir.StartsWith(loc));
                if (!isInSafeLocation)
                {
                    suspicionScore++;
                    reason += ResourceManagerHelper.Instance.SuspectLoc;
                }

                long memoryUsage = process.WorkingSet64;
                if (memoryUsage > 4000000000 && !hasValidSig)
                {
                    suspicionScore++;
                    reason += ResourceManagerHelper.Instance.ExcessiveMemNoPfx;
                }

                var connections = GetProcessConnections(process.Id);
                if (connections.Any(conn => IsSuspiciousConnection(conn)))
                {
                    suspicionScore++;
                    reason += $"{ResourceManagerHelper.Instance.SuspectConnec} {string.Join(", ", connections)}; ";
                }

                if (process.ProcessName.ToLower() == "svchost" && !dir.StartsWith(@"c:\windows\"))
                {
                    suspicionScore++;
                    reason += ResourceManagerHelper.Instance.SvchostOutWin;
                }

                if (suspicionScore > 1)
                    return (true, reason.TrimEnd(' ', ';'));
                return (false, ResourceManagerHelper.Instance.Approved);
            }
            catch (Exception ex)
            {
                return (true, $"Erro na análise: {ex.Message}");
            }
        }

        private void RemoveThreats(object sender, RoutedEventArgs e)
        {
            if (_isScanning || MalwareResults.Items.Count == 0)
            {
                StatusText.Text = ResourceManagerHelper.Instance.NoThreatsOrScanning;
                StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
                return;
            }

            var selectedItems = MalwareResults.Items.Cast<ProcessInfo>().Where(p => p.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                StatusText.Text = ResourceManagerHelper.Instance.SelectThreatsToRemove;
                StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
                return;
            }

            var result = MessageBox.Show($"{ResourceManagerHelper.Instance.RemoveThreatsConfirm} {selectedItems.Count} {ResourceManagerHelper.Instance.RemoveThreatsConfirm1}",
                $"{ResourceManagerHelper.Instance.RemoveThreatsConfirm2}", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            StatusText.Text = ResourceManagerHelper.Instance.RemovingSelectedThreats;
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            foreach (var item in selectedItems.ToList())
            {
                try
                {
                    var process = Process.GetProcessById(item.PID);
                    process.Kill();
                    MalwareResults.Items.Remove(item);
                }
                catch (Exception ex)
                {
                    StatusText.Text = $"{ResourceManagerHelper.Instance.ErrorRemoveThreat} (PID: {item.PID}): {ex.Message}";
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    return;
                }
            }

            UpdateScanStatus();
        }

        private bool IsSuspiciousPowerShellCommand(string command)
        {
            string[] suspiciousPatterns =
            {
                "Invoke-Expression", "IEX", "DownloadString", "Net.WebClient",
                "Start-Process", "EncodedCommand", "-NoP", "-NonI", "-W Hidden",
                "bypass", "Invoke-WebRequest", "irm"
            };
            return suspiciousPatterns.Any(pattern => command.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        private void UpdateScanStatus()
        {
            StatusText.Text = MalwareResults.Items.Count > 0
                ? $"{ResourceManagerHelper.Instance.ScanEnded} {MalwareResults.Items.Count} {ResourceManagerHelper.Instance.ScanEnded1}"
                : ResourceManagerHelper.Instance.NoThreats;
            StatusText.Foreground = MalwareResults.Items.Count > 0
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.Green);
        }

        #endregion Detector de Malware Fantasma

        #region Análise Comportamental

        private async void StartBehaviorMonitoring(object sender, RoutedEventArgs e)
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _monitoringCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _monitoringCancellationTokenSource.Token;
            StatusText.Text = ResourceManagerHelper.Instance.ComportamentalBehavioralStarted;
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
            AppBehaviorList.Items.Clear();
            var behaviorData = new List<string>();

            try
            {
                await Task.Run(async () =>
                {
                    var monitoringDuration = TimeSpan.FromMinutes(1);
                    var startTime = DateTime.Now;

                    while (!cancellationToken.IsCancellationRequested && DateTime.Now - startTime < monitoringDuration)
                    {
                        var processes = Process.GetProcesses()
                            .Where(p => IsProcessAccessible(p));

                        foreach (var process in processes)
                        {
                            try
                            {
                                var memUsage = process.WorkingSet64 / 1024 / 1024;
                                float cpuUsage;

                                using (var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true))
                                {
                                    cpuCounter.NextValue();
                                    await Task.Delay(200, cancellationToken);
                                    cpuUsage = cpuCounter.NextValue() / Environment.ProcessorCount;
                                }

                                var connections = GetProcessConnections(process.Id);
                                var connectionInfo = connections.Any() ? string.Join(", ", connections) : ResourceManagerHelper.Instance.NoConnection;

                                var processInfo = $"{process.ProcessName} (PID: {process.Id}) - CPU: {cpuUsage:F2}%, Mem: {memUsage}MB, {ResourceManagerHelper.Instance.Connections} {connectionInfo}";

                                Dispatcher.Invoke(() =>
                                {
                                    AppBehaviorList.Items.Add(processInfo);
                                });

                                behaviorData.Add(processInfo);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erro ao processar {process.ProcessName} (PID: {process.Id}): {ex.Message}");
                            }
                        }

                        await Task.Delay(10000, cancellationToken);
                        Dispatcher.Invoke(() => AppBehaviorList.Items.Clear());
                    }

                    Dispatcher.Invoke(() => GenerateBehaviorReport(behaviorData));
                }, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                Dispatcher.Invoke(() => GenerateBehaviorReport(behaviorData));
            }
            finally
            {
                _isMonitoring = false;
            }
        }

        private void StopBehaviorMonitoring(object sender, RoutedEventArgs e)
        {
            if (!_isMonitoring) return;

            _monitoringCancellationTokenSource?.Cancel();
            StatusText.Text = ResourceManagerHelper.Instance.StoppingBehavioral;
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
        }

        private void GenerateBehaviorReport(List<string> behaviorData)
        {
            try
            {
                string reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"BehaviorReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                using (var writer = new StreamWriter(reportPath))
                {
                    writer.WriteLine($"{ResourceManagerHelper.Instance.BehaviorReport} {DateTime.Now}");
                    writer.WriteLine("--------------------------------------------------");
                    foreach (string item in behaviorData)
                    {
                        writer.WriteLine(item);
                    }
                    writer.WriteLine("--------------------------------------------------");
                    writer.WriteLine($"{ResourceManagerHelper.Instance.ProcessesMonitored} {behaviorData.Count}");
                }
                StatusText.Text = $"{ResourceManagerHelper.Instance.ReportGenerated} {reportPath}";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"{ResourceManagerHelper.Instance.ReportNotGenerated} {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        #endregion Análise Comportamental

        #region Otimizador de Latência

        private void LoadPeripherals()
        {
            PeripheralsList.Items.Clear();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE PNPDeviceID LIKE 'USB%'"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString() ?? "Desconhecido";
                        string manufacturer = obj["Manufacturer"]?.ToString() ?? "N/A";
                        string status = obj["Status"]?.ToString() ?? "N/A";
                        PeripheralsList.Items.Add($"{name} (Fabricante: {manufacturer}, Status: {status})");
                    }
                }
                StatusText.Text = $"Periféricos encontrados: {PeripheralsList.Items.Count}";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erro ao carregar periféricos: {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void OptimizeLatency(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Otimizando latência...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            try
            {
                if (!IsRunningAsAdmin())
                {
                    throw new UnauthorizedAccessException("Este recurso requer privilégios administrativos.");
                }

                var audioProcesses = Process.GetProcessesByName("audiodg");
                foreach (var process in audioProcesses)
                {
                    process.PriorityClass = ProcessPriorityClass.RealTime;
                    process.ProcessorAffinity = (IntPtr)(Environment.ProcessorCount > 1 ? 2 : 1);
                }

                var peripheralProcesses = Process.GetProcesses()
                    .Where(p => IsProcessAccessible(p) &&
                               (p.ProcessName.ToLower().Contains("usb") || p.ProcessName.ToLower().Contains("hid")));
                foreach (var process in peripheralProcesses)
                {
                    process.PriorityClass = ProcessPriorityClass.AboveNormal;
                }

                var gameProcesses = Process.GetProcesses()
                    .Where(p => IsProcessAccessible(p) &&
                               (p.ProcessName.ToLower().Contains("game") || p.ProcessName.ToLower().Contains("steam") ||
                                p.ProcessName.ToLower().Contains("riot")));
                foreach (var process in gameProcesses)
                {
                    process.PriorityClass = ProcessPriorityClass.High;
                    process.ProcessorAffinity = (IntPtr)(Environment.ProcessorCount - 1);
                }

                StatusText.Text = "Latência otimizada com sucesso!";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Erro ao otimizar latência: {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void ShowAdvancedSettings(object sender, RoutedEventArgs e)
        {
            var advancedSettingsWindow = new AdvancedLatencySettings();
            advancedSettingsWindow.ShowDialog();
        }

        public class AdvancedLatencySettings : Window
        {
            private ComboBox processPriorityCombo;
            private ListBox processList;
            private TextBox filterTextBox;
            private CheckBox autoRefreshCheckBox;
            private Label processDetailsLabel;
            private DispatcherTimer refreshTimer;

            public AdvancedLatencySettings()
            {
                Title = "Configurações Avançadas de Latência";
                Width = 600;
                Height = 450;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());

                filterTextBox = new TextBox { Margin = new Thickness(10), Width = 200 };
                filterTextBox.TextChanged += FilterProcesses;
                Grid.SetRow(filterTextBox, 0);
                Grid.SetColumn(filterTextBox, 0);
                grid.Children.Add(new Label { Content = "Filtrar processos:", Margin = new Thickness(10, 10, 0, 0) });
                grid.Children.Add(filterTextBox);

                autoRefreshCheckBox = new CheckBox { Content = "Atualizar automaticamente", Margin = new Thickness(10), IsChecked = true };
                autoRefreshCheckBox.Checked += StartAutoRefresh;
                autoRefreshCheckBox.Unchecked += StopAutoRefresh;
                Grid.SetRow(autoRefreshCheckBox, 0);
                Grid.SetColumn(autoRefreshCheckBox, 1);
                grid.Children.Add(autoRefreshCheckBox);

                processList = new ListBox { Margin = new Thickness(10), Height = 200 };
                processList.SelectionChanged += UpdateProcessDetails;
                Grid.SetRow(processList, 1);
                Grid.SetColumnSpan(processList, 2);
                grid.Children.Add(processList);

                processDetailsLabel = new Label { Margin = new Thickness(10), Content = "Selecione um processo para ver detalhes." };
                Grid.SetRow(processDetailsLabel, 2);
                Grid.SetColumnSpan(processDetailsLabel, 2);
                grid.Children.Add(processDetailsLabel);

                processPriorityCombo = new ComboBox
                {
                    ItemsSource = Enum.GetValues(typeof(ProcessPriorityClass)).Cast<ProcessPriorityClass>(),
                    Margin = new Thickness(10),
                    SelectedIndex = 0
                };
                Grid.SetRow(processPriorityCombo, 3);
                Grid.SetColumn(processPriorityCombo, 0);
                grid.Children.Add(new Label { Content = "Prioridade:", Margin = new Thickness(10, 0, 0, 0) });
                grid.Children.Add(processPriorityCombo);

                var applyButton = new Button { Content = "Aplicar Prioridade", Margin = new Thickness(10), Width = 150 };
                applyButton.Click += ApplyPriority;
                Grid.SetRow(applyButton, 3);
                Grid.SetColumn(applyButton, 1);
                grid.Children.Add(applyButton);

                Content = grid;

                refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                refreshTimer.Tick += (s, args) => RefreshProcessList();
                RefreshProcessList();
                if (autoRefreshCheckBox.IsChecked == true) refreshTimer.Start();
            }

            private void RefreshProcessList()
            {
                var filter = filterTextBox.Text.ToLower();
                processList.Items.Clear();
                foreach (var process in Process.GetProcesses().Where(p => IsProcessAccessible(p)))
                {
                    var processInfo = $"{process.ProcessName} (PID: {process.Id})";
                    if (string.IsNullOrEmpty(filter) || processInfo.ToLower().Contains(filter))
                    {
                        processList.Items.Add(processInfo);
                    }
                }
            }

            private void FilterProcesses(object sender, TextChangedEventArgs e)
            {
                RefreshProcessList();
            }

            private void StartAutoRefresh(object sender, RoutedEventArgs e)
            {
                refreshTimer.Start();
            }

            private void StopAutoRefresh(object sender, RoutedEventArgs e)
            {
                refreshTimer.Stop();
            }

            private void UpdateProcessDetails(object sender, SelectionChangedEventArgs e)
            {
                if (processList.SelectedItem == null)
                {
                    processDetailsLabel.Content = "Selecione um processo para ver detalhes.";
                    return;
                }

                var selectedItem = processList.SelectedItem.ToString();
                var pidStr = selectedItem.Split(new[] { "PID: " }, StringSplitOptions.None)[1].Trim(')');
                if (int.TryParse(pidStr, out int pid))
                {
                    try
                    {
                        var process = Process.GetProcessById(pid);
                        var memUsage = process.WorkingSet64 / 1024 / 1024;
                        var cpuCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
                        cpuCounter.NextValue();
                        Thread.Sleep(100);
                        var cpuUsage = cpuCounter.NextValue() / Environment.ProcessorCount;

                        processDetailsLabel.Content = $"Nome: {process.ProcessName}\n" +
                                                      $"PID: {process.Id}\n" +
                                                      $"Uso de CPU: {cpuUsage:F2}%\n" +
                                                      $"Uso de Memória: {memUsage} MB\n" +
                                                      $"Prioridade Atual: {process.PriorityClass}\n" +
                                                      $"Caminho: {process.MainModule?.FileName ?? "N/A"}";
                    }
                    catch (Exception ex)
                    {
                        processDetailsLabel.Content = $"Erro ao obter detalhes: {ex.Message}";
                    }
                }
            }

            private void ApplyPriority(object sender, RoutedEventArgs e)
            {
                if (processList.SelectedItem == null)
                {
                    MessageBox.Show("Selecione um processo primeiro!");
                    return;
                }

                var selectedItem = processList.SelectedItem.ToString();
                var pidStr = selectedItem.Split(new[] { "PID: " }, StringSplitOptions.None)[1].Trim(')');
                if (int.TryParse(pidStr, out int pid))
                {
                    try
                    {
                        var process = Process.GetProcessById(pid);
                        var newPriority = (ProcessPriorityClass)processPriorityCombo.SelectedItem;
                        process.PriorityClass = newPriority;
                        MessageBox.Show($"Prioridade de {process.ProcessName} ajustada para {newPriority} com sucesso!");
                        UpdateProcessDetails(null, null);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao ajustar prioridade: {ex.Message}");
                    }
                }
            }
        }

        #endregion Otimizador de Latência

        #region Bloqueador de Rastreamento

        private async void StartTrackingAudit(object sender, RoutedEventArgs e)
        {
            StatusText.Text = ResourceManagerHelper.Instance.AuditTracking;
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
            TrackingResults.Items.Clear();

            await Task.Run(async () =>
            {
                try
                {
                    var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                    var hostsContent = File.ReadAllLines(hostsPath);
                    var trackingInHosts = hostsContent
                        .Where(line => !line.StartsWith("#") && (line.Contains("tracking") || line.Contains("analytics") || line.Contains("ads")))
                        .Select(line => ("Hosts File", line.Trim()));

                    var networkTrackers = await AuditNetworkConnections();
                    var cookieTrackers = AuditBrowserCookies();
                    var allTrackers = trackingInHosts.Concat(networkTrackers).Concat(cookieTrackers).ToList();

                    Dispatcher.Invoke(() =>
                    {
                        foreach (var (source, tracker) in allTrackers)
                        {
                            TrackingResults.Items.Add(new TrackingItem
                            {
                                Source = source,
                                Details = tracker,
                                Category = CategorizeTracker(tracker),
                                IsSelected = false
                            });
                        }
                    });

                    GenerateTrackingReport(allTrackers);
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = $"Audit error: {ex.Message}";
                        StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    });
                }
            });

            StatusText.Text = $"{ResourceManagerHelper.Instance.AuditCompleted} {TrackingResults.Items.Count} {ResourceManagerHelper.Instance.AuditCompleted1}";
            StatusText.Foreground = TrackingResults.Items.Count > 0
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.Green);
        }

        private async Task<List<(string Source, string Tracker)>> AuditNetworkConnections()
        {
            var trackers = new List<(string, string)>();
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var suspiciousDomains = new[] { "tracking", "analytics", "ads", "telemetry", "doubleclick", "googleadservices" };

                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("TCP") && suspiciousDomains.Any(d => line.Contains(d, StringComparison.OrdinalIgnoreCase)))
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5)
                        {
                            trackers.Add(("Network", $"{parts[1]} -> {parts[2]} (PID: {parts[4]})"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when auditing network connections: {ex.Message}");
            }
            return trackers;
        }

        private List<(string Source, string Tracker)> AuditBrowserCookies()
        {
            var trackers = new List<(string, string)>();
            try
            {
                string chromeCookiesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\User Data\Default\Cookies");
                if (File.Exists(chromeCookiesPath))
                {
                    var suspiciousCookies = new[] { "_ga", "_gid", "__utma", "__utmz", "NID" };
                    trackers.Add(("Chrome Cookies", $"{ResourceManagerHelper.Instance.CookiesAudit} {string.Join(", ", suspiciousCookies)}"));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when auditing cookies: {ex.Message}");
            }
            return trackers;
        }

        private string CategorizeTracker(string tracker)
        {
            if (tracker.Contains("ads") || tracker.Contains("doubleclick") || tracker.Contains("adform"))
                return "Advertisements";
            if (tracker.Contains("analytics") || tracker.Contains("google-analytics") || tracker.Contains("_ga"))
                return "Analytics";
            if (tracker.Contains("facebook") || tracker.Contains("twitter"))
                return "Social media";
            return "Other";
        }

        private void GenerateTrackingReport(List<(string Source, string Tracker)> trackers)
        {
            try
            {
                string reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"TrackingAuditReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                using (var writer = new PdfWriter(reportPath))
                {
                    using (var pdf = new PdfDocument(writer))
                    {
                        var document = new Document(pdf);
                        var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                        document.Add(new Paragraph(ResourceManagerHelper.Instance.AuditReports)
                            .SetFont(boldFont)
                            .SetFontSize(16));
                        document.Add(new Paragraph($"{ResourceManagerHelper.Instance.AuditGenIn} {DateTime.Now}"));
                        document.Add(new Paragraph($"{ResourceManagerHelper.Instance.TrackersDetected} {trackers.Count}"));

                        foreach (var (source, tracker) in trackers)
                        {
                            document.Add(new Paragraph($"- {source}: {tracker} ({ResourceManagerHelper.Instance.Category} {CategorizeTracker(tracker)})"));
                        }

                        document.Close();
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    StatusText.Text += $" {ResourceManagerHelper.Instance.ReportSaved} {reportPath}";
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = $"{ResourceManagerHelper.Instance.ReportNotGenerated} {ex.Message}";
                    StatusText.Foreground = new SolidColorBrush(Colors.Red);
                });
            }
        }

        private void BlockAllTracking(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdmin())
            {
                StatusText.Text = "This feature requires administrative privileges.";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            StatusText.Text = ResourceManagerHelper.Instance.BlockingTracking;
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            try
            {
                var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                BackupHostsFile(hostsPath);

                var hostsContent = File.ReadAllLines(hostsPath).ToList();
                var trackingDomains = GetComprehensiveTrackingDomains();

                var selectedItems = TrackingResults.Items.Cast<TrackingItem>()
                    .Where(item => item.IsSelected).Select(item => item.Details).ToList();

                var domainsToBlock = selectedItems.Any() ? selectedItems : trackingDomains;

                foreach (var domain in domainsToBlock)
                {
                    var cleanDomain = domain.Split(' ').Last().Trim();
                    if (!hostsContent.Any(line => line.Contains(cleanDomain)))
                    {
                        hostsContent.Add($"127.0.0.1 {cleanDomain}");
                        _blockedDomainsHistory.Add(cleanDomain);
                    }
                }

                File.WriteAllLines(hostsPath, hostsContent);
                BlockTrackingPorts();

                StatusText.Text = $"Blocked trackers: {domainsToBlock.Count}";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
                TrackingResults.Items.Clear();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error when blocking: {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private void BackupHostsFile(string hostsPath)
        {
            if (string.IsNullOrEmpty(_originalHostsBackupPath))
            {
                _originalHostsBackupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"hosts_backup_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.Copy(hostsPath, _originalHostsBackupPath, true);
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text += $" Backup hosts saved in: {_originalHostsBackupPath}";
                });
            }
        }

        private List<string> GetComprehensiveTrackingDomains()
        {
            return new List<string>
            {
                "doubleclick.net", "google-analytics.com", "facebook.com", "ads.twitter.com",
                "track.adform.net", "analytics.google.com", "googletagmanager.com", "scorecardresearch.com",
                "quantserve.com", "adnxs.com", "mathtag.com", "bluekai.com", "krxd.net",
                "demdex.net", "omtrdc.net", "everesttech.net", "pixel.rubiconproject.com",
                "pubmatic.com", "openx.net", "adroll.com", "taboola.com", "outbrain.com"
            };
        }

        private void BlockTrackingPorts()
        {
            string[] trackingPorts = { "80", "443", "9000" };
            try
            {
                foreach (var port in trackingPorts)
                {
                    var ruleName = $"BlockTrackingPort_{port}";
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=out action=block protocol=TCP localport={port}",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error blocking ports: {ex.Message}");
            }
        }

        private class TrackingItem
        {
            public string Source { get; set; }
            public string Details { get; set; }
            public string Category { get; set; }
            public bool IsSelected { get; set; }
        }

        #endregion Bloqueador de Rastreamento

        #region Proteção Contra Phishing

        private async void VerifySingleUrl(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdmin())
            {
                StatusText.Text = "This feature requires administrative privileges.";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            string url = PhishingUrlInput.Text;
            if (string.IsNullOrWhiteSpace(url) || url == ResourceManagerHelper.Instance.EnterURL)
            {
                StatusText.Text = "Enter a valid URL to check.";
                StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
                return;
            }

            StatusText.Text = $"Checking {url}...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
            PhishingResults.Items.Clear();

            await Task.Run(async () =>
            {
                try
                {
                    var analysis = await AnalyzeURL(url);
                    Dispatcher.Invoke(() =>
                    {
                        PhishingResults.Items.Add(new PhishingItem
                        {
                            URL = url,
                            Reason = analysis.Reason,
                            IsSelected = false
                        });
                        StatusText.Text = analysis.IsSuspicious
                            ? $"Suspicious URL detected: {url}"
                            : $"Secure URL: {url}";
                        StatusText.Foreground = analysis.IsSuspicious
                            ? new SolidColorBrush(Colors.Red)
                            : new SolidColorBrush(Colors.Green);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = $"Error when checking: {ex.Message}";
                        StatusText.Foreground = new SolidColorBrush(Colors.Red);
                    });
                }
            });
        }

        private void PhishingUrlInput_GotFocus(object sender, RoutedEventArgs e)
        {
            if (PhishingUrlInput.Text == ResourceManagerHelper.Instance.EnterURL)
            {
                PhishingUrlInput.Text = "";
                PhishingUrlInput.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void PhishingUrlInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PhishingUrlInput.Text))
            {
                PhishingUrlInput.Text = ResourceManagerHelper.Instance.EnterURL;
                PhishingUrlInput.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private async Task<(bool IsSuspicious, string Reason)> AnalyzeURL(string url)
        {
            try
            {
                string reason = "";
                var uri = new Uri(url);
                var domain = uri.Host.ToLower();

                bool isPhishing = await CheckGoogleSafeBrowsing(url);
                if (isPhishing)
                {
                    reason += "Detected as phishing by Google Safe Browsing; ";
                }

                bool hasValidSsl = await VerifySSL(url);
                if (!hasValidSsl)
                {
                    reason += "Invalid or missing SSL certificate; ";
                }

                var suspiciousPatterns = new[]
                {
            @"\b(update|secure-?login|signin-?verify)\b",
            @"\b[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\b"
        };
                if (suspiciousPatterns.Any(p => Regex.IsMatch(url, p, RegexOptions.IgnoreCase)))
                {
                    reason += "Contains advanced phishing patterns; ";
                }

                var legitDomains = new[] { "google.com", "microsoft.com", "paypal.com", "amazon.com", "facebook.com", "deepseek.com" };
                bool isLegitDomain = legitDomains.Any(d => domain == d || domain.EndsWith($".{d}"));
                if (isLegitDomain)
                {
                    return (isPhishing, isPhishing ? reason.TrimEnd(' ', ';') : "Secure (trusted domain)");
                }

                if (legitDomains.Any(d => domain.Contains(d) && domain != d && !domain.EndsWith($".{d}")))
                {
                    reason += "Possible imitation of legitimate domain; ";
                }

                bool isSuspicious = isPhishing || (reason.Length > 0 && reason != "Invalid or missing SSL certificate; ");
                return (isSuspicious, isSuspicious ? reason.TrimEnd(' ', ';') : "Secure");
            }
            catch (Exception ex)
            {
                return (true, $"Error when analyzing: {ex.Message}");
            }
        }

        private async Task<bool> VerifySSL(string url)
        {
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => cert.Verify()
                };
                using (var client = new HttpClient(handler))
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> CheckGoogleSafeBrowsing(string url)
        {
            const string apiKey = "No ;D";
            const string safeBrowsingUrl = "https://safebrowsing.googleapis.com/v4/threatMatches:find?key=";

            var requestBody = new
            {
                client = new { clientId = "DarkHub", clientVersion = "1.0" },
                threatInfo = new
                {
                    threatTypes = new[] { "MALWARE", "SOCIAL_ENGINEERING", "UNWANTED_SOFTWARE" },
                    platformTypes = new[] { "WINDOWS" },
                    threatEntryTypes = new[] { "URL" },
                    threatEntries = new[] { new { url } }
                }
            };

            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{safeBrowsingUrl}{apiKey}", content);
                response.EnsureSuccessStatusCode();
                var responseContent = await response.Content.ReadAsStringAsync();
                return responseContent.Contains("matches");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Google Safe Browsing error: {ex.Message}");
                return false;
            }
        }

        private void BlockPhishingSites(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdmin())
            {
                StatusText.Text = "This feature requires administrative privileges.";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            StatusText.Text = "Blocking phishing sites...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            try
            {
                var hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                BackupHostsFile(hostsPath);
                var hostsContent = File.ReadAllLines(hostsPath).ToList();

                var selectedItems = PhishingResults.Items.Cast<PhishingItem>()
                    .Where(item => item.IsSelected).Select(item => item.URL).ToList();

                if (selectedItems.Count == 0)
                {
                    StatusText.Text = "Select URLs to block.";
                    StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
                    return;
                }

                foreach (var url in selectedItems)
                {
                    var domain = new Uri(url).Host;
                    if (!hostsContent.Any(line => line.Contains(domain)))
                    {
                        hostsContent.Add($"127.0.0.1 {domain}");
                        _blockedDomainsHistory.Add(domain);
                    }
                }

                File.WriteAllLines(hostsPath, hostsContent);
                StatusText.Text = $"Blocked {selectedItems.Count} phishing sites.";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
                PhishingResults.Items.Clear();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error when blocking: {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private List<string> ExtractURLs(string text)
        {
            var urlPattern = @"https?://[^\s]+";
            return Regex.Matches(text, urlPattern).Cast<Match>().Select(m => m.Value).ToList();
        }

        private string ExtractURLFromConnection(string connection)
        {
            var parts = connection.Split(new[] { "->" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                var destination = parts[1].Trim().Split(' ')[0];
                if (Uri.TryCreate($"http://{destination}", UriKind.Absolute, out _))
                {
                    return $"http://{destination}";
                }
            }
            return null;
        }

        private class PhishingItem
        {
            public string URL { get; set; }
            public string Reason { get; set; }
            public bool IsSelected { get; set; }
        }

        #endregion Proteção Contra Phishing

        #region Proteção Contra Exploits

        private async void StartExploitMonitoring(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdmin())
            {
                StatusText.Text = "Este recurso requer privilégios administrativos.";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            if (_isExploitMonitoring) return;

            _isExploitMonitoring = true;
            _exploitCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _exploitCancellationTokenSource.Token;
            StatusText.Text = ResourceManagerHelper.Instance.MonitoringExploits;
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
            ExploitResults.Items.Clear();

            try
            {
                await Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var processes = Process.GetProcesses().Where(p => IsProcessAccessible(p));
                        foreach (var process in processes)
                        {
                            try
                            {
                                var exploitDetected = await DetectExploit(process);
                                if (exploitDetected.IsSuspicious)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        ExploitResults.Items.Add(new ExploitItem
                                        {
                                            ProcessName = process.ProcessName,
                                            PID = process.Id,
                                            Reason = exploitDetected.Reason,
                                            IsSelected = false
                                        });
                                    });
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Monitoring error {process.ProcessName}: {ex.Message}");
                            }
                            finally
                            {
                                process.Dispose();
                            }
                        }
                        await Task.Delay(3000, cancellationToken);
                    }
                }, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "Exploit monitoring paused.";
                    StatusText.Foreground = new SolidColorBrush(Colors.Green);
                });
            }
            finally
            {
                _isExploitMonitoring = false;
            }
        }

        private void StopExploitMonitoring(object sender, RoutedEventArgs e)
        {
            if (!_isExploitMonitoring) return;

            _exploitCancellationTokenSource?.Cancel();
            StatusText.Text = "Stopping exploit monitoring...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
        }

        private async Task<(bool IsSuspicious, string Reason)> DetectExploit(Process process)
        {
            try
            {
                string reason = "";
                var filePath = process.MainModule.FileName;

                bool hasValidSig = VerifyBinarySignature(filePath);
                if (!hasValidSig)
                {
                    reason += "Invalid or missing digital signature; ";
                }

                var memoryUsage = process.WorkingSet64;
                var peakMemory = process.PeakWorkingSet64;
                if (memoryUsage > 2_000_000_000 || (peakMemory > memoryUsage * 1.5))
                {
                    reason += "Suspicious memory usage (potential buffer overflow); ";
                }

                var connections = GetProcessConnections(process.Id);
                if (connections.Any(conn => IsExploitConnection(conn)))
                {
                    reason += "Suspected OER connection detected; ";
                }

                var modules = process.Modules.Cast<ProcessModule>();
                if (modules.Any(m => m.ModuleName.ToLower().Contains("kernel32") && m.BaseAddress.ToInt64() > 0x7FFF0000))
                {
                    reason += "Possible code injection detected; ";
                }

                return (reason.Length > 0, reason.Length > 0 ? reason.TrimEnd(' ', ';') : "Seguro");
            }
            catch (Exception ex)
            {
                return (true, $"Error when analyzing: {ex.Message}");
            }
        }

        private bool VerifyBinarySignature(string filePath)
        {
            try
            {
                var cert = X509Certificate.CreateFromSignedFile(filePath);
                var cert2 = new X509Certificate2(cert);
                return cert2.Verify();
            }
            catch
            {
                return false;
            }
        }

        private void BlockExploitThreats(object sender, RoutedEventArgs e)
        {
            if (!IsRunningAsAdmin())
            {
                StatusText.Text = "This feature requires administrative privileges.";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            StatusText.Text = "Blocking exploit threats...";
            StatusText.Foreground = new SolidColorBrush(Colors.Yellow);

            try
            {
                var selectedItems = ExploitResults.Items.Cast<ExploitItem>()
                    .Where(item => item.IsSelected).ToList();

                if (selectedItems.Count == 0)
                {
                    StatusText.Text = "Select threats to block.";
                    StatusText.Foreground = new SolidColorBrush(Colors.Yellow);
                    return;
                }

                foreach (var item in selectedItems)
                {
                    try
                    {
                        var process = Process.GetProcessById(item.PID);
                        process.Kill();
                        process.WaitForExit(1000);
                        ExploitResults.Items.Remove(item);
                    }
                    catch (Exception ex)
                    {
                        StatusText.Text = $"Error when blocking PID {item.PID}: {ex.Message}";
                        StatusText.Foreground = new SolidColorBrush(Colors.Red);
                        return;
                    }
                }

                StatusText.Text = $"Blocked {selectedItems.Count} exploit threats.";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error when blocking: {ex.Message}";
                StatusText.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        private bool IsExploitConnection(string connection)
        {
            var suspiciousPorts = new[] { "4444", "1337", "5555", "3389" };
            var suspiciousDomains = new[] { ".ru", ".cn", "exploit", "hack" };
            return suspiciousPorts.Any(p => connection.Contains(p)) ||
                   suspiciousDomains.Any(d => connection.Contains(d, StringComparison.OrdinalIgnoreCase));
        }

        private class ExploitItem
        {
            public string ProcessName { get; set; }
            public int PID { get; set; }
            public string Reason { get; set; }
            public bool IsSelected { get; set; }
        }

        #endregion Proteção Contra Exploits

        private bool IsSuspiciousConnection(string connection)
        {
            string[] suspiciousDomains = { ".ru", ".cn", ".xyz", ".top" };
            string[] suspiciousPorts = { "4444", "6667", "1337" };
            return suspiciousDomains.Any(domain => connection.Contains(domain)) &&
                   suspiciousPorts.Any(port => connection.Contains(port));
        }
    }
}