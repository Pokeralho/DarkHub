using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Security.Cryptography.X509Certificates;

namespace DarkHub
{
    public partial class Optimizer : Page
    {
        public Optimizer()
        {
            try
            {
                InitializeComponent();
                Debug.WriteLine("Optimizer inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar Optimizer: {ex.Message}\nStackTrace: {ex.StackTrace}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao inicializar Optimizer: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void SystemInfo(object sender, RoutedEventArgs e)
        {
            try
            {
                var systemInfo = new StringBuilder();
                Debug.WriteLine("Coletando informações do sistema...");

                // OS Information
                var os = Environment.OSVersion;
                systemInfo.AppendLine($"SO: {os.VersionString}");

                // CPU Information
                var processorQuery = new ManagementObjectSearcher("SELECT Name, L2CacheSize, L3CacheSize FROM Win32_Processor");
                var processor = processorQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                systemInfo.AppendLine($"CPU: {processor?["Name"]?.ToString() ?? "Não encontrado"}");
                systemInfo.AppendLine($"Cache L2: {processor?["L2CacheSize"]?.ToString() ?? "Não encontrado"}");
                systemInfo.AppendLine($"Cache L3: {processor?["L3CacheSize"]?.ToString() ?? "Não encontrado"}");

                // RAM Information
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

                // GPU Information
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

                // Motherboard Information
                var motherboardQuery = new ManagementObjectSearcher("SELECT Product FROM Win32_BaseBoard");
                var motherboard = motherboardQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                systemInfo.AppendLine($"Placa Mãe: {motherboard?["Product"]?.ToString() ?? "Não encontrado"}");

                // BIOS Information
                var biosQuery = new ManagementObjectSearcher("SELECT Version FROM Win32_BIOS");
                var bios = biosQuery.Get().Cast<ManagementObject>().FirstOrDefault();
                systemInfo.AppendLine($"BIOS: {bios?["Version"]?.ToString() ?? "Não encontrado"}");

                await ShowSystemInfoWindowAsync(systemInfo.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao obter informações do sistema: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em SystemInfo: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async Task ShowSystemInfoWindowAsync(string systemInfo)
        {
            try
            {
                var infoWindow = new Window
                {
                    Title = "Informações do Sistema",
                    Width = 600,
                    Height = 500,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(Colors.White),
                    WindowStyle = WindowStyle.ToolWindow,
                    FontFamily = new FontFamily("JetBrains Mono"),
                    FontSize = 14,
                };

                infoWindow.BorderBrush = new SolidColorBrush(Color.FromRgb(115, 69, 161));
                infoWindow.BorderThickness = new Thickness(2);

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var titleBlock = new TextBlock
                {
                    Text = "Informações do Sistema",
                    FontFamily = new FontFamily("JetBrains Mono"),
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 10)
                };
                Grid.SetRow(titleBlock, 0);
                grid.Children.Add(titleBlock);

                var textBox = new TextBox
                {
                    Text = systemInfo ?? "Nenhum dado disponível",
                    IsReadOnly = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    Foreground = Brushes.Black,
                    Margin = new Thickness(10),
                    Padding = new Thickness(10),
                    FontSize = 14,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                    BorderThickness = new Thickness(1),
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
                Debug.WriteLine("Janela de informações do sistema exibida com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exibir janela de informações: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em ShowSystemInfoWindowAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void ClearTempFilesAndLogs(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                Debug.WriteLine("Sender não é um botão em ClearTempFilesAndLogs, evento ignorado.");
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow("Progresso da Limpeza");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, "Iniciando limpeza...\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar janela de progresso: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao criar janela de progresso em ClearTempFilesAndLogs: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                        ("Limpando Windows Temp...", () => ClearDirectory($"{windows}\\temp", progressTextBox)),
                        ("Limpando Prefetch (*.exe)...", () => ClearFilesByExtension($"{windows}\\Prefetch", "*.exe", progressTextBox)),
                        ("Limpando Prefetch (*.dll)...", () => ClearFilesByExtension($"{windows}\\Prefetch", "*.dll", progressTextBox)),
                        ("Limpando Prefetch (*.pf)...", () => ClearFilesByExtension($"{windows}\\Prefetch", "*.pf", progressTextBox)),
                        ("Limpando dllcache...", () => ClearDirectory($"{windows}\\system32\\dllcache", progressTextBox)),
                        ("Limpando SystemDrive Temp...", () => ClearDirectory($"{systemDrive}\\Temp", progressTextBox)),
                        ("Limpando %TEMP%...", () => ClearDirectory(temp, progressTextBox)),
                        ("Limpando History...", () => ClearDirectory(Path.Combine(userProfile, "Local Settings", "History"), progressTextBox)),
                        ("Limpando Temporary Internet Files...", () => ClearDirectory(Path.Combine(userProfile, "Local Settings", "Temporary Internet Files"), progressTextBox)),
                        ("Limpando Local Temp...", () => ClearDirectory(Path.Combine(userProfile, "Local Settings", "Temp"), progressTextBox)),
                        ("Limpando Recent...", () => ClearDirectory(Path.Combine(userProfile, "Recent"), progressTextBox)),
                        ("Limpando Cookies...", () => ClearDirectory(Path.Combine(userProfile, "Cookies"), progressTextBox)),
                        ("Limpando registros de eventos...", () => ClearEventLogsWithWevtutil(progressTextBox))
                    };

                    foreach (var task in tasks)
                    {
                        AppendProgress(progressTextBox, task.Description);
                        await Task.Run(task.Action);
                        AppendProgress(progressTextBox, "Concluído.\n");
                        await Task.Delay(100);
                    }
                });

                AppendProgress(progressTextBox, "Limpeza concluída com sucesso!");
                await Task.Run(() => MessageBox.Show("Limpeza concluída!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro geral: {ex.Message}");
                MessageBox.Show($"Erro geral ao limpar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em ClearTempFilesAndLogs: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                    Background = new SolidColorBrush(Colors.White),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(115, 69, 161)),
                    BorderThickness = new Thickness(2)
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
                    Foreground = Brushes.Black,
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
                    Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
                    Foreground = Brushes.Black,
                    Margin = new Thickness(10),
                    Padding = new Thickness(5),
                    FontFamily = new FontFamily("JetBrains Mono"),
                    FontSize = 12
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao criar janela de progresso: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void ClearDirectory(string directoryPath, TextBox? progressTextBox)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                AppendProgress(progressTextBox, "Diretório não existe ou é inválido, pulando...");
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
                        AppendProgress(progressTextBox, $"Acesso negado ao arquivo {file.FullName}: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        AppendProgress(progressTextBox, $"Arquivo em uso {file.FullName}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Erro ao deletar {file.FullName}: {ex.Message}");
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
                        AppendProgress(progressTextBox, $"Falha ao deletar subdiretório {dir.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro ao limpar {directoryPath}: {ex.Message}");
                Debug.WriteLine($"Erro em ClearDirectory {directoryPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void ClearFilesByExtension(string directoryPath, string extension, TextBox? progressTextBox)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                AppendProgress(progressTextBox, "Diretório não existe ou é inválido, pulando...");
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
                        AppendProgress(progressTextBox, $"Acesso negado ao arquivo {file.FullName}: {ex.Message}");
                    }
                    catch (IOException ex)
                    {
                        AppendProgress(progressTextBox, $"Arquivo em uso {file.FullName}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Erro ao deletar {file.FullName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro ao limpar arquivos {extension} em {directoryPath}: {ex.Message}");
                Debug.WriteLine($"Erro em ClearFilesByExtension {directoryPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                    AppendProgress(progressTextBox, $"Limpando {logName}...");
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
                            AppendProgress(progressTextBox, $"Erro: {error}");
                        else
                            AppendProgress(progressTextBox, "Concluído.");
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Erro ao limpar {logName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro geral ao limpar logs: {ex.Message}");
                Debug.WriteLine($"Erro em ClearEventLogsWithWevtutil: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao atualizar ProgressTextBox: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void DisableVisualEffects(object sender, RoutedEventArgs e)
        {
            try
            {
                const string desktopKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop";
                const string windowMetricsKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics";
                const string advancedExplorerKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                const string visualEffectsKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
                const string dwmKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM";

                byte[] optimizedUserPrefs = new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 };
                await Task.Run(() =>
                {
                    SetRegistryValue(desktopKeyPath, "UserPreferencesMask", optimizedUserPrefs, RegistryValueKind.Binary);
                    SetRegistryValue(visualEffectsKeyPath, "VisualFXSetting", 2, RegistryValueKind.DWord);
                    SetRegistryValue(windowMetricsKeyPath, "MinAnimate", "0", RegistryValueKind.String);
                    SetRegistryValue(advancedExplorerKeyPath, "TaskbarAnimations", 0, RegistryValueKind.DWord);
                    SetRegistryValue(dwmKeyPath, "EnableAeroPeek", 0, RegistryValueKind.DWord);
                    SetRegistryValue(dwmKeyPath, "AlwaysHibernateThumbnails", 0, RegistryValueKind.DWord);
                    SetRegistryValue(advancedExplorerKeyPath, "IconsOnly", 1, RegistryValueKind.DWord);
                    SetRegistryValue(advancedExplorerKeyPath, "ListviewAlphaSelect", 0, RegistryValueKind.DWord);
                    SetRegistryValue(desktopKeyPath, "DragFullWindows", "0", RegistryValueKind.String);
                    SetRegistryValue(desktopKeyPath, "FontSmoothing", "0", RegistryValueKind.String);
                    SetRegistryValue(advancedExplorerKeyPath, "ListviewShadow", 0, RegistryValueKind.DWord);
                    SetRegistryValue(dwmKeyPath, "EnableTransparency", 0, RegistryValueKind.DWord);
                    SetRegistryValue(dwmKeyPath, "ColorizationOpaqueBlend", 1, RegistryValueKind.DWord);
                    SetRegistryValue(desktopKeyPath, "DisableAnimations", 1, RegistryValueKind.DWord);
                    SetRegistryValue(dwmKeyPath, "Composition", 0, RegistryValueKind.DWord);
                    SetRegistryValue(desktopKeyPath, "MenuAnimation", "0", RegistryValueKind.String);
                    SetRegistryValue(advancedExplorerKeyPath, "ExtendedUIHoverTime", 10000, RegistryValueKind.DWord);

                    SystemParametersInfo(0x200, 0, IntPtr.Zero, 0x2);
                });

                MessageBox.Show("Efeitos visuais desabilitados para máxima otimização!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                Debug.WriteLine("Efeitos visuais desabilitados com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao desabilitar efeitos visuais: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em DisableVisualEffects: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao configurar {name} em {keyPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                Debug.WriteLine("Sender não é um botão em RepairWindows, evento ignorado.");
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow("Reparando o Windows");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, "Iniciando reparação do Windows...\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar janela de progresso: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao criar janela de progresso em RepairWindows: {ex.Message}\nStackTrace: {ex.StackTrace}");
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    var commands = new List<(string Description, string Command)>
                    {
                        ("Executando Verificador de Arquivos do Sistema (SFC)...", "sfc /scannow"),
                        ("Executando DISM para restaurar a saúde do sistema...", "dism /online /cleanup-image /restorehealth"),
                        ("Agendando verificação de disco (CHKDSK) para a próxima reinicialização...", "chkdsk /f /r")
                    };

                    foreach (var (description, command) in commands)
                    {
                        AppendProgress(progressTextBox, description);
                        string result = await Task.Run(() => ExecuteCommandWithOutput(command, progressTextBox));
                        if (command.Contains("chkdsk") && result.Contains("agendada"))
                            AppendProgress(progressTextBox, "Verificação agendada para a próxima reinicialização.\n");
                        else
                            AppendProgress(progressTextBox, "Concluído.\n");
                        await Task.Delay(100);
                    }
                });

                AppendProgress(progressTextBox, "Reparos concluídos. Reinicie o sistema para aplicar todas as alterações.");
                await Task.Run(() => MessageBox.Show("Os comandos de reparo foram executados. Reinicie o sistema para aplicar todas as alterações.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro geral: {ex.Message}");
                MessageBox.Show($"Erro geral ao reparar o Windows: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em RepairWindows: {ex.Message}\nStackTrace: {ex.StackTrace}");
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

        private string ExecuteCommandWithOutput(string command, TextBox? progressTextBox)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {command}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.GetEncoding(850),
                        StandardErrorEncoding = Encoding.GetEncoding(850)
                    }
                };

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                    AppendProgress(progressTextBox, $"Saída: {output}");
                if (!string.IsNullOrEmpty(error))
                    AppendProgress(progressTextBox, $"Erro: {error}");

                return output + error;
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro ao executar '{command}': {ex.Message}");
                Debug.WriteLine($"Erro em ExecuteCommandWithOutput: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private async void EnableHighPerformanceMode(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                Debug.WriteLine("Sender não é um botão em EnableHighPerformanceMode, evento ignorado.");
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow("Ativando Modo de Alto Desempenho");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, "Iniciando ativação do modo de alto desempenho...\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar janela de progresso: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao criar janela de progresso em EnableHighPerformanceMode: {ex.Message}\nStackTrace: {ex.StackTrace}");
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    AppendProgress(progressTextBox, "Ativando plano de energia de alto desempenho...");
                    string result = ExecuteCommandWithOutput("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", progressTextBox);
                    if (result.Contains("sucesso") || string.IsNullOrEmpty(result))
                        AppendProgress(progressTextBox, "Concluído.\n");
                    else
                        AppendProgress(progressTextBox, "Plano já ativo ou não encontrado.\n");

                    await Task.Delay(100);
                });

                AppendProgress(progressTextBox, "Modo de alto desempenho ativado com sucesso!");
                await Task.Run(() => MessageBox.Show("Modo de alto desempenho ativado!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro: {ex.Message}");
                MessageBox.Show($"Erro ao ativar modo de alto desempenho: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em EnableHighPerformanceMode: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                Debug.WriteLine("Sender não é um botão em AdjustTimerResolution, evento ignorado.");
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow("Ajustando Resolução do Temporizador");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, "Iniciando ajuste da resolução do temporizador...\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar janela de progresso: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao criar janela de progresso em AdjustTimerResolution: {ex.Message}\nStackTrace: {ex.StackTrace}");
                button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    AppendProgress(progressTextBox, "Tentando ajustar resolução do timer para 0.5ms...");
                    try
                    {
                        uint currentResolution, desiredResolution = 5000, actualResolution;
                        NtQueryTimerResolution(out currentResolution, out _, out _);
                        NtSetTimerResolution(desiredResolution, true, out actualResolution);
                        AppendProgress(progressTextBox, $"Resolução ajustada para {(actualResolution / 10000f):F2}ms.\n");
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Erro ao ajustar timer via API: {ex.Message}\n");
                        AppendProgress(progressTextBox, "Executando fallback com powercfg...");
                        ExecuteCommandWithOutput("powercfg /energy", progressTextBox);
                        AppendProgress(progressTextBox, "Fallback concluído.\n");
                    }

                    await Task.Delay(100);
                });

                AppendProgress(progressTextBox, "Ajuste da resolução do temporizador concluído!");
                await Task.Run(() => MessageBox.Show("Resolução do temporizador ajustada!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro geral: {ex.Message}");
                MessageBox.Show($"Erro ao ajustar resolução do temporizador: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em AdjustTimerResolution: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                Debug.WriteLine("Sender não é um botão em ChangePriority, evento ignorado.");
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
                    Title = "Selecionar Executável para Alterar Prioridade"
                };

                if (fileDialog.ShowDialog() != true)
                {
                    button.IsEnabled = true;
                    return;
                }

                string filePath = fileDialog.FileName;
                string processName = Path.GetFileName(filePath);

                (progressWindow, progressTextBox) = CreateProgressWindow("Alterando Prioridade do Processo");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, $"Iniciando alteração de prioridade para {processName}...\n");

                await Task.Run(async () =>
                {
                    AppendProgress(progressTextBox, $"Verificando se o processo '{processName}' está em execução...");
                    string checkResult = ExecuteCommandWithOutput($"wmic process where name='{processName}' get processid", progressTextBox);
                    if (string.IsNullOrEmpty(checkResult) || !checkResult.Contains("ProcessId"))
                    {
                        AppendProgress(progressTextBox, "Processo não encontrado.\n");
                        return;
                    }

                    AppendProgress(progressTextBox, "Alterando prioridade para 'Alta' (256)...");
                    string result = ExecuteCommandWithOutput($"wmic process where name='{processName}' CALL setpriority 256", progressTextBox);
                    if (result.Contains("ReturnValue = 0"))
                        AppendProgress(progressTextBox, "Concluído.\n");
                    else
                        AppendProgress(progressTextBox, "Falha ao alterar prioridade.\n");

                    await Task.Delay(100);
                });

                AppendProgress(progressTextBox, "Alteração de prioridade concluída!");
                await Task.Run(() => MessageBox.Show("Prioridade do processo alterada para 'Alta'!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro: {ex.Message}");
                MessageBox.Show($"Erro ao alterar prioridade: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em ChangePriority: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
            if (button == null)
            {
                Debug.WriteLine("Sender não é um botão em UninstallProgram, evento ignorado.");
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                var fileDialog = new OpenFileDialog
                {
                    Filter = "Aplicativos (*.exe)|*.exe",
                    Title = "Selecionar Programa para Desinstalar"
                };

                if (fileDialog.ShowDialog() != true)
                {
                    button.IsEnabled = true;
                    return;
                }

                string programPath = fileDialog.FileName;
                string programName = Path.GetFileNameWithoutExtension(programPath);

                if (!File.Exists(programPath))
                {
                    MessageBox.Show("O programa não foi encontrado!", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    button.IsEnabled = true;
                    return;
                }

                (progressWindow, progressTextBox) = CreateProgressWindow("Desinstalando Programa");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, $"Iniciando desinstalação de {programName}...\n");

                await Task.Run(async () =>
                {
                    AppendProgress(progressTextBox, $"Verificando se '{programName}' está registrado...");
                    string checkResult = ExecuteCommandWithOutput($"wmic product where \"Name like '%{programName}%'\" get Name", progressTextBox);
                    if (string.IsNullOrEmpty(checkResult) || !checkResult.Contains(programName, StringComparison.OrdinalIgnoreCase))
                    {
                        AppendProgress(progressTextBox, "Programa não encontrado no registro de produtos instalados.\n");
                        AppendProgress(progressTextBox, "Tentando remover o arquivo diretamente...");
                        File.Delete(programPath);
                        AppendProgress(progressTextBox, "Arquivo removido com sucesso.\n");
                        return;
                    }

                    AppendProgress(progressTextBox, "Desinstalando via WMIC...");
                    string result = ExecuteCommandWithOutput($"wmic product where \"Name like '%{programName}%'\" call uninstall", progressTextBox);
                    if (result.Contains("ReturnValue = 0"))
                        AppendProgress(progressTextBox, "Concluído.\n");
                    else
                        AppendProgress(progressTextBox, "Falha na desinstalação.\n");

                    await Task.Delay(100);
                });

                AppendProgress(progressTextBox, "Desinstalação concluída!");
                await Task.Run(() => MessageBox.Show("O programa foi desinstalado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro: {ex.Message}");
                MessageBox.Show($"Erro ao desinstalar programa: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em UninstallProgram: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                Debug.WriteLine("Comando de ativação do Windows executado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao executar o comando de ativação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em AtivarWindows: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void CleanRegistry(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                Debug.WriteLine("Sender não é um botão em CleanRegistry, evento ignorado.");
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow("Limpando Registro");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, "Iniciando limpeza do registro...\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar janela de progresso: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao criar janela de progresso em CleanRegistry: {ex.Message}\nStackTrace: {ex.StackTrace}");
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

                    AppendProgress(progressTextBox, "Escaneando registro por entradas inválidas...\n");

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
                                    AppendProgress(progressTextBox, $"Verificando {fullPath}...\n");
                                    foreach (var valueName in key.GetValueNames())
                                    {
                                        string? value = key.GetValue(valueName)?.ToString();
                                        if (!string.IsNullOrEmpty(value) && value.Contains(".exe") && !File.Exists(value))
                                        {
                                            invalidEntries.Add((fullPath, valueName, value));
                                            AppendProgress(progressTextBox, $"Entrada inválida encontrada: {valueName} -> {value}\n");
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

                    AppendProgress(progressTextBox, $"Encontradas {invalidEntries.Count} entradas inválidas:\n");
                    foreach (var entry in invalidEntries)
                    {
                        AppendProgress(progressTextBox, $" - {entry.KeyPath}\\{entry.Name}: {entry.Value}\n");
                    }

                    bool confirmed = await progressTextBox.Dispatcher.InvokeAsync(() =>
                    {
                        var result = MessageBox.Show($"Deseja remover {invalidEntries.Count} entradas inválidas?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
                                        AppendProgress(progressTextBox, $"Removido: {entry.KeyPath}\\{entry.Name}\n");
                                    }
                                    else
                                    {
                                        AppendProgress(progressTextBox, $"Erro: Não foi possível abrir {entry.KeyPath} para escrita.\n");
                                    }
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                AppendProgress(progressTextBox, $"Permissão negada ao remover {entry.KeyPath}\\{entry.Name}. Execute como administrador.\n");
                            }
                            catch (Exception ex)
                            {
                                AppendProgress(progressTextBox, $"Erro ao remover {entry.KeyPath}\\{entry.Name}: {ex.Message}\n");
                            }
                        }
                    }
                    else
                    {
                        AppendProgress(progressTextBox, "Limpeza cancelada pelo usuário.\n");
                    }

                    await Task.Delay(100);
                });

                AppendProgress(progressTextBox, "Limpeza do registro concluída!");
                await Task.Run(() => MessageBox.Show("Limpeza do registro concluída!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro geral: {ex.Message}");
                MessageBox.Show($"Erro ao limpar registro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em CleanRegistry: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                Debug.WriteLine("Sender não é um botão em ManageStartupPrograms, evento ignorado.");
                return;
            }

            button.IsEnabled = false;
            var (progressWindow, progressTextBox) = CreateProgressWindow("Gerenciando Programas de Inicialização");
            await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
            AppendProgress(progressTextBox, "Iniciando gerenciamento de inicialização...\n");

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

                    AppendProgress(progressTextBox, "Listando programas de inicialização...\n");
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
                                    AppendProgress(progressTextBox, $"Verificando {fullPath}...\n");
                                    foreach (var name in key.GetValueNames())
                                    {
                                        string? value = key.GetValue(name)?.ToString();
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            startupItems[name] = (value, fullPath, true, root);
                                            AppendProgress(progressTextBox, $"Encontrado em {fullPath}: {name} -> {value}\n");
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
                            AppendProgress(progressTextBox, $"Verificando pasta de inicialização: {folder}...\n");
                            foreach (var file in Directory.EnumerateFiles(folder, "*.lnk", SearchOption.TopDirectoryOnly))
                            {
                                string name = Path.GetFileNameWithoutExtension(file);
                                string targetPath = GetShortcutTarget(file);
                                if (!string.IsNullOrEmpty(targetPath))
                                {
                                    startupItems[name] = (targetPath, folder, false, null);
                                    AppendProgress(progressTextBox, $"Encontrado em {folder}: {name} -> {targetPath}\n");
                                }
                            }
                        }
                    }

                    if (startupItems.Count == 0)
                    {
                        AppendProgress(progressTextBox, "Nenhum programa de inicialização encontrado.\n");
                        return;
                    }

                    await progressTextBox.Dispatcher.InvokeAsync(() =>
                    {
                        var selectionWindow = new Window
                        {
                            Title = "Selecionar Programa para Desativar",
                            Width = 400,
                            Height = 300,
                            WindowStartupLocation = WindowStartupLocation.CenterScreen
                        };

                        var stackPanel = new StackPanel { Margin = new Thickness(10) };
                        var listBox = new ListBox { Height = 200 };
                        var disableButton = new Button
                        {
                            Content = "Desativar Selecionado",
                            Margin = new Thickness(0, 10, 0, 0)
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
                                                    AppendProgress(progressTextBox, $"Programa '{selectedName}' desativado no registro.\n");
                                                }
                                                else
                                                {
                                                    AppendProgress(progressTextBox, $"Erro: Não foi possível abrir {item.Source}.\n");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            string shortcutPath = Path.Combine(item.Source, $"{selectedName}.lnk");
                                            if (File.Exists(shortcutPath))
                                            {
                                                File.Delete(shortcutPath);
                                                AppendProgress(progressTextBox, $"Programa '{selectedName}' removido da pasta de inicialização.\n");
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
                                        AppendProgress(progressTextBox, $"Permissão negada ao desativar '{selectedName}'. Execute como administrador.\n");
                                    }
                                    catch (Exception ex)
                                    {
                                        AppendProgress(progressTextBox, $"Erro ao desativar '{selectedName}': {ex.Message}\n");
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

                AppendProgress(progressTextBox, "Gerenciamento concluído!\n");
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro geral: {ex.Message}\n");
            }
            finally
            {
                button.IsEnabled = true;
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
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
                Debug.WriteLine($"Erro ao obter alvo do atalho {shortcutPath}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return string.Empty;
            }
        }

        private async void CleanNetworkData(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                Debug.WriteLine("Sender não é um botão em CleanNetworkData, evento ignorado.");
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {
                (progressWindow, progressTextBox) = CreateProgressWindow("Limpando Dados de Rede");
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                AppendProgress(progressTextBox, "Iniciando limpeza de dados de rede...");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar janela de progresso: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao criar janela de progresso em CleanNetworkData: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                        AppendProgress(progressTextBox, $"Executando: {command}...");
                        string result = await RunCommandAsync(command);
                        AppendProgress(progressTextBox, result);
                        await Task.Delay(100);
                    }
                });

                AppendProgress(progressTextBox, "Limpeza de dados de rede concluída!");
                await Task.Run(() => MessageBox.Show("Limpeza de dados de rede concluída com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro geral: {ex.Message}");
                MessageBox.Show($"Erro ao limpar dados de rede: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em CleanNetworkData: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                ProcessStartInfo psi = new ProcessStartInfo
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

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await Task.Run(() => process.WaitForExit());

                    if (!string.IsNullOrEmpty(error))
                        return $"Erro: {error}";
                    return output.Trim();
                }
            }
            catch (Exception ex)
            {
                return $"Exceção ao executar comando: {ex.Message}";
            }
        }

        private async void RunSpaceSniffer(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                Debug.WriteLine("Sender não é um botão em RunAssetExecutable, evento ignorado.");
                return;
            }

            button.IsEnabled = false;
            TextBox? progressTextBox = null;
            Window? progressWindow = null;

            try
            {

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao criar janela de progresso: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao criar janela de progresso em RunAssetExecutable: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                        AppendProgress(progressTextBox, $"Pasta 'assets' não encontrada em {AppDomain.CurrentDomain.BaseDirectory}.\n");
                        throw new DirectoryNotFoundException("Pasta 'assets' não encontrada.");
                    }

                    if (!File.Exists(executablePath))
                    {
                        AppendProgress(progressTextBox, $"Executável 'SpaceSniffer.exe' não encontrado em {assetsFolder}.\n");
                        throw new FileNotFoundException($"Executável 'SpaceSniffer.exe' não encontrado em {assetsFolder}.");
                    }

                    AppendProgress(progressTextBox, $"Executando: {executablePath}...\n");

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
                            AppendProgress(progressTextBox, "Falha ao iniciar o processo.\n");
                            return;
                        }

                        await Task.Run(() => process.WaitForExit());
                        int exitCode = process.ExitCode;
                        AppendProgress(progressTextBox, $"Processo concluído com código de saída: {exitCode}.\n");
                    }
                });

            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro ao executar o programa: {ex.Message}");
                MessageBox.Show($"Erro ao executar o SpaceSniffer: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em RunAssetExecutable: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                MessageBox.Show("Erro ao tentar abrir o MRT: " + ex.Message);
            }
        }
    }
}