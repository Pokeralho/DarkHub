using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DarkHub.UI
{
    public class HighPerformanceModeActivator
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public HighPerformanceModeActivator(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.HighPerformanceModeTitle);
            _progressWindow.Owner = owner;
        }

        public async Task ActivateHighPerformanceModeAsync()
        {
            var choice = ShowOptionsDialog();
            if (choice == null)
            {
                return;
            }

            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingHighPerformanceMode);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                _button.IsEnabled = true;
                return;
            }

            try
            {
                if (choice == true)
                {
                    await ApplyOptimizationsAsync();
                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.HighPerformanceModeSuccess);
                    await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.HighPerformanceModeActivated,
                        ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
                }
                else
                {
                    await RevertOptimizationsAsync();
                    WindowFactory.AppendProgress(_progressTextBox, "Modo de alto desempenho revertido com sucesso.");
                    await Task.Run(() => MessageBox.Show("Modo de alto desempenho revertido com sucesso.",
                        ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
                }
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorActivatingHighPerformanceMode, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorActivatingHighPerformanceMode, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _button.IsEnabled = true;
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Close()));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var mainWindow = Application.Current.MainWindow;
                    if (mainWindow != null)
                    {
                        mainWindow.Activate();
                        mainWindow.Topmost = true;
                        mainWindow.Topmost = false;
                    }
                });
            }
        }

        private bool? ShowOptionsDialog()
        {
            var owner = _progressWindow.Owner;
            var ownerState = owner?.WindowState ?? WindowState.Normal;
            var isOwnerVisible = owner?.IsVisible ?? false;

            Window window = null;
            bool? result = null;

            try
            {
                window = WindowFactory.CreateWindow(
                    title: ResourceManagerHelper.Instance.HighPerformanceModeTitle,
                    width: 500,
                    height: 200,
                    owner: owner,
                    isModal: true,
                    resizable: false
                );

                var stackPanel = new StackPanel { Margin = new Thickness(15) };
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "Deseja aplicar ou reverter as otimizações de alto desempenho?\nIsso modificará configurações do sistema.",
                    Foreground = WindowFactory.DefaultTextForeground,
                    Margin = new Thickness(0, 0, 0, 15),
                    TextWrapping = TextWrapping.Wrap
                });

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 15, 0, 0)
                };

                var applyButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.Apply,
                    Width = 120,
                    Height = 40,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = WindowFactory.AccentColor,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                applyButton.Style = WindowFactory.CreateButtonStyle();

                var revertButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.Revert,
                    Width = 120,
                    Height = 40,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = WindowFactory.AccentColor,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                revertButton.Style = WindowFactory.CreateButtonStyle();

                var cancelButton = new Button
                {
                    Content = ResourceManagerHelper.Instance.Cancel,
                    Width = 120,
                    Height = 40,
                    Background = WindowFactory.AccentColor,
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };
                cancelButton.Style = WindowFactory.CreateButtonStyle();

                applyButton.Click += (s, ev) =>
                {
                    result = true;
                    window.Close();
                };

                revertButton.Click += (s, ev) =>
                {
                    result = false;
                    window.Close();
                };

                cancelButton.Click += (s, ev) =>
                {
                    result = null;
                    window.Close();
                };

                buttonPanel.Children.Add(applyButton);
                buttonPanel.Children.Add(revertButton);
                buttonPanel.Children.Add(cancelButton);
                stackPanel.Children.Add(buttonPanel);
                window.Content = stackPanel;

                window.Closing += (s, args) =>
                {
                    if (result == null)
                    {
                        result = null;
                    }
                };

                window.ShowDialog();

                if (owner != null && owner.IsLoaded && isOwnerVisible)
                {
                    owner.WindowState = ownerState;
                    owner.Activate();
                    owner.Focus();
                }

                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao mostrar diálogo: {ex.Message}",
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async Task ApplyOptimizationsAsync()
        {
            await Task.Run(async () =>
            {
                WindowFactory.AppendProgress(_progressTextBox, "Verificando privilégios administrativos...");
                if (!IsAdmin()) throw new Exception("Este programa requer privilégios administrativos.");

                WindowFactory.AppendProgress(_progressTextBox, "Desativando HAGS...");
                ExecuteCommandWithOutput("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v \"HwSchMode\" /t REG_DWORD /d 1 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Ativando Ultimate Performance Mode...");
                string checkUltimate = ExecuteCommandWithOutput("powercfg -list", _progressTextBox);
                if (!checkUltimate.Contains("Ultimate Performance"))
                {
                    ExecuteCommandWithOutput("powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61", _progressTextBox);
                }
                string ultimateGuid = ExtractGuid(checkUltimate, "Ultimate Performance");
                if (!string.IsNullOrEmpty(ultimateGuid))
                {
                    ExecuteCommandWithOutput($"powercfg -setactive {ultimateGuid}", _progressTextBox);
                }

                WindowFactory.AppendProgress(_progressTextBox, "Desativando Core Isolation...");
                ExecuteCommandWithOutput("reg add \"HKLM\\SOFTWARE\\CurrentControlSet\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v \"Enabled\" /t REG_DWORD /d 0 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Desativando Storage Sense...");
                ExecuteCommandWithOutput("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy\" /v \"01\" /t REG_DWORD /d 0 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Desativando Hibernação...");
                ExecuteCommandWithOutput("powercfg.exe /hibernate off", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Desativando Fullscreen Optimizations...");
                ExecuteCommandWithOutput("reg add \"HKCU\\System\\GameConfigStore\" /v \"GameDVR_DXGIHonorFSEWindowsCompatible\" /t REG_DWORD /d 1 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Desativando Windows Telemetry...");
                ExecuteCommandWithOutput("schtasks /change /TN \"\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator\" /DISABLE", _progressTextBox);
                ExecuteCommandWithOutput("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v \"AllowTelemetry\" /t REG_DWORD /d 0 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Bloqueando Adobe Telemetry via hosts...");
                ExecuteCommandWithOutput("curl -s -o \"%temp%\\list.txt\" \"https://a.dove.isdumb.one/list.txt\" && type \"%temp%\\list.txt\" >> \"%windir%\\System32\\drivers\\etc\\hosts\" && del \"%temp%\\list.txt\"", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Desativando NVIDIA Telemetry...");
                ExecuteCommandWithOutput("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\nvlddmkm\\Global\\Startup\" /v \"SendTelemetryData\" /t REG_DWORD /d 0 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Reiniciando Explorer...");
                ExecuteCommandWithOutput("taskkill /f /im explorer.exe & start explorer", _progressTextBox);

                await Task.Delay(100);
            });
        }

        private async Task RevertOptimizationsAsync()
        {
            await Task.Run(async () =>
            {
                WindowFactory.AppendProgress(_progressTextBox, "Verificando privilégios administrativos...");
                if (!IsAdmin()) throw new Exception("Este programa requer privilégios administrativos.");

                WindowFactory.AppendProgress(_progressTextBox, "Reativando HAGS...");
                ExecuteCommandWithOutput("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Control\\GraphicsDrivers\" /v \"HwSchMode\" /t REG_DWORD /d 2 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Revertendo para plano de energia padrão...");
                ExecuteCommandWithOutput("powercfg -setactive 381b4222-f694-41f0-9685-ff5bb260df2e", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Reativando Core Isolation...");
                ExecuteCommandWithOutput("reg add \"HKLM\\SOFTWARE\\CurrentControlSet\\CurrentControlSet\\Control\\DeviceGuard\\Scenarios\\HypervisorEnforcedCodeIntegrity\" /v \"Enabled\" /t REG_DWORD /d 1 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Reativando Storage Sense...");
                ExecuteCommandWithOutput("reg add \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\StorageSense\\Parameters\\StoragePolicy\" /v \"01\" /t REG_DWORD /d 1 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Reativando Hibernação...");
                ExecuteCommandWithOutput("powercfg.exe /hibernate on", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Reativando Fullscreen Optimizations...");
                ExecuteCommandWithOutput("reg add \"HKCU\\System\\GameConfigStore\" /v \"GameDVR_DXGIHonorFSEWindowsCompatible\" /t REG_DWORD /d 0 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Reativando Windows Telemetry...");
                ExecuteCommandWithOutput("schtasks /change /TN \"\\Microsoft\\Windows\\Customer Experience Improvement Program\\Consolidator\" /ENABLE", _progressTextBox);
                ExecuteCommandWithOutput("reg delete \"HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\" /v \"AllowTelemetry\" /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Removendo bloqueios do Adobe do arquivo hosts...");
                ExecuteCommandWithOutput("powershell -command \"(Get-Content $env:windir\\System32\\drivers\\etc\\hosts) -notmatch 'dove.isdumb.one' | Set-Content $env:windir\\System32\\drivers\\etc\\hosts\"", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Reativando NVIDIA Telemetry...");
                ExecuteCommandWithOutput("reg add \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\nvlddmkm\\Global\\Startup\" /v \"SendTelemetryData\" /t REG_DWORD /d 1 /f", _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, "Reiniciando Explorer...");
                ExecuteCommandWithOutput("taskkill /f /im explorer.exe & start explorer", _progressTextBox);

                await Task.Delay(100);
            });
        }

        private static string ExecuteCommandWithOutput(string command, TextBox progressTextBox)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
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
                    WindowFactory.AppendProgress(progressTextBox, output);
                if (!string.IsNullOrEmpty(error))
                    WindowFactory.AppendProgress(progressTextBox, $"Erro: {error}");

                return output + error;
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(progressTextBox, $"Erro ao executar comando: {ex.Message}");
                return string.Empty;
            }
        }

        private static bool IsAdmin()
        {
            try
            {
                using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        private static string ExtractGuid(string powercfgOutput, string planName)
        {
            var lines = powercfgOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains(planName))
                {
                    var parts = line.Split(' ');
                    foreach (var part in parts)
                    {
                        if (Guid.TryParse(part, out _))
                            return part;
                    }
                }
            }
            return null;
        }
    }
}