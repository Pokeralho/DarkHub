using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DarkHub
{
    public partial class Optimizer : Page
    {
        public Optimizer()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorInitializingOptimizer, ex.Message + "\nStackTrace: " + ex.StackTrace),
                ResourceManagerHelper.Instance.CriticalErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableUnnecessaryServices(object sender, RoutedEventArgs e)
        {
            var servicesWindow = new Window
            {
                Title = ResourceManagerHelper.Instance.DisableServicesTitle,
                Width = 500,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Owner = Window.GetWindow(this),
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            stackPanel.Children.Add(new TextBlock
            {
                Text = ResourceManagerHelper.Instance.DisableServices,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = Brushes.White
            });

            var listBox = new ListBox
            {
                Height = 150,
                Background = new SolidColorBrush(Color.FromRgb(42, 42, 46)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
            };
            var unnecessaryServices = GetUnnecessaryServices();

            foreach (var service in unnecessaryServices)
            {
                string statusText = service.Status == ServiceControllerStatus.Running ? ResourceManagerHelper.Instance.Enabled : ResourceManagerHelper.Instance.Disabled;
                var checkBox = new CheckBox
                {
                    Content = $"{service.DisplayName} ({service.ServiceName}) {statusText}",
                    Tag = service.ServiceName,
                    IsChecked = false,
                    Foreground = Brushes.White
                };
                listBox.Items.Add(checkBox);
            }

            stackPanel.Children.Add(listBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            var disableButton = new Button
            {
                Content = ResourceManagerHelper.Instance.DisableSelected,
                Width = 170,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
            };
            var enableButton = new Button
            {
                Content = ResourceManagerHelper.Instance.EnableSelected,
                Width = 170,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
            };
            var cancelButton = new Button
            {
                Content = ResourceManagerHelper.Instance.Cancel,
                Width = 100,
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
            };

            buttonPanel.Children.Add(disableButton);
            buttonPanel.Children.Add(enableButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);
            servicesWindow.Content = stackPanel;

            disableButton.Click += (s, ev) =>
            {
                var selectedServices = listBox.Items.Cast<CheckBox>()
                    .Where(cb => cb.IsChecked == true)
                    .Select(cb => cb.Tag.ToString())
                    .ToList();
                DisableSelectedServices(selectedServices);
                servicesWindow.Close();
            };

            enableButton.Click += (s, ev) =>
            {
                var selectedServices = listBox.Items.Cast<CheckBox>()
                    .Where(cb => cb.IsChecked == true)
                    .Select(cb => cb.Tag.ToString())
                    .ToList();
                EnableSelectedServices(selectedServices);
                servicesWindow.Close();
            };

            cancelButton.Click += (s, ev) => servicesWindow.Close();

            servicesWindow.ShowDialog();
        }

        private List<ServiceController> GetUnnecessaryServices()
        {
            var allServices = ServiceController.GetServices();
            var unnecessaryServiceNames = new List<string>
            {
        "WSearch",
        "SysMain",
        "WbioSrvc",
        "MapsBroker",
        "dmwappushservice",
        "DiagTrack",
        "GeolocationSvc",
        "RetailDemo",
        "XblAuthManager",
        "XblGameSave",
        "XboxGipSvc",
        "XboxNetApiSvc",
        "GamingServices",
        "GameBarPresenceWriter",
        "PcaSvc",
        "diagnosticshub.standardcollector.service",
        "WerSvc",
        "Fax",
        "Spooler",
        "TabletInputService",
        "SensrSvc",
        "HomeGroupListener",
        "HomeGroupProvider",
        "RemoteRegistry",
        "SSDPSRV",
        "upnphost",
        "wuauserv",
        "OneSyncSvc",
        "PushToInstall"
    };
            return allServices
                .Where(s => unnecessaryServiceNames.Contains(s.ServiceName))
                .ToList();
        }

        private void DisableSelectedServices(List<string> serviceNames)
        {
            try
            {
                foreach (var serviceName in serviceNames)
                {
                    using var service = new ServiceController(serviceName);
                    if (service.Status != ServiceControllerStatus.Stopped)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                    using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", writable: true);
                    if (key != null)
                    {
                        key.SetValue("Start", 4, RegistryValueKind.DWord);
                    }
                }

                MessageBox.Show(ResourceManagerHelper.Instance.SelectedServicesDisabled,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.NoAccessToDisableService + " " + ex.Message,
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.NoAccessToDisableService + " " + ex.Message,
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableSelectedServices(List<string> serviceNames)
        {
            try
            {
                foreach (var serviceName in serviceNames)
                {
                    using var service = new ServiceController(serviceName);
                    if (service.Status == ServiceControllerStatus.Stopped)
                    {
                        using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", writable: true);
                        if (key != null)
                        {
                            key.SetValue("Start", 2, RegistryValueKind.DWord);
                        }

                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }

                MessageBox.Show(ResourceManagerHelper.Instance.SelectedServicesEnabled,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.NoAccessToEnableService + " " + ex.Message,
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.NoAccessToEnableService + " " + ex.Message,
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SystemInfo(object sender, RoutedEventArgs e)
        {
            try
            {
                var systemInfo = new StringBuilder();

                var os = Environment.OSVersion;
                systemInfo.AppendLine($"SO: {os.VersionString}");

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
                systemInfo.AppendLine($"Frequência da RAM: {memorySpeedDisplay}");

                var gpuQuery = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController");
                var gpu = gpuQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                if (gpu != null)
                {
                    var gpuName = gpu["Name"]?.ToString() ?? "Não encontrado";
                    var gpuVram = gpu["AdapterRAM"];
                    string vramDisplay = gpuVram != null ? $"{Convert.ToInt64(gpuVram) / (1024 * 1024):F2} MB" : "Não encontrado";
                    systemInfo.AppendLine($"GPU: {gpuName}");
                    systemInfo.AppendLine($"VRAM: {vramDisplay}");
                }
                else
                {
                    systemInfo.AppendLine("GPU: Não encontrada");
                }

                var motherboardQuery = new ManagementObjectSearcher("SELECT Product FROM Win32_BaseBoard");
                var motherboard = motherboardQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                systemInfo.AppendLine($"Placa Mãe: {motherboard?["Product"]?.ToString() ?? "Não encontrado"}");

                var biosQuery = new ManagementObjectSearcher("SELECT Version FROM Win32_BIOS");
                var bios = biosQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                systemInfo.AppendLine($"BIOS: {bios?["Version"]?.ToString() ?? "Não encontrado"}");

                await ShowSystemInfoWindowAsync(systemInfo.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorGettingSystemInfo, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ShowSystemInfoWindowAsync(string systemInfo)
        {
            try
            {
                var infoWindow = new Window
                {
                    Title = ResourceManagerHelper.Instance.SystemInfoTitle,
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    WindowStyle = WindowStyle.ToolWindow,
                    FontFamily = new FontFamily("JetBrains Mono"),
                    FontSize = 14
                };

                infoWindow.BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142));
                infoWindow.BorderThickness = new Thickness(1);

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var titleBlock = new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.SystemInfoTitle,
                    FontFamily = new FontFamily("JetBrains Mono"),
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                Grid.SetRow(titleBlock, 0);
                grid.Children.Add(titleBlock);

                var textBox = new TextBox
                {
                    Text = systemInfo ?? ResourceManagerHelper.Instance.NoDataAvailable,
                    IsReadOnly = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = new SolidColorBrush(Color.FromRgb(42, 42, 46)),
                    Foreground = Brushes.White,
                    Margin = new Thickness(10),
                    Padding = new Thickness(10),
                    FontSize = 14,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                    BorderThickness = new Thickness(1)
                };
                Grid.SetRow(textBox, 1);
                grid.Children.Add(textBox);

                var border = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = infoWindow.Background,
                    Child = grid,
                    Margin = new Thickness(2)
                };
                infoWindow.Content = border;

                await Task.Run(() => infoWindow.Dispatcher.Invoke(() => infoWindow.ShowDialog()));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorShowingInfoWindow, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ClearTempFilesAndLogs(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.CleaningProgressTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingCleanup);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    string windows = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
                    string systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
                    string userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? @"C:\Users\Default";
                    string temp = Environment.GetEnvironmentVariable("TEMP") ?? Path.Combine(userProfile, "AppData", "Local", "Temp");

                    var tasks = new List<(string Description, Action Action)>
                    {
                        (ResourceManagerHelper.Instance.CleaningWindowsTemp, () => ClearDirectory($"{windows}\\temp", progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningPrefetchExe, () => ClearFilesByExtension($"{windows}\\Prefetch", "*.exe", progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningPrefetchDll, () => ClearFilesByExtension($"{windows}\\Prefetch", "*.dll", progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningPrefetchPf, () => ClearFilesByExtension($"{windows}\\Prefetch", "*.pf", progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningDllCache, () => ClearDirectory($"{windows}\\system32\\dllcache", progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningSystemDriveTemp, () => ClearDirectory($"{systemDrive}\\Temp", progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningUserTemp, () => ClearDirectory(temp, progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningHistory, () => ClearDirectory(Path.Combine(userProfile, "Local Settings", "History"), progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningTempInternetFiles, () => ClearDirectory(Path.Combine(userProfile, "Local Settings", "Temporary Internet Files"), progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningLocalTemp, () => ClearDirectory(Path.Combine(userProfile, "Local Settings", "Temp"), progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningRecent, () => ClearDirectory(Path.Combine(userProfile, "Recent"), progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningCookies, () => ClearDirectory(Path.Combine(userProfile, "Cookies"), progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningEventLogs, () => ClearEventLogsWithWevtutil(progressTextBox))
                    };

                    foreach (var task in tasks)
                    {
                        AppendProgress(progressTextBox, task.Description);
                        await Task.Run(task.Action);
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                        await Task.Delay(100);
                    }
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.CleanupCompletedSuccess);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.CleanupCompleted,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.GeneralCleanupError, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.GeneralCleanupError, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private (Window Window, TextBox ProgressTextBox) CreateProgressWindow(string title)
        {
            try
            {
                var window = new Window
                {
                    Title = title,
                    Width = 500,
                    Height = 400,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                    BorderThickness = new Thickness(1)
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var titleText = new TextBlock
                {
                    Text = title,
                    FontFamily = new FontFamily("JetBrains Mono"),
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(titleText, 0);
                grid.Children.Add(titleText);

                var textBox = new TextBox
                {
                    Name = "ProgressTextBox",
                    IsReadOnly = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = new SolidColorBrush(Color.FromRgb(42, 42, 46)),
                    Foreground = Brushes.White,
                    Margin = new Thickness(10),
                    Padding = new Thickness(5),
                    FontFamily = new FontFamily("JetBrains Mono"),
                    FontSize = 12,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                    BorderThickness = new Thickness(1)
                };
                Grid.SetRow(textBox, 1);
                grid.Children.Add(textBox);

                window.Content = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = window.Background,
                    Child = grid,
                    Margin = new Thickness(2)
                };

                return (window, textBox);
            }
            catch
            {
                throw;
            }
        }

        private void ClearDirectory(string directoryPath, TextBox? progressTextBox)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.DirectoryNotFoundOrInvalid);
                return;
            }

            try
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                foreach (var file in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        file.Attributes = FileAttributes.Normal;
                        file.Delete();
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.AccessDeniedToFile, file.FullName, ex.Message));
                    }
                    catch (IOException ex)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.FileInUse, file.FullName, ex.Message));
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorDeletingFile, file.FullName, ex.Message));
                    }
                }

                foreach (var dir in dirInfo.EnumerateDirectories("*", SearchOption.AllDirectories).Reverse())
                {
                    try
                    {
                        dir.Delete(false);
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorDeletingSubdirectory, dir.FullName, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningDirectory, directoryPath, ex.Message));
            }
        }

        private void ClearFilesByExtension(string directoryPath, string extension, TextBox? progressTextBox)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.DirectoryNotFoundOrInvalid);
                return;
            }

            try
            {
                var dirInfo = new DirectoryInfo(directoryPath);
                foreach (var file in dirInfo.EnumerateFiles(extension, SearchOption.AllDirectories))
                {
                    try
                    {
                        file.Attributes = FileAttributes.Normal;
                        file.Delete();
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.AccessDeniedToFile, file.FullName, ex.Message));
                    }
                    catch (IOException ex)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.FileInUse, file.FullName, ex.Message));
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorDeletingFile, file.FullName, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningFilesByExtension, extension, directoryPath, ex.Message));
            }
        }

        private void ClearEventLogsWithWevtutil(TextBox? progressTextBox)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "wevtutil.exe",
                        Arguments = "el",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.GetEncoding(850)
                    }
                };
                process.Start();
                string logs = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (string log in logs.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string logName = log.Trim();
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.CleaningLog, logName));
                    try
                    {
                        var clearProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "wevtutil.exe",
                                Arguments = $"cl \"{logName}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                                RedirectStandardError = true,
                                StandardErrorEncoding = Encoding.GetEncoding(850)
                            }
                        };
                        clearProcess.Start();
                        string error = clearProcess.StandardError.ReadToEnd();
                        clearProcess.WaitForExit();
                        if (!string.IsNullOrEmpty(error))
                            AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.LogError, error));
                        else
                            AppendProgress(progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningLog, logName, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningLogs, ex.Message));
            }
        }

        private void AppendProgress(TextBox? textBox, string message)
        {
            try
            {
                if (textBox != null)
                {
                    textBox.Dispatcher.Invoke(() =>
                    {
                        textBox.AppendText($"{message}\n");
                        textBox.ScrollToEnd();
                    });
                }
            }
            catch
            {
            }
        }

        private async void DisableVisualEffects(object sender, RoutedEventArgs e)
        {
            var confirmationWindow = new Window
            {
                Title = "Confirmação",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Owner = Window.GetWindow(this)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Deseja realmente desativar todos os efeitos visuais?",
                TextWrapping = TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };
            var applyButton = new Button { Content = "Aplicar", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "Cancelar", Width = 80 };
            var revertButton = new Button
            {
                Content = "Reverter",
                Width = 80,
                Margin = new Thickness(10, 0, 0, 0),
                IsEnabled = true
            };

            buttonPanel.Children.Add(applyButton);
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(revertButton);
            stackPanel.Children.Add(buttonPanel);
            confirmationWindow.Content = stackPanel;

            applyButton.Click += async (s, ev) =>
            {
                await DisableVisualEffectsAsync();
            };
            cancelButton.Click += (s, ev) => confirmationWindow.Close();
            revertButton.Click += async (s, ev) =>
            {
                await RevertVisualEffectsAsync();
            };

            confirmationWindow.ShowDialog();
        }

        private async Task DisableVisualEffectsAsync()
        {
            try
            {
                const string DesktopKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop";
                const string WindowMetricsKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics";
                const string AdvancedExplorerKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                const string VisualEffectsKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
                const string DwmKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM";
                const string CursorsKeyPath = @"HKEY_CURRENT_USER\Control Panel\Cursors";
                const string AccessibilityKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Accessibility";

                var registrySettings = new Dictionary<(string Key, string Name), (object Value, RegistryValueKind Kind)>
                {
                    {(DesktopKeyPath, "UserPreferencesMask"), (new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary)},
                    {(VisualEffectsKeyPath, "VisualFXSetting"), (2, RegistryValueKind.DWord)},
                    {(WindowMetricsKeyPath, "MinAnimate"), ("0", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "TaskbarAnimations"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "EnableAeroPeek"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "AlwaysHibernateThumbnails"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "IconsOnly"), (1, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ListviewAlphaSelect"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DragFullWindows"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "FontSmoothing"), ("0", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "ListviewShadow"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "EnableTransparency"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "ColorizationOpaqueBlend"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DisableAnimations"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "Composition"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "MenuAnimation"), ("0", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "ExtendedUIHoverTime"), (10000, RegistryValueKind.DWord)},
                    {(CursorsKeyPath, "CursorShadow"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "SmoothScroll"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ListviewFade"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "WallpaperStyle"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "TileWallpaper"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "WallpaperSlideshow"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ShowInfoTip"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "ComboBoxAnimation"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "ListBoxSmoothScrolling"), ("0", RegistryValueKind.String)},
                    {(WindowMetricsKeyPath, "FadeFullScreen"), ("0", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "IconQuality"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "VisualSoundEffects"), (0, RegistryValueKind.DWord)},
                    {(AccessibilityKeyPath, "HighContrast"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DropShadow"), ("0", RegistryValueKind.String)}
                };

                await Task.Run(() =>
                {
                    Parallel.ForEach(registrySettings, setting =>
                    {
                        var (keyPath, name) = setting.Key;
                        var (newValue, kind) = setting.Value;
                        SetRegistryValue(keyPath, name, newValue, kind);
                    });

                    SystemParametersInfo(0x200, 0, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x1012, 0, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x101E, 0, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x1026, 0, IntPtr.Zero, 0x2);
                });

                MessageBox.Show(ResourceManagerHelper.Instance.VisualEffectsDisabledSuccess,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Acesso negado ao Registro: {ex.Message}", ResourceManagerHelper.Instance.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao desativar efeitos visuais: {ex.Message}", ResourceManagerHelper.Instance.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RevertVisualEffectsAsync()
        {
            try
            {
                const string DesktopKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop";
                const string WindowMetricsKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics";
                const string AdvancedExplorerKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                const string VisualEffectsKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
                const string DwmKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM";
                const string CursorsKeyPath = @"HKEY_CURRENT_USER\Control Panel\Cursors";
                const string AccessibilityKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Accessibility";

                var defaultSettings = new Dictionary<(string Key, string Name), (object Value, RegistryValueKind Kind)>
                {
                    {(DesktopKeyPath, "UserPreferencesMask"), (new byte[] { 0x9E, 0x3E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary)},
                    {(VisualEffectsKeyPath, "VisualFXSetting"), (0, RegistryValueKind.DWord)},
                    {(WindowMetricsKeyPath, "MinAnimate"), ("1", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "TaskbarAnimations"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "EnableAeroPeek"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "AlwaysHibernateThumbnails"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "IconsOnly"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ListviewAlphaSelect"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DragFullWindows"), ("1", RegistryValueKind.String)},
                    {(DesktopKeyPath, "FontSmoothing"), ("2", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "ListviewShadow"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "EnableTransparency"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "ColorizationOpaqueBlend"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DisableAnimations"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "Composition"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "MenuAnimation"), ("1", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "ExtendedUIHoverTime"), (400, RegistryValueKind.DWord)},
                    {(CursorsKeyPath, "CursorShadow"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "SmoothScroll"), (1, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ListviewFade"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "WallpaperStyle"), ("10", RegistryValueKind.String)},
                    {(DesktopKeyPath, "TileWallpaper"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "WallpaperSlideshow"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ShowInfoTip"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "ComboBoxAnimation"), ("1", RegistryValueKind.String)},
                    {(DesktopKeyPath, "ListBoxSmoothScrolling"), ("1", RegistryValueKind.String)},
                    {(WindowMetricsKeyPath, "FadeFullScreen"), ("1", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "IconQuality"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "VisualSoundEffects"), (1, RegistryValueKind.DWord)},
                    {(AccessibilityKeyPath, "HighContrast"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DropShadow"), ("1", RegistryValueKind.String)}
                };

                await Task.Run(() =>
                {
                    Parallel.ForEach(defaultSettings, setting =>
                    {
                        var (keyPath, name) = setting.Key;
                        var (defaultValue, kind) = setting.Value;
                        SetRegistryValue(keyPath, name, defaultValue, kind);
                    });

                    SystemParametersInfo(0x200, 1, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x1012, 1, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x101E, 1, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x1026, 1, IntPtr.Zero, 0x2);
                });

                MessageBox.Show("Efeitos visuais restaurados para os padrões do Windows!", ResourceManagerHelper.Instance.SuccessTitle,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Acesso negado ao Registro ao reverter: {ex.Message}", ResourceManagerHelper.Instance.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao reverter efeitos visuais: {ex.Message}", ResourceManagerHelper.Instance.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetRegistryValue(string keyPath, string name, object value, RegistryValueKind kind)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true) ?? Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key != null)
                    {
                        key.SetValue(name, value, kind);
                    }
                    else
                    {
                        throw new Exception($"Não foi possível abrir ou criar a chave {keyPath}");
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);

        private async void RepairWindows(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.RepairingWindowsTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingWindowsRepair);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    var commands = new List<(string Description, string Command)>
                    {
                        (ResourceManagerHelper.Instance.RunningSFC, "sfc /scannow"),
                        (ResourceManagerHelper.Instance.RunningDISM, "dism /online /cleanup-image /restorehealth"),
                        (ResourceManagerHelper.Instance.SchedulingCHKDSK, "chkdsk /f /r")
                    };

                    foreach (var (description, command) in commands)
                    {
                        AppendProgress(progressTextBox, description);
                        string result = await Task.Run(() => ExecuteCommandWithOutput(command, progressTextBox));
                        if (command.Contains("chkdsk") && result.Contains("agendada"))
                            AppendProgress(progressTextBox, ResourceManagerHelper.Instance.CheckScheduled);
                        else
                            AppendProgress(progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                        await Task.Delay(100);
                    }
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.RepairsCompleted);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.RepairCommandsExecuted,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.GeneralRepairError, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.GeneralRepairError, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private string ExecuteCommandWithOutput(string command, TextBox progressTextBox)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(850),
                    StandardErrorEncoding = Encoding.GetEncoding(850)
                };

                using (Process process = new Process { StartInfo = processInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    string result = output;
                    if (!string.IsNullOrEmpty(error))
                    {
                        result += $"\nErro: {error}";
                        AppendProgress(progressTextBox, $"Erro ao executar '{command}': {error}");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Exceção ao executar comando: {ex.Message}");
                return $"Erro: {ex.Message}";
            }
        }

        private async void EnableHighPerformanceMode(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.HighPerformanceModeTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingHighPerformanceMode);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ActivatingPowerPlan);
                    string result = ExecuteCommandWithOutput("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", progressTextBox);
                    if (result.Contains("sucesso") || string.IsNullOrEmpty(result))
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                    else
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.PowerPlanAlreadyActive);

                    await Task.Delay(100);
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.HighPerformanceModeSuccess);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.HighPerformanceModeActivated,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorActivatingHighPerformanceMode, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorActivatingHighPerformanceMode, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private async void AdjustTimerResolution(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.TimerResolutionTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingTimerAdjustment);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.AdjustingTimer);
                    try
                    {
                        uint currentResolution, desiredResolution = 5000, actualResolution;
                        NtQueryTimerResolution(out currentResolution, out _, out _);
                        NtSetTimerResolution(desiredResolution, true, out actualResolution);
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.TimerAdjusted, (actualResolution / 10000f).ToString("F2")));
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorAdjustingTimerAPI, ex.Message));
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.RunningPowercfgFallback);
                        ExecuteCommandWithOutput("powercfg /energy", progressTextBox);
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.FallbackCompleted);
                    }

                    await Task.Delay(100);
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.TimerAdjustmentCompleted);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.TimerResolutionAdjusted,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorAdjustingTimerResolution, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorAdjustingTimerResolution, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetTimerResolution(uint DesiredResolution, bool Set, out uint CurrentResolution);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryTimerResolution(out uint MinimumResolution, out uint MaximumResolution, out uint CurrentResolution);

        private async void ChangePriority(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                var fileDialog = new OpenFileDialog
                {
                    Filter = "Executáveis (*.exe)|*.exe",
                    Title = ResourceManagerHelper.Instance.SelectExecutableForPriority
                };

                if (fileDialog.ShowDialog() != true)
                {
                    button.IsEnabled = true;
                    return;
                }

                string filePath = fileDialog.FileName;
                string processName = Path.GetFileName(filePath);

                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.ChangingProcessPriorityTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.StartingPriorityChange, processName));

                await Task.Run(async () =>
                {
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.CheckingProcessRunning, processName));
                    string checkResult = ExecuteCommandWithOutput($"wmic process where name='{processName}' get processid", progressTextBox);
                    if (string.IsNullOrEmpty(checkResult) || !checkResult.Contains("ProcessId"))
                    {
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ProcessNotFound);
                        return;
                    }

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ChangingPriorityToHigh);
                    string result = ExecuteCommandWithOutput($"wmic process where name='{processName}' CALL setpriority 256", progressTextBox);
                    if (result.Contains("ReturnValue = 0"))
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                    else
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.PriorityChangeFailed);

                    await Task.Delay(100);
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.PriorityChangeCompleted);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.PriorityChangedToHigh,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorChangingPriority, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorChangingPriority, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private async void UninstallProgram(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null) return;

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                var installedPrograms = await GetInstalledProgramsAsync();
                if (installedPrograms.Count == 0)
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.NoProgramsFound,
                        ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                    button.IsEnabled = true;
                    return;
                }

                var selectionWindow = CreateProgramSelectionWindow(installedPrograms);
                if (selectionWindow.ShowDialog() != true || selectionWindow.Tag == null)
                {
                    button.IsEnabled = true;
                    return;
                }

                var selectedProgram = (InstalledProgram)selectionWindow.Tag;
                string programName = selectedProgram.Name;
                string? uninstallString = selectedProgram.UninstallString;
                string? installLocation = selectedProgram.InstallLocation;

                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.UninstallingProgramTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.StartingUninstallation, programName));

                bool uninstallerExecuted = false;
                if (!string.IsNullOrEmpty(uninstallString))
                {
                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.RunningOfficialUninstaller);
                    uninstallerExecuted = await ExecuteUninstallerAsync(uninstallString, installLocation, progressTextBox);
                    if (!uninstallerExecuted)
                    {
                        AppendProgress(progressTextBox, "Uninstaller failed or not found. Proceeding with deep cleanup.");
                    }
                    else
                    {
                        AppendProgress(progressTextBox, "Uninstaller executed. Verifying removal...");
                        if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                        {
                            AppendProgress(progressTextBox, "Program files still detected. Forcing deep cleanup.");
                            uninstallerExecuted = false;
                        }
                    }
                }
                else
                {
                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.NoUninstallerFound);
                    AppendProgress(progressTextBox, "Proceeding with deep cleanup due to missing uninstaller.");
                }

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ScanningLeftovers);
                var leftovers = await ScanForLeftoversAsync(programName, installLocation, progressTextBox);
                if (leftovers.HasLeftovers || !uninstallerExecuted)
                {
                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.FoundLeftovers);
                    await CleanupLeftoversAsync(leftovers, progressTextBox);
                }
                else
                {
                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.NoLeftoversFound);
                }

                await RemoveFromInstalledProgramsAsync(programName, progressTextBox);

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.UninstallationCompleted);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.ProgramUninstalledSuccess,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorUninstallingProgram, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorUninstallingProgram, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        public class InstalledProgram
        {
            public string Name { get; set; }
            public string? UninstallString { get; set; }
            public string? InstallLocation { get; set; }
        }

        public class Leftovers
        {
            public List<string> Files { get; set; } = new List<string>();
            public List<string> RegistryKeys { get; set; } = new List<string>();
            public bool HasLeftovers => Files.Count > 0 || RegistryKeys.Count > 0;
        }

        private async Task<Leftovers> ScanForLeftoversAsync(string programName, string? installLocation, TextBox progressTextBox)
        {
            var leftovers = new Leftovers();
            await Task.Run(() =>
            {
                AppendProgress(progressTextBox, $"Scanning for leftovers of '{programName}'...");

                string[] commonPaths = {
            @"C:\Program Files",
            @"C:\Program Files (x86)",
            @"C:\ProgramData",
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86)
        };

                if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    AppendProgress(progressTextBox, $"Explicitly scanning InstallLocation: '{installLocation}'");
                    try
                    {
                        var dirs = Directory.GetDirectories(installLocation, $"*{programName}*", SearchOption.AllDirectories);
                        leftovers.Files.AddRange(dirs);
                        AppendProgress(progressTextBox, $"Found {dirs.Length} directories in InstallLocation");

                        var files = Directory.GetFiles(installLocation, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.Contains(programName, StringComparison.OrdinalIgnoreCase) ||
                                       Path.GetExtension(f).ToLower() is ".log" or ".tmp");
                        leftovers.Files.AddRange(files);
                        AppendProgress(progressTextBox, $"Found {files.Count()} files in InstallLocation");
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Error scanning InstallLocation: {ex.Message}");
                    }
                }

                foreach (var path in commonPaths)
                {
                    if (Directory.Exists(path))
                    {
                        try
                        {
                            var dirs = Directory.GetDirectories(path, $"*{programName}*", SearchOption.AllDirectories);
                            leftovers.Files.AddRange(dirs);
                            AppendProgress(progressTextBox, $"Found {dirs.Length} directories in '{path}'");

                            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                .Where(f => f.Contains(programName, StringComparison.OrdinalIgnoreCase) ||
                                           Path.GetExtension(f).ToLower() is ".log" or ".tmp");
                            leftovers.Files.AddRange(files);
                            AppendProgress(progressTextBox, $"Found {files.Count()} files in '{path}'");
                        }
                        catch (Exception ex)
                        {
                            AppendProgress(progressTextBox, $"Error scanning '{path}': {ex.Message}");
                        }
                    }
                }

                string[] regPaths = { @"SOFTWARE", @"SOFTWARE\Wow6432Node" };
                foreach (var regPath in regPaths)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(regPath))
                    {
                        if (key != null)
                        {
                            try
                            {
                                var matchingKeys = key.GetSubKeyNames()
                                    .Where(k => k.Contains(programName, StringComparison.OrdinalIgnoreCase));
                                leftovers.RegistryKeys.AddRange(matchingKeys.Select(k => $@"HKEY_LOCAL_MACHINE\{regPath}\{k}"));
                                AppendProgress(progressTextBox, $"Found {matchingKeys.Count()} registry keys in '{regPath}'");
                            }
                            catch (Exception ex)
                            {
                                AppendProgress(progressTextBox, $"Error scanning registry '{regPath}': {ex.Message}");
                            }
                        }
                    }
                }
            });
            return leftovers;
        }

        private async Task<List<InstalledProgram>> GetInstalledProgramsAsync()
        {
            return await Task.Run(() =>
            {
                var programs = new List<InstalledProgram>();
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                var name = subKey?.GetValue("DisplayName")?.ToString();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    programs.Add(new InstalledProgram
                                    {
                                        Name = name,
                                        UninstallString = subKey?.GetValue("UninstallString")?.ToString(),
                                        InstallLocation = subKey?.GetValue("InstallLocation")?.ToString()
                                    });
                                }
                            }
                        }
                    }
                }
                return programs.OrderBy(p => p.Name).ToList();
            });
        }

        private Window CreateProgramSelectionWindow(List<InstalledProgram> programs)
        {
            var window = new Window
            {
                Title = ResourceManagerHelper.Instance.SelectProgramToUninstall,
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                BorderThickness = new Thickness(1)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            window.Content = grid;

            var searchBox = new TextBox
            {
                Margin = new Thickness(10, 10, 10, 5),
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                Foreground = Brushes.WhiteSmoke,
                BorderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Padding = new Thickness(5),
                Text = "Pesquisar programas..."
            };
            searchBox.GotFocus += (s, e) => { if (searchBox.Text == "Pesquisar programas...") searchBox.Text = ""; };
            searchBox.LostFocus += (s, e) => { if (string.IsNullOrEmpty(searchBox.Text)) searchBox.Text = "Pesquisar programas..."; };
            Grid.SetRow(searchBox, 0);
            grid.Children.Add(searchBox);

            var listBox = new ListBox
            {
                ItemsSource = programs,
                DisplayMemberPath = "Name",
                Margin = new Thickness(10, 0, 10, 10),
                Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
                Foreground = Brushes.WhiteSmoke,
                BorderThickness = new Thickness(0)
            };
            listBox.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Hidden);
            listBox.SetValue(ScrollViewer.CanContentScrollProperty, true);

            var listBoxItemStyle = new Style(typeof(ListBoxItem));
            listBoxItemStyle.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, Brushes.Transparent));
            listBoxItemStyle.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.WhiteSmoke));
            listBoxItemStyle.Setters.Add(new Setter(ListBoxItem.PaddingProperty, new Thickness(5)));
            var hoverTrigger = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(50, 50, 50))));
            listBoxItemStyle.Triggers.Add(hoverTrigger);
            var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(ListBoxItem.BackgroundProperty, new SolidColorBrush(Color.FromRgb(70, 130, 180))));
            selectedTrigger.Setters.Add(new Setter(ListBoxItem.ForegroundProperty, Brushes.White));
            listBoxItemStyle.Triggers.Add(selectedTrigger);
            listBox.ItemContainerStyle = listBoxItemStyle;

            Grid.SetRow(listBox, 1);
            grid.Children.Add(listBox);

            searchBox.TextChanged += (s, e) =>
            {
                if (searchBox.Text == "Pesquisar programas..." || string.IsNullOrEmpty(searchBox.Text))
                {
                    listBox.ItemsSource = programs;
                }
                else
                {
                    listBox.ItemsSource = programs.Where(p => p.Name.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            };

            var confirmButton = new Button
            {
                Content = "Desinstalar",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 10),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand
            };
            var buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, CreateButtonTemplate()));
            confirmButton.Style = buttonStyle;

            confirmButton.Click += (s, e) =>
            {
                window.DialogResult = true;
                window.Close();
            };
            Grid.SetRow(confirmButton, 2);
            grid.Children.Add(confirmButton);

            window.Tag = null;
            listBox.SelectionChanged += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    window.Tag = listBox.SelectedItem as InstalledProgram;
                }
            };

            return window;
        }

        private ControlTemplate CreateButtonTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(60, 60, 60)));
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(80, 80, 80)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(contentPresenter);

            var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(80, 80, 80))));
            template.Triggers.Add(trigger);

            template.VisualTree = border;
            return template;
        }

        private async Task<bool> ExecuteUninstallerAsync(string uninstallString, string? installLocation, TextBox progressTextBox)
        {
            try
            {
                AppendProgress(progressTextBox, $"Raw uninstall string from registry: '{uninstallString}'");
                uninstallString = uninstallString.Trim();

                string fileName = uninstallString;
                string arguments = "";
                if (uninstallString.Contains(" "))
                {
                    if (uninstallString.StartsWith("\""))
                    {
                        int endQuote = uninstallString.IndexOf("\"", 1);
                        if (endQuote != -1)
                        {
                            fileName = uninstallString.Substring(1, endQuote - 1);
                            arguments = uninstallString.Substring(endQuote + 1).Trim();
                        }
                    }
                    else
                    {
                        fileName = uninstallString.Substring(0, uninstallString.IndexOf(" ")).Trim();
                        arguments = uninstallString.Substring(uninstallString.IndexOf(" ")).Trim();
                    }
                }

                AppendProgress(progressTextBox, $"Parsed fileName: '{fileName}'");
                AppendProgress(progressTextBox, $"Parsed arguments: '{arguments}'");

                if (!File.Exists(fileName))
                {
                    AppendProgress(progressTextBox, $"Uninstaller file not found at: '{fileName}'");

                    if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                    {
                        AppendProgress(progressTextBox, $"Checking InstallLocation: '{installLocation}'");
                        var possibleUninstaller = Path.Combine(installLocation, "tesseract-uninstall.exe");
                        if (!File.Exists(possibleUninstaller))
                        {
                            possibleUninstaller = Path.Combine(installLocation, "uninstall.exe");
                        }

                        if (File.Exists(possibleUninstaller))
                        {
                            fileName = possibleUninstaller;
                            arguments = "";
                            AppendProgress(progressTextBox, $"Found alternative uninstaller: '{fileName}'");
                        }
                        else
                        {
                            AppendProgress(progressTextBox, "No alternative uninstaller found in InstallLocation.");
                            return false;
                        }
                    }
                    else
                    {
                        AppendProgress(progressTextBox, "InstallLocation not provided or invalid.");
                        return false;
                    }
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        Verb = "runas"
                    }
                };

                AppendProgress(progressTextBox, $"Starting uninstaller: '{fileName} {arguments}'");
                process.Start();
                await process.WaitForExitAsync();

                AppendProgress(progressTextBox, $"Uninstaller finished with exit code: {process.ExitCode}");
                bool initialSuccess = process.ExitCode == 0 || process.ExitCode == 3010;

                if (initialSuccess && !string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    AppendProgress(progressTextBox, $"Installation directory still exists: '{installLocation}'");
                    return false;
                }

                return initialSuccess;
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Uninstaller error: {ex.Message}");
                if (ex.Message.Contains("access is denied"))
                {
                    AppendProgress(progressTextBox, "Please run the application as administrator.");
                }
                return false;
            }
        }

        private async Task RemoveFromInstalledProgramsAsync(string programName, TextBox progressTextBox)
        {
            await Task.Run(() =>
            {
                AppendProgress(progressTextBox, $"Removing {programName} from installed programs list...");
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
                    {
                        if (key != null)
                        {
                            foreach (var subKeyName in key.GetSubKeyNames())
                            {
                                using (var subKey = key.OpenSubKey(subKeyName, true))
                                {
                                    if (subKey != null)
                                    {
                                        var name = subKey.GetValue("DisplayName")?.ToString();
                                        if (!string.IsNullOrEmpty(name) && name.Contains(programName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            key.DeleteSubKeyTree(subKeyName);
                                            AppendProgress(progressTextBox, $"Removed registry entry: HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{subKeyName}");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", true))
                    {
                        if (key != null)
                        {
                            foreach (var subKeyName in key.GetSubKeyNames())
                            {
                                using (var subKey = key.OpenSubKey(subKeyName, true))
                                {
                                    if (subKey != null)
                                    {
                                        var name = subKey.GetValue("DisplayName")?.ToString();
                                        if (!string.IsNullOrEmpty(name) && name.Contains(programName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            key.DeleteSubKeyTree(subKeyName);
                                            AppendProgress(progressTextBox, $"Removed registry entry: HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{subKeyName}");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendProgress(progressTextBox, $"Error removing from installed programs: {ex.Message}");
                }
            });
        }

        private async Task CleanupLeftoversAsync(Leftovers leftovers, TextBox progressTextBox)
        {
            await Task.Run(() =>
            {
                foreach (var file in leftovers.Files.Where(f => File.Exists(f)))
                {
                    try
                    {
                        File.Delete(file);
                        AppendProgress(progressTextBox, $"Removed file: '{file}'");
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Failed to remove file '{file}': {ex.Message}");
                    }
                }

                foreach (var dir in leftovers.Files.Where(f => Directory.Exists(f)))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        AppendProgress(progressTextBox, $"Removed directory and contents: '{dir}'");
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Failed to remove directory '{dir}': {ex.Message}");
                        try
                        {
                            foreach (var subFile in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    File.Delete(subFile);
                                    AppendProgress(progressTextBox, $"Forced removal of file: '{subFile}'");
                                }
                                catch (Exception subEx)
                                {
                                    AppendProgress(progressTextBox, $"Failed to force remove '{subFile}': {subEx.Message}");
                                }
                            }
                            Directory.Delete(dir, true);
                            AppendProgress(progressTextBox, $"Successfully forced removal of directory: '{dir}'");
                        }
                        catch (Exception subEx)
                        {
                            AppendProgress(progressTextBox, $"Final failure to remove '{dir}': {subEx.Message}");
                        }
                    }
                }

                foreach (var keyPath in leftovers.RegistryKeys)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE", true))
                        {
                            if (key != null && keyPath.StartsWith(@"HKEY_LOCAL_MACHINE\SOFTWARE\"))
                            {
                                key.DeleteSubKeyTree(keyPath.Replace(@"HKEY_LOCAL_MACHINE\SOFTWARE\", ""), false);
                                AppendProgress(progressTextBox, $"Removed registry key: '{keyPath}'");
                            }
                        }
                        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node", true))
                        {
                            if (key != null && keyPath.StartsWith(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\"))
                            {
                                key.DeleteSubKeyTree(keyPath.Replace(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\", ""), false);
                                AppendProgress(progressTextBox, $"Removed registry key: '{keyPath}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Failed to remove registry key '{keyPath}': {ex.Message}");
                    }
                }
            });
        }

        private static readonly DependencyProperty SelectedProgramProperty =
            DependencyProperty.Register("SelectedProgram", typeof(InstalledProgram), typeof(Window));

        public static InstalledProgram? GetSelectedProgram(Window window) =>
            (InstalledProgram?)window.GetValue(SelectedProgramProperty);

        public static void SetSelectedProgram(Window window, InstalledProgram? value) =>
            window.SetValue(SelectedProgramProperty, value);

        private async void AtivarWindows(object sender, RoutedEventArgs e)
        {
            try
            {
                string comando = "irm https://massgrave.dev/get | iex";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoExit -Command \"{comando}\"",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                await Task.Run(() => Process.Start(psi));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorActivatingWindows, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CleanRegistry(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.CleaningRegistryTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingRegistryCleanup);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    string[] registryPaths =
                    {
                        @"Software\Microsoft\Windows\CurrentVersion\Run",
                        @"Software\Microsoft\Windows\CurrentVersion\RunOnce"
                    };

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ScanningRegistry);

                    List<(string KeyPath, string Name, string Value)> invalidEntries = new();
                    foreach (var path in registryPaths)
                    {
                        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
                        {
                            string fullPath = root == Registry.CurrentUser ? $"HKEY_CURRENT_USER\\{path}" : $"HKEY_LOCAL_MACHINE\\{path}";
                            using (var key = root.OpenSubKey(path))
                            {
                                if (key != null)
                                {
                                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.CheckingRegistryPath, fullPath));
                                    foreach (var valueName in key.GetValueNames())
                                    {
                                        string? value = key.GetValue(valueName)?.ToString();
                                        if (!string.IsNullOrEmpty(value) && value.Contains(".exe") && !File.Exists(value))
                                        {
                                            invalidEntries.Add((fullPath, valueName, value));
                                            AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.CheckingRegistryPath, fullPath));
                                        }
                                    }
                                }
                                else
                                {
                                    AppendProgress(progressTextBox, $"{fullPath} não acessível ou inexistente.\n");
                                }
                            }
                        }
                    }

                    if (invalidEntries.Count == 0)
                    {
                        AppendProgress(progressTextBox, "Nenhuma entrada inválida encontrada.\n");
                        return;
                    }

                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.InvalidEntriesFound, invalidEntries.Count));
                    foreach (var entry in invalidEntries)
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.InvalidEntryDetail, entry.KeyPath, entry.Name, entry.Value));
                    }

                    bool confirmed = await progressTextBox.Dispatcher.InvokeAsync(() =>
                    {
                        var result = MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ConfirmRemoveInvalidEntries, invalidEntries.Count),
                        ResourceManagerHelper.Instance.ConfirmationTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
                        return result == MessageBoxResult.Yes;
                    });

                    if (confirmed)
                    {
                        foreach (var entry in invalidEntries)
                        {
                            try
                            {
                                using (var key = entry.KeyPath.StartsWith("HKEY_CURRENT_USER") ?
                                    Registry.CurrentUser.OpenSubKey(entry.KeyPath.Replace("HKEY_CURRENT_USER\\", ""), writable: true) :
                                    Registry.LocalMachine.OpenSubKey(entry.KeyPath.Replace("HKEY_LOCAL_MACHINE\\", ""), writable: true))
                                {
                                    if (key != null)
                                    {
                                        key.DeleteValue(entry.Name);
                                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.EntryRemoved, entry.KeyPath, entry.Name));
                                    }
                                    else
                                    {
                                        AppendProgress(progressTextBox, $"Erro: Não foi possível abrir {entry.KeyPath} para escrita.\n");
                                    }
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.PermissionDeniedRemovingEntry, entry.KeyPath, entry.Name));
                            }
                            catch (Exception ex)
                            {
                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorRemovingEntry, entry.KeyPath, entry.Name, ex.Message));
                            }
                        }
                    }
                    else
                    {
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.CleanupCancelledByUser);
                    }

                    await Task.Delay(100);
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.RegistryCleanupCompleted);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.RegistryCleanupCompleted,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningRegistry, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCleaningRegistry, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private async void ManageStartupPrograms(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.ManagingStartupProgramsTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingStartupManagement);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    string[] startupPaths =
                    {
                        @"Software\Microsoft\Windows\CurrentVersion\Run",
                        @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                        @"Software\Microsoft\Windows\CurrentVersion\RunServices"
                    };

                    string userStartupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    string allUsersStartupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup");

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ListingStartupPrograms);
                    Dictionary<string, (string Path, string Source, bool IsRegistry, RegistryKey? Root)> startupItems = new();

                    foreach (var path in startupPaths)
                    {
                        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
                        {
                            string fullPath = root == Registry.CurrentUser ? $"HKCU\\{path}" : $"HKLM\\{path}";
                            using (var key = root.OpenSubKey(path, writable: false))
                            {
                                if (key != null)
                                {
                                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.CheckingStartupPath, fullPath));
                                    foreach (var name in key.GetValueNames())
                                    {
                                        string? value = key.GetValue(name)?.ToString();
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            startupItems[name] = (value, fullPath, true, root);
                                            AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.StartupItemFound, fullPath, name, value));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    foreach (var folder in new[] { userStartupFolder, allUsersStartupFolder })
                    {
                        if (Directory.Exists(folder))
                        {
                            AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.CheckingStartupFolder, folder));
                            foreach (var file in Directory.EnumerateFiles(folder, "*.lnk", SearchOption.TopDirectoryOnly))
                            {
                                string name = Path.GetFileNameWithoutExtension(file);
                                string targetPath = GetShortcutTarget(file);
                                if (!string.IsNullOrEmpty(targetPath))
                                {
                                    startupItems[name] = (targetPath, folder, false, null);
                                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.StartupFolderItemFound, folder, name, targetPath));
                                }
                            }
                        }
                    }

                    if (startupItems.Count == 0)
                    {
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.NoStartupProgramsFound);
                        return;
                    }

                    await progressTextBox.Dispatcher.InvokeAsync(() =>
                    {
                        var selectionWindow = new Window
                        {
                            Title = ResourceManagerHelper.Instance.SelectProgramToDisableTitle,
                            Width = 400,
                            Height = 300,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                            BorderThickness = new Thickness(1)
                        };

                        var stackPanel = new StackPanel { Margin = new Thickness(10) };
                        var listBox = new ListBox
                        {
                            Height = 200,
                            Background = new SolidColorBrush(Color.FromRgb(42, 42, 46)),
                            Foreground = Brushes.White,
                            BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
                        };
                        var disableButton = new Button
                        {
                            Content = ResourceManagerHelper.Instance.DisableSelectedButton,
                            Margin = new Thickness(0, 10, 0, 0),
                            Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                            Foreground = Brushes.White,
                            BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                            BorderThickness = new Thickness(1),
                            Padding = new Thickness(5)
                        };

                        foreach (var item in startupItems)
                        {
                            listBox.Items.Add($"{item.Key} ({item.Value.Source}) -> {item.Value.Path}");
                        }

                        disableButton.Click += (s, ev) =>
                        {
                            if (listBox.SelectedItem != null)
                            {
                                string selectedFull = listBox.SelectedItem.ToString();
                                string selectedName = selectedFull.Split(" -> ")[0].Split(" (")[0];
                                if (startupItems.TryGetValue(selectedName, out var item))
                                {
                                    try
                                    {
                                        if (item.IsRegistry && item.Root != null)
                                        {
                                            using (var key = item.Root.OpenSubKey(item.Source.Replace(item.Root.Name + "\\", ""), writable: true))
                                            {
                                                if (key != null)
                                                {
                                                    key.DeleteValue(selectedName);
                                                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ProgramDisabledInRegistry, selectedName));
                                                }
                                                else
                                                {
                                                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorOpeningStartupSource, item.Source));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            string shortcutPath = Path.Combine(item.Source, $"{selectedName}.lnk");
                                            if (File.Exists(shortcutPath))
                                            {
                                                File.Delete(shortcutPath);
                                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ProgramRemovedFromStartupFolder, selectedName));
                                            }
                                            else
                                            {
                                                AppendProgress(progressTextBox, $"Erro: Atalho {shortcutPath} não encontrado.\n");
                                            }
                                        }
                                        listBox.Items.Remove(listBox.SelectedItem);
                                    }
                                    catch (UnauthorizedAccessException)
                                    {
                                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.PermissionDeniedDisablingProgram, selectedName));
                                    }
                                    catch (Exception ex)
                                    {
                                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorDisablingProgram, selectedName, ex.Message));
                                    }
                                }
                            }
                        };

                        stackPanel.Children.Add(listBox);
                        stackPanel.Children.Add(disableButton);
                        selectionWindow.Content = stackPanel;
                        selectionWindow.ShowDialog();
                    });
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartupManagementCompleted);
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorManagingStartupPrograms, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorManagingStartupPrograms, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return string.Empty;

                object? shell = Activator.CreateInstance(shellType);
                if (shell == null) return string.Empty;

                object? shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                if (shortcut == null) return string.Empty;

                string target = (string)shortcut.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, shortcut, null);
                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);
                return target ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em GetShortcutTarget {ex.Message}");
                return string.Empty;
            }
        }

        private async void CleanNetworkData(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.CleaningNetworkDataTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingNetworkDataCleanup);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    string[] networkCommands =
                    {
                        "ipconfig /flushdns",
                        "ipconfig /release",
                        "ipconfig /renew",
                        "nbtstat -R",
                        "nbtstat -RR",
                        "netsh winsock reset",
                        "netsh int ip reset"
                    };

                    foreach (string command in networkCommands)
                    {
                        AppendProgress(progressTextBox, $"Executando: {command}...\n");
                        string result = await RunCommandAsync(command);
                        AppendProgress(progressTextBox, result + "\n");
                        await Task.Delay(100);
                    }
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.NetworkDataCleanupCompleted);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.NetworkDataCleanupSuccess,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningNetworkData, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCleaningNetworkData, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private async Task<string> RunCommandAsync(string command)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    throw new Exception(error);
                }

                return output;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao executar comando: {ex.Message}");
            }
        }

        private async void RunSpaceSniffer(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.RunningSpaceSnifferTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingSpaceSniffer);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    string assetsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                    string executablePath = Path.Combine(assetsFolder, "SpaceSniffer.exe");

                    if (!Directory.Exists(assetsFolder))
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.AssetsFolderNotFound, AppDomain.CurrentDomain.BaseDirectory));
                        throw new DirectoryNotFoundException("Pasta 'assets' não encontrada.");
                    }

                    if (!File.Exists(executablePath))
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.SpaceSnifferNotFound, assetsFolder));
                        throw new FileNotFoundException($"Executável 'SpaceSniffer.exe' não encontrado em {assetsFolder}.");
                    }

                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ExecutingSpaceSniffer, executablePath));

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };

                    using (Process process = Process.Start(psi))
                    {
                        if (process == null)
                        {
                            AppendProgress(progressTextBox, ResourceManagerHelper.Instance.FailedToStartProcess);
                            return;
                        }

                        await Task.Run(() => process.WaitForExit());
                        int exitCode = process.ExitCode;
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ProcessCompletedWithExitCode, exitCode));
                    }
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.SpaceSnifferExecutedSuccess);
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorRunningSpaceSniffer, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorRunningSpaceSniffer, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private void OpenWindowsDefender(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("C:\\Windows\\System32\\mrt.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorOpeningMRT, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveWindowsBloatware(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.CheckingInstalledAppsTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.CheckingInstalledApps);

                var potentialBloatware = new Dictionary<string, string>
                {
                    {"Microsoft.OneDrive", "OneDrive"},
                    {"Microsoft.Edge", "Microsoft Edge"},
                    {"Microsoft.BingNews", "Bing News"},
                    {"Microsoft.BingWeather", "Bing Weather"},
                    {"Microsoft.GetHelp", "Get Help"},
                    {"Microsoft.Getstarted", "Get Started"},
                    {"Microsoft.MicrosoftOfficeHub", "Office Hub"},
                    {"Microsoft.MicrosoftSolitaireCollection", "Solitaire Collection"},
                    {"Microsoft.People", "People"},
                    {"Microsoft.WindowsFeedbackHub", "Feedback Hub"},
                    {"Microsoft.WindowsMaps", "Windows Maps"},
                    {"Microsoft.Xbox.TCUI", "Xbox TCUI"},
                    {"Microsoft.XboxApp", "Xbox App"},
                    {"Microsoft.XboxGameOverlay", "Xbox Game Overlay"},
                    {"Microsoft.XboxGamingOverlay", "Xbox Gaming Overlay"},
                    {"Microsoft.XboxIdentityProvider", "Xbox Identity Provider"},
                    {"Microsoft.XboxSpeechToTextOverlay", "Xbox Speech To Text Overlay"},
                    {"Microsoft.ZuneMusic", "Groove Music"},
                    {"Microsoft.ZuneVideo", "Movies & TV"}
                };

                var installedApps = new Dictionary<string, string>();

                await Task.Run(async () =>
                {
                    if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive")) ||
                        Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive")))
                    {
                        installedApps.Add("Microsoft.OneDrive", potentialBloatware["Microsoft.OneDrive"]);
                        AppendProgress(progressTextBox, "Encontrado: OneDrive\n");
                    }

                    if (Directory.Exists(@"C:\Program Files (x86)\Microsoft\Edge") ||
                        Directory.Exists(@"C:\Program Files\Microsoft\Edge"))
                    {
                        installedApps.Add("Microsoft.Edge", potentialBloatware["Microsoft.Edge"]);
                        AppendProgress(progressTextBox, "Encontrado: Microsoft Edge\n");
                    }

                    foreach (var app in potentialBloatware)
                    {
                        if (app.Key != "Microsoft.OneDrive" && app.Key != "Microsoft.Edge")
                        {
                            string result = await RunCommandAsync($"powershell -Command \"Get-AppxPackage {app.Key} | Select-Object -ExpandProperty Name\"");
                            if (!string.IsNullOrEmpty(result) && result.Contains(app.Key))
                            {
                                installedApps.Add(app.Key, app.Value);
                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.AppFound, app.Value));
                            }
                        }
                    }
                });

                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }

                if (!installedApps.Any())
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.NoBloatwareFound,
                    ResourceManagerHelper.Instance.InfoTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                    button.IsEnabled = true;
                    return;
                }

                var selectionWindow = new Window
                {
                    Title = ResourceManagerHelper.Instance.SelectAppsToRemoveTitle,
                    Width = 500,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                    BorderThickness = new Thickness(1)
                };

                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var titleText = new TextBlock
                {
                    Text = string.Format(ResourceManagerHelper.Instance.AppsFoundToRemove, installedApps.Count),
                    Foreground = Brushes.White,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(titleText, 0);

                var scrollViewer = new ScrollViewer
                {
                    Margin = new Thickness(10),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                Grid.SetRow(scrollViewer, 1);

                var checkBoxPanel = new StackPanel { Margin = new Thickness(10) };
                var checkBoxes = new Dictionary<string, CheckBox>();

                foreach (var app in installedApps)
                {
                    var checkBox = new CheckBox
                    {
                        Content = app.Value,
                        Foreground = Brushes.White,
                        Margin = new Thickness(5),
                        Tag = app.Key
                    };
                    checkBoxes[app.Key] = checkBox;
                    checkBoxPanel.Children.Add(checkBox);
                }

                scrollViewer.Content = checkBoxPanel;

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                };
                Grid.SetRow(buttonPanel, 2);

                var selectAllButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.SelectAllButton,
                    Width = 120,
                    Height = 30,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
                };

                var deselectAllButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.DeselectAllButton,
                    Width = 120,
                    Height = 30,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
                };

                var confirmButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.RemoveSelectedButton,
                    Width = 150,
                    Height = 30,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
                };

                selectAllButton.Click += (s, ev) =>
                {
                    foreach (var cb in checkBoxes.Values)
                        cb.IsChecked = true;
                };

                deselectAllButton.Click += (s, ev) =>
                {
                    foreach (var cb in checkBoxes.Values)
                        cb.IsChecked = false;
                };

                bool? dialogResult = null;
                confirmButton.Click += (s, ev) =>
                {
                    dialogResult = true;
                    selectionWindow.Close();
                };

                buttonPanel.Children.Add(selectAllButton);
                buttonPanel.Children.Add(deselectAllButton);
                buttonPanel.Children.Add(confirmButton);

                mainGrid.Children.Add(titleText);
                mainGrid.Children.Add(scrollViewer);
                mainGrid.Children.Add(buttonPanel);

                selectionWindow.Content = mainGrid;
                selectionWindow.ShowDialog();

                if (dialogResult != true)
                {
                    button.IsEnabled = true;
                    return;
                }

                var selectedApps = checkBoxes.Where(cb => cb.Value.IsChecked == true)
                                           .ToDictionary(cb => cb.Key, cb => installedApps[cb.Key]);

                if (!selectedApps.Any())
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.NoAppsSelectedForRemoval,
                    ResourceManagerHelper.Instance.WarningTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                    button.IsEnabled = true;
                    return;
                }

                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.RemovingBloatwareTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingBloatwareRemoval);

                await Task.Run(async () =>
                {
                    if (selectedApps.ContainsKey("Microsoft.OneDrive"))
                    {
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.RemovingOneDrive);
                        await RunCommandAsync("taskkill /f /im OneDrive.exe");
                        await RunCommandAsync("%SystemRoot%\\System32\\OneDriveSetup.exe /uninstall");
                        await RunCommandAsync("%SystemRoot%\\SysWOW64\\OneDriveSetup.exe /uninstall");
                    }

                    if (selectedApps.ContainsKey("Microsoft.Edge"))
                    {
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.RemovingMicrosoftEdge);
                        string edgeUninstaller = @"C:\Program Files (x86)\Microsoft\Edge\Application\*\Installer\setup.exe";
                        if (Directory.Exists(Path.GetDirectoryName(edgeUninstaller)))
                        {
                            string[] installerPaths = Directory.GetFiles(@"C:\Program Files (x86)\Microsoft\Edge\Application", "setup.exe", SearchOption.AllDirectories);
                            foreach (string installer in installerPaths)
                            {
                                await RunCommandAsync($"\"{installer}\" --uninstall --system-level --verbose-logging --force-uninstall");
                            }
                        }
                    }

                    foreach (var app in selectedApps)
                    {
                        if (app.Key != "Microsoft.OneDrive" && app.Key != "Microsoft.Edge")
                        {
                            AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.RemovingApp, app.Value));
                            string result = await RunCommandAsync($"powershell -Command \"Get-AppxPackage *{app.Key}* | Remove-AppxPackage\"");
                            if (!string.IsNullOrEmpty(result))
                            {
                                AppendProgress(progressTextBox, result + "\n");
                            }
                            await Task.Delay(100);
                        }
                    }

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.CleaningResidualFiles);
                    var foldersToDelete = new List<string>();

                    if (selectedApps.ContainsKey("Microsoft.Edge"))
                    {
                        foldersToDelete.Add(@"%LOCALAPPDATA%\Microsoft\Edge");
                        foldersToDelete.Add(@"%LOCALAPPDATA%\Microsoft\EdgeUpdate");
                    }

                    if (selectedApps.ContainsKey("Microsoft.OneDrive"))
                    {
                        foldersToDelete.Add(@"%LOCALAPPDATA%\Microsoft\OneDrive");
                        foldersToDelete.Add(@"%PROGRAMDATA%\Microsoft\OneDrive");
                    }

                    foreach (var folder in foldersToDelete)
                    {
                        string expandedPath = Environment.ExpandEnvironmentVariables(folder);
                        if (Directory.Exists(expandedPath))
                        {
                            try
                            {
                                Directory.Delete(expandedPath, true);
                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.FolderDeleted, expandedPath));
                            }
                            catch (Exception ex)
                            {
                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorDeletingFolder, expandedPath, ex.Message));
                            }
                        }
                    }
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.BloatwareRemovalCompleted);
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.RestartRecommended);

                var result = await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.BloatwareRemovalDone,
                ResourceManagerHelper.Instance.CompletedTitle, MessageBoxButton.YesNo, MessageBoxImage.Question));

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("shutdown.exe", "/r /t 10");
                    MessageBox.Show(ResourceManagerHelper.Instance.RestartingIn10Seconds,
                    ResourceManagerHelper.Instance.RestartingTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorRemovingBloatware, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorRemovingBloatware, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        private async Task<(double UsedMemory, double AvailableMemory)> GetMemoryInfo()
        {
            try
            {
                string result = await RunCommandAsync("powershell -Command \"Get-CimInstance Win32_OperatingSystem | Select-Object -Property TotalVisibleMemorySize,FreePhysicalMemory | ConvertTo-Json\"");

                result = result.Trim().Replace("\r", "").Replace("\n", "");

                int totalIndex = result.IndexOf("TotalVisibleMemorySize") + "TotalVisibleMemorySize".Length + 2;
                int freeIndex = result.IndexOf("FreePhysicalMemory") + "FreePhysicalMemory".Length + 2;

                string totalStr = result.Substring(totalIndex, result.IndexOf(",", totalIndex) - totalIndex);
                string freeStr = result.Substring(freeIndex, result.IndexOf("}", freeIndex) - freeIndex);

                if (double.TryParse(totalStr, out double totalMemoryKB) && double.TryParse(freeStr, out double freeMemoryKB))
                {
                    double totalMemoryGB = totalMemoryKB / (1024 * 1024);
                    double freeMemoryGB = freeMemoryKB / (1024 * 1024);
                    double usedMemoryGB = totalMemoryGB - freeMemoryGB;

                    return (usedMemoryGB, freeMemoryGB);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em GetMemoryInfo {ex.Message}");
            }

            try
            {
                var computerInfo = new Microsoft.VisualBasic.Devices.ComputerInfo();
                double totalMemoryGB = computerInfo.TotalPhysicalMemory / (1024.0 * 1024 * 1024);
                double availableMemoryGB = computerInfo.AvailablePhysicalMemory / (1024.0 * 1024 * 1024);
                double usedMemoryGB = totalMemoryGB - availableMemoryGB;

                return (usedMemoryGB, availableMemoryGB);
            }
            catch (Exception ex)
            {
            }

            return (0, 0);
        }

        private async void OptimizeMemory(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null) return;

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.OptimizingMemoryTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingMemoryOptimization);

                await Task.Run(async () =>
                {
                    var initialMemoryInfo = await GetMemoryInfo();
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryUsageBefore, initialMemoryInfo.UsedMemory.ToString("N2")));
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryAvailableBefore, initialMemoryInfo.AvailableMemory.ToString("N2")));

                    var safeProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "chrome", "firefox", "msedge", "iexplore", "opera",
                        "notepad", "wordpad", "mspaint", "calc",
                        "winrar", "7zg", "vlc", "wmplayer",
                        "explorer", "powershell", "cmd"
                    };

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.OptimizingNonEssentialProcesses);
                    var processes = Process.GetProcesses();
                    int processesOptimized = 0;

                    foreach (var process in processes)
                    {
                        try
                        {
                            if (!process.HasExited &&
                                process.Id != Process.GetCurrentProcess().Id &&
                                safeProcesses.Contains(process.ProcessName.ToLower()))
                            {
                                EmptyWorkingSet(process.Handle);
                                processesOptimized++;

                                if (processesOptimized % 5 == 0)
                                {
                                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ProcessesOptimized, processesOptimized));
                                }
                            }
                        }
                        catch { }
                    }

                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.TotalProcessesOptimized, processesOptimized));
                    await Task.Delay(1000);

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ClearingFileSystemCache);
                    await RunCommandAsync("powershell -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"");
                    await Task.Delay(1000);

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ClearingTempFiles);
                    string[] tempPaths = {
                        Path.GetTempPath(),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp")
                    };

                    foreach (string tempPath in tempPaths)
                    {
                        try
                        {
                            if (Directory.Exists(tempPath))
                            {
                                foreach (string file in Directory.GetFiles(tempPath))
                                {
                                    try
                                    {
                                        var fileInfo = new FileInfo(file);
                                        if ((DateTime.Now - fileInfo.LastAccessTime).TotalDays > 1)
                                        {
                                            File.Delete(file);
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        catch { }
                    }
                    await Task.Delay(1000);

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.RunningGarbageCollection);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(1000);

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ClearingDNSCacheMemory);
                    await RunCommandAsync("ipconfig /flushdns");
                    await Task.Delay(1000);

                    var finalMemoryInfo = await GetMemoryInfo();
                    double memorySaved = initialMemoryInfo.UsedMemory - finalMemoryInfo.UsedMemory;

                    AppendProgress(progressTextBox, ResourceManagerHelper.Instance.OptimizationResults);
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryUsageAfter, finalMemoryInfo.UsedMemory.ToString("N2")));
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryAvailableAfter, finalMemoryInfo.AvailableMemory.ToString("N2")));
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryFreed, memorySaved.ToString("N2")));
                });

                MessageBox.Show(ResourceManagerHelper.Instance.MemoryOptimizationSuccess,
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorOptimizingMemory, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorOptimizingMemory, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        public class DnsResult
        {
            public string Name { get; set; }
            public string Primary { get; set; }
            public string Secondary { get; set; }
            public double Latency { get; set; }
        }

        private async void DNSBenchmark(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.DNSBenchmarkTitle);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingDNSTest);

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
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.TestingDNS, dns.Name));
                    double latency = await TestDNSLatency(dns.Primary);
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.LatencyResult, latency.ToString("F2")));
                    return new DnsResult
                    {
                        Name = dns.Name,
                        Primary = dns.Primary,
                        Secondary = dns.Secondary,
                        Latency = latency
                    };
                });

                results = (await Task.WhenAll(tasks)).OrderBy(r => r.Latency).ToList();

                var resultsWindow = new Window
                {
                    Title = ResourceManagerHelper.Instance.DNSBenchmarkResultsTitle,
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var dataGrid = new DataGrid
                {
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                    GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                    HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                    VerticalGridLinesBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                    AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(63, 65, 70)),
                    RowBackground = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    ItemsSource = results,
                    CanUserSortColumns = true,
                    CanUserResizeColumns = true,
                    HeadersVisibility = DataGridHeadersVisibility.Column,
                    RowHeaderWidth = 0,
                    SelectionMode = DataGridSelectionMode.Single,
                    SelectionUnit = DataGridSelectionUnit.FullRow,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                };

                var headerStyle = new Style(typeof(DataGridColumnHeader));
                headerStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(63, 65, 70))));
                headerStyle.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
                headerStyle.Setters.Add(new Setter(TextElement.FontWeightProperty, FontWeights.Bold));
                headerStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
                dataGrid.ColumnHeaderStyle = headerStyle;

                var cellStyle = new Style(typeof(DataGridCell));
                cellStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(53, 55, 60))));
                cellStyle.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
                cellStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
                dataGrid.CellStyle = cellStyle;

                var rowStyle = new Style(typeof(DataGridRow));
                rowStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(53, 55, 60))));
                var trigger = new Trigger { Property = DataGridRow.IsSelectedProperty, Value = true };
                trigger.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Color.FromRgb(73, 75, 80))));
                rowStyle.Triggers.Add(trigger);
                dataGrid.RowStyle = rowStyle;

                var nameColumn = new DataGridTextColumn
                {
                    Header = ResourceManagerHelper.Instance.DNSNameColumn,
                    Binding = new Binding("Name"),
                    Width = 150
                };
                dataGrid.Columns.Add(nameColumn);

                var primaryColumn = new DataGridTextColumn
                {
                    Header = ResourceManagerHelper.Instance.DNSPrimaryColumn,
                    Binding = new Binding("Primary"),
                    Width = 150
                };
                dataGrid.Columns.Add(primaryColumn);

                var secondaryColumn = new DataGridTextColumn
                {
                    Header = ResourceManagerHelper.Instance.DNSSecondaryColumn,
                    Binding = new Binding("Secondary"),
                    Width = 150
                };
                dataGrid.Columns.Add(secondaryColumn);

                var latencyColumn = new DataGridTextColumn
                {
                    Header = ResourceManagerHelper.Instance.DNSLatencyColumn,
                    Binding = new Binding("Latency") { StringFormat = "F2" },
                    Width = 100
                };
                dataGrid.Columns.Add(latencyColumn);

                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = results;

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };

                var configureButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.ConfigureSelectedDNSButton,
                    Style = (Style)Application.Current.Resources["ButtonStyle"],
                    Width = 200,
                    Margin = new Thickness(5)
                };

                configureButton.Click += async (s, e) =>
                {
                    var selectedItem = dataGrid.SelectedItem as DnsResult;
                    if (selectedItem != null)
                    {
                        await ConfigureDNS(selectedItem.Primary, selectedItem.Secondary);
                    }
                };

                buttonPanel.Children.Add(configureButton);
                Grid.SetRow(buttonPanel, 0);
                Grid.SetRow(dataGrid, 1);

                grid.Children.Add(buttonPanel);
                grid.Children.Add(dataGrid);

                resultsWindow.Content = grid;
                resultsWindow.Show();
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorRunningDNSBenchmark, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorRunningDNSBenchmark, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                button.IsEnabled = true;
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private async Task<double> TestDNSLatency(string dnsServer)
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

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern int GetAdaptersInfo(IntPtr pAdapterInfo, ref int pOutBufLen);

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern int SetInterfaceDnsSettings(string adapterName, string primaryDNS, string secondaryDNS);

        private async Task ConfigureDNS(string primaryDNS, string secondaryDNS)
        {
            try
            {
                string command = "netsh interface show interface";
                string output = await RunCommandAsync(command);

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

                command = $"netsh interface ip set address name=\"{adapterName}\" dhcp";
                await RunCommandAsync(command);

                command = $"netsh interface ip set dns name=\"{adapterName}\" dhcp";
                await RunCommandAsync(command);

                await Task.Delay(2000);

                command = $"netsh interface ip set dns name=\"{adapterName}\" static {primaryDNS}";
                await RunCommandAsync(command);

                await Task.Delay(2000);

                command = $"netsh interface ip add dns name=\"{adapterName}\" {secondaryDNS} index=2";
                await RunCommandAsync(command);

                await Task.Delay(2000);

                command = "ipconfig /flushdns";
                await RunCommandAsync(command);

                command = $"netsh interface ip show dns name=\"{adapterName}\"";
                output = await RunCommandAsync(command);

                command = "ipconfig /all";
                output = await RunCommandAsync(command);

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

                    command = "ipconfig /flushdns";
                    await RunCommandAsync(command);
                }

                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.DNSConfiguredSuccess, adapterName, primaryDNS, secondaryDNS),
                ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format(ResourceManagerHelper.Instance.ErrorConfiguringDNS, ex.Message);
                MessageBox.Show(errorMessage, ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        private async void OptimizeGameRoute(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogWindow = new Window
                {
                    Title = ResourceManagerHelper.Instance.GameServerIPTitle,
                    Width = 400,
                    Height = 180,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    WindowStyle = WindowStyle.ToolWindow,
                    ResizeMode = ResizeMode.NoResize
                };

                var dialogGrid = new Grid
                {
                    Margin = new Thickness(15)
                };
                dialogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                dialogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                dialogGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var label = new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.EnterGameServerIP,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 10),
                    FontSize = 14
                };

                var textBox = new TextBox
                {
                    Height = 30,
                    FontSize = 14,
                    Background = new SolidColorBrush(Color.FromRgb(45, 47, 52)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    Margin = new Thickness(0, 0, 0, 15)
                };

                var dialogButtonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(0, 5, 0, 0)
                };

                var okButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.OKButton,
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };

                var cancelButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.CancelButton,
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100))
                };

                dialogButtonPanel.Children.Add(okButton);
                dialogButtonPanel.Children.Add(cancelButton);

                Grid.SetRow(label, 0);
                Grid.SetRow(textBox, 1);
                Grid.SetRow(dialogButtonPanel, 2);

                dialogGrid.Children.Add(label);
                dialogGrid.Children.Add(textBox);
                dialogGrid.Children.Add(dialogButtonPanel);

                dialogWindow.Content = dialogGrid;

                bool? result = null;
                string gameServerIP = "";

                okButton.Click += (s, args) =>
                {
                    gameServerIP = textBox.Text.Trim();
                    if (string.IsNullOrEmpty(gameServerIP))
                    {
                        MessageBox.Show(ResourceManagerHelper.Instance.InvalidIPWarning,
                        ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    result = true;
                    dialogWindow.Close();
                };

                cancelButton.Click += (s, args) =>
                {
                    result = false;
                    dialogWindow.Close();
                };

                dialogWindow.ShowDialog();

                if (result != true)
                {
                    return;
                }

                var (progressWindow, progressTextBox) = CreateProgressWindow(ResourceManagerHelper.Instance.OptimizingGameRouteTitle);
                progressWindow.Show();

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
                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.StartingGameDNSOptimization);

                var semaphore = new SemaphoreSlim(5);
                var tasks = dnsList.Select(async dns =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.TestingDNS, dns.Name));
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
                            AppendProgress(progressTextBox, $"Concluído {dns.Name}: {latency:F2} ms");
                        }
                        else
                        {
                            AppendProgress(progressTextBox, $"{dns.Name} falhou ou atingiu timeout");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Erro ao testar {dns.Name}: {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
                progressWindow.Close();

                var resultsWindow = new Window
                {
                    Title = ResourceManagerHelper.Instance.DNSResultsTitle,
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60))
                };

                var resultsGrid = new Grid();
                resultsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                resultsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                resultsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var resultsLabel = new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.DNSResultsLabel,
                    Foreground = Brushes.White,
                    Margin = new Thickness(10),
                    FontSize = 14
                };

                var dataGrid = new DataGrid
                {
                    AutoGenerateColumns = false,
                    IsReadOnly = true,
                    Background = new SolidColorBrush(Color.FromRgb(45, 47, 52)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    GridLinesVisibility = DataGridGridLinesVisibility.All,
                    HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    VerticalGridLinesBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    RowBackground = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(45, 47, 52))
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
                    Binding = new Binding("Latency"),
                    Width = 100
                });

                var resultsButtonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };

                var configureButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.ConfigureSelectedDNSButton,
                    Width = 200,
                    Height = 30,
                    Margin = new Thickness(5),
                    Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100))
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

                configureButton.Click += async (s, args) =>
                {
                    var selectedItem = dataGrid.SelectedItem as DnsResult;
                    if (selectedItem == null)
                    {
                        MessageBox.Show(ResourceManagerHelper.Instance.SelectDNSWarning,
                        ResourceManagerHelper.Instance.WarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await ConfigureDNS(selectedItem.Primary, selectedItem.Secondary);
                };

                resultsWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorOptimizingGameRoute, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OptimizeAdvancedNetworkSettings(object sender, RoutedEventArgs e)
        {
            var window = new Window
            {
                Title = ResourceManagerHelper.Instance.OptimizeAdvancedNetworkSettings,
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142)),
                BorderThickness = new Thickness(1)
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            stackPanel.Children.Add(new TextBlock
            {
                Text = ResourceManagerHelper.Instance.AdvancedNetworkAdvertise,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var applyButton = new Button
            {
                Content = ResourceManagerHelper.Instance.Apply,
                Width = 100,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
            };

            var revertButton = new Button
            {
                Content = ResourceManagerHelper.Instance.Revert,
                Width = 100,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
            };

            var cancelButton = new Button
            {
                Content = ResourceManagerHelper.Instance.Cancel,
                Width = 100,
                Background = new SolidColorBrush(Color.FromRgb(53, 55, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(128, 132, 142))
            };

            buttonPanel.Children.Add(applyButton);
            buttonPanel.Children.Add(revertButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);
            window.Content = stackPanel;

            applyButton.Click += async (s, ev) => await ApplyNetworkOptimizations(window);
            revertButton.Click += async (s, ev) => await RevertNetworkOptimizations(window);
            cancelButton.Click += (s, ev) => window.Close();

            window.ShowDialog();
        }

        private async Task ApplyNetworkOptimizations(Window window)
        {
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow("Otimizando Configurações de Rede Avançadas");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, "Iniciando otimização avançada da rede...");

                await Task.Run(async () =>
                {
                    AppendProgress(progressTextBox, "Desativando reserva de banda QoS...");
                    ExecuteCommandWithOutput("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched\" /v NonBestEffortLimit /t REG_DWORD /d 0 /f", progressTextBox);
                    AppendProgress(progressTextBox, "QoS ajustado com sucesso.");
                    AppendProgress(progressTextBox, "Otimizando parâmetros TCP/IP...");
                    var tcpSettings = new Dictionary<string, (string Value, RegistryValueKind Kind)>
            {
                {"TcpAckFrequency", ("1", RegistryValueKind.String)},
                {"TCPNoDelay", ("1", RegistryValueKind.DWord)},
                {"MaxConnectionsPerServer", ("16", RegistryValueKind.DWord)},
                {"DefaultTTL", ("64", RegistryValueKind.DWord)},
                {"TcpMaxDataRetransmissions", ("3", RegistryValueKind.DWord)}
            };

                    string tcpKeyPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
                    foreach (var setting in tcpSettings)
                    {
                        SetRegistryValue(tcpKeyPath, setting.Key, setting.Value.Value, setting.Value.Kind);
                    }
                    AppendProgress(progressTextBox, "Parâmetros TCP/IP otimizados.");
                    AppendProgress(progressTextBox, "Ajustando MTU para 1500...");
                    string adapterName = await GetActiveNetworkAdapterNameAsync();
                    if (!string.IsNullOrEmpty(adapterName))
                    {
                        ExecuteCommandWithOutput($"netsh interface ipv4 set subinterface \"{adapterName}\" mtu=1500 store=persistent", progressTextBox);
                        AppendProgress(progressTextBox, $"MTU ajustado para {adapterName}.");
                    }
                    else
                    {
                        AppendProgress(progressTextBox, ResourceManagerHelper.Instance.NetworkAdapterNotFound);
                    }

                    AppendProgress(progressTextBox, $"{ResourceManagerHelper.Instance.Disabling} Energy-Efficient Ethernet...");
                    ExecuteCommandWithOutput("powershell -Command \"Set-NetAdapterAdvancedProperty -Name * -DisplayName 'Energy-Efficient Ethernet' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue\"", progressTextBox);
                    AppendProgress(progressTextBox, "EEE disabled.");
                    AppendProgress(progressTextBox, "Cleaning cache ARP...");
                    ExecuteCommandWithOutput("netsh interface ip delete arpcache", progressTextBox);
                    AppendProgress(progressTextBox, "Cache ARP cleaned.");
                    AppendProgress(progressTextBox, $"{ResourceManagerHelper.Instance.Enabling} Receive Side Scaling (RSS)...");
                    ExecuteCommandWithOutput("netsh int tcp set global rss=enabled", progressTextBox);
                    ExecuteCommandWithOutput("powershell -Command \"Set-NetAdapterRss -Name * -NumberOfReceiveQueues 4 -ErrorAction SilentlyContinue\"", progressTextBox);
                    AppendProgress(progressTextBox, "RSS configured.");

                    await Task.Delay(500);
                });

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.AdvancedNetworkSuccess);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.AdvancedNetworkApplied,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro durante a otimização da rede: {ex.Message}");
                MessageBox.Show($"Erro ao otimizar configurações de rede: {ex.Message}",
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
                window.Close();
            }
        }

        private async Task RevertNetworkOptimizations(Window window)
        {
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow("Revertendo Configurações de Rede Avançadas");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, "Iniciando reversão das configurações de rede...");

                await Task.Run(async () =>
                {
                    AppendProgress(progressTextBox, "Restaurando configuração QoS padrão...");
                    ExecuteCommandWithOutput("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched\" /v NonBestEffortLimit /f", progressTextBox);
                    AppendProgress(progressTextBox, "QoS restaurado.");
                    AppendProgress(progressTextBox, "Restaurando parâmetros TCP/IP padrão...");
                    var tcpSettings = new[] { "TcpAckFrequency", "TCPNoDelay", "MaxConnectionsPerServer", "DefaultTTL", "TcpMaxDataRetransmissions" };
                    string tcpKeyPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
                    foreach (var setting in tcpSettings)
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(tcpKeyPath, true))
                        {
                            key?.DeleteValue(setting, false);
                        }
                    }
                    AppendProgress(progressTextBox, "Parâmetros TCP/IP restaurados.");
                    AppendProgress(progressTextBox, "Restaurando MTU para automático...");
                    string adapterName = await GetActiveNetworkAdapterNameAsync();
                    if (!string.IsNullOrEmpty(adapterName))
                    {
                        ExecuteCommandWithOutput($"netsh interface ipv4 set subinterface \"{adapterName}\" mtu=store=persistent", progressTextBox);
                        AppendProgress(progressTextBox, $"MTU restaurado para {adapterName}.");
                    }
                    else
                    {
                        AppendProgress(progressTextBox, "Adaptador de rede não encontrado.");
                    }
                    AppendProgress(progressTextBox, "Reativando Energy-Efficient Ethernet...");
                    ExecuteCommandWithOutput("powershell -Command \"Set-NetAdapterAdvancedProperty -Name * -DisplayName 'Energy-Efficient Ethernet' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue\"", progressTextBox);
                    AppendProgress(progressTextBox, "EEE reativado.");
                    AppendProgress(progressTextBox, "Limpando cache ARP...");
                    ExecuteCommandWithOutput("netsh interface ip delete arpcache", progressTextBox);
                    AppendProgress(progressTextBox, "Cache ARP limpo.");
                    AppendProgress(progressTextBox, "Restaurando Receive Side Scaling (RSS) para padrão...");
                    ExecuteCommandWithOutput("netsh int tcp set global rss=default", progressTextBox);
                    ExecuteCommandWithOutput("powershell -Command \"Set-NetAdapterRss -Name * -NumberOfReceiveQueues 2 -ErrorAction SilentlyContinue\"", progressTextBox);
                    AppendProgress(progressTextBox, "RSS restaurado.");

                    await Task.Delay(500);
                });

                AppendProgress(progressTextBox, "Reversão das configurações de rede concluída com sucesso!");
                await Task.Run(() => MessageBox.Show(
                    "Configurações de rede restauradas para os padrões do Windows!\nReinicie o sistema para aplicar todas as alterações.",
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro durante a reversão da rede: {ex.Message}");
                MessageBox.Show($"Erro ao reverter configurações de rede: {ex.Message}",
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
                window.Close();
            }
        }

        private async Task<string> GetActiveNetworkAdapterNameAsync()
        {
            string output = await RunCommandAsync("netsh interface show interface");
            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                if (line.Contains("Conectado") || line.Contains("Connected"))
                {
                    string[] parts = line.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        return parts[parts.Length - 1].Trim();
                    }
                }
            }
            return null;
        }
    }
}