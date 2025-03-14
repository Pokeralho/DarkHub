using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DarkHub.UI
{
    public class AdvancedNetworkOptimizer
    {
        private Window _progressWindow;
        private TextBox _progressTextBox;
        private readonly Button _button;

        public AdvancedNetworkOptimizer(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow("Otimizando Configurações de Rede Avançadas");
            _progressWindow.Owner = owner;
        }

        public async Task OptimizeAdvancedNetworkSettingsAsync()
        {
            _button.IsEnabled = false;

            try
            {
                bool? choice = null;

                choice = _progressWindow.Dispatcher.Invoke(() => ShowOptionsDialog());

                if (choice == null)
                {
                    _button.IsEnabled = true;
                    WindowFactory.AppendProgress(_progressTextBox, "Operação cancelada pelo usuário.");
                    return;
                }

                _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show());

                if (choice == true)
                {
                    await ApplyNetworkOptimizationsAsync();
                }
                else if (choice == false)
                {
                    await RevertNetworkOptimizationsAsync();
                }
                else
                {
                    WindowFactory.AppendProgress(_progressTextBox, "Operação cancelada.");
                    _button.IsEnabled = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, $"Erro: {ex.Message}");
                MessageBox.Show($"Erro ao processar configurações de rede: {ex.Message}",
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _button.IsEnabled = true;

                if (_progressWindow.IsVisible)
                {
                    _progressWindow.Dispatcher.Invoke(() => _progressWindow.Close());
                }
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
                    title: ResourceManagerHelper.Instance.OptimizeAdvancedNetworkSettings,
                    width: 500,
                    height: 200,
                    owner: owner,
                    isModal: true,
                    resizable: false
                );

                var stackPanel = new StackPanel { Margin = new Thickness(15) };
                stackPanel.Children.Add(new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.AdvancedNetworkAdvertise,
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

        private async Task<bool?> ShowOptionsDialogAsync()
        {
            throw new NotImplementedException("Este método foi substituído por ShowOptionsDialog");
        }

        private async Task ApplyNetworkOptimizationsAsync()
        {
            WindowFactory.AppendProgress(_progressTextBox, "Iniciando otimização avançada da rede...");

            await Task.Run(async () =>
            {
                WindowFactory.AppendProgress(_progressTextBox, "Desativando reserva de banda QoS...");
                await WindowFactory.ExecuteCommandWithOutputAsync("reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched\" /v NonBestEffortLimit /t REG_DWORD /d 0 /f", _progressTextBox, true);
                WindowFactory.AppendProgress(_progressTextBox, "QoS ajustado com sucesso.");

                WindowFactory.AppendProgress(_progressTextBox, "Otimizando parâmetros TCP/IP...");
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
                WindowFactory.AppendProgress(_progressTextBox, "Parâmetros TCP/IP otimizados.");

                WindowFactory.AppendProgress(_progressTextBox, "Ajustando MTU para 1500...");
                string adapterName = await GetActiveNetworkAdapterNameAsync();
                if (!string.IsNullOrEmpty(adapterName))
                {
                    await WindowFactory.ExecuteCommandWithOutputAsync($"netsh interface ipv4 set subinterface \"{adapterName}\" mtu=1500 store=persistent", _progressTextBox, true);
                    WindowFactory.AppendProgress(_progressTextBox, $"MTU ajustado para {adapterName}.");
                }
                else
                {
                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.NetworkAdapterNotFound);
                }

                WindowFactory.AppendProgress(_progressTextBox, $"{ResourceManagerHelper.Instance.Disabling} Energy-Efficient Ethernet...");
                await WindowFactory.ExecuteCommandWithOutputAsync("powershell -Command \"Set-NetAdapterAdvancedProperty -Name * -DisplayName 'Energy-Efficient Ethernet' -DisplayValue 'Disabled' -ErrorAction SilentlyContinue\"", _progressTextBox, true);
                WindowFactory.AppendProgress(_progressTextBox, "EEE desativado.");

                WindowFactory.AppendProgress(_progressTextBox, "Limpando cache ARP...");
                await WindowFactory.ExecuteCommandWithOutputAsync("netsh interface ip delete arpcache", _progressTextBox, true);
                WindowFactory.AppendProgress(_progressTextBox, "Cache ARP limpo.");

                WindowFactory.AppendProgress(_progressTextBox, $"{ResourceManagerHelper.Instance.Enabling} Receive Side Scaling (RSS)...");
                await WindowFactory.ExecuteCommandWithOutputAsync("netsh int tcp set global rss=enabled", _progressTextBox, true);
                await WindowFactory.ExecuteCommandWithOutputAsync("powershell -Command \"Set-NetAdapterRss -Name * -NumberOfReceiveQueues 4 -ErrorAction SilentlyContinue\"", _progressTextBox, true);
                WindowFactory.AppendProgress(_progressTextBox, "RSS configurado.");

                await Task.Delay(500);
            });

            WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.AdvancedNetworkSuccess);

            var owner = _progressWindow.Owner;

            var ownerState = WindowState.Normal;
            var isOwnerVisible = false;

            await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
            {
                if (owner != null && owner.IsLoaded)
                {
                    ownerState = owner.WindowState;
                    isOwnerVisible = owner.IsVisible;
                }

                MessageBox.Show(ResourceManagerHelper.Instance.AdvancedNetworkApplied,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                if (owner != null && owner.IsLoaded)
                {
                    if (isOwnerVisible)
                    {
                        owner.WindowState = ownerState;
                        owner.Activate();
                        owner.Focus();
                    }
                }
            }));
        }

        private async Task RevertNetworkOptimizationsAsync()
        {
            WindowFactory.AppendProgress(_progressTextBox, "Iniciando reversão das configurações de rede...");

            await Task.Run(async () =>
            {
                WindowFactory.AppendProgress(_progressTextBox, "Restaurando configuração QoS padrão...");
                await WindowFactory.ExecuteCommandWithOutputAsync("reg delete \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Policies\\Microsoft\\Windows\\Psched\" /v NonBestEffortLimit /f", _progressTextBox, true);
                WindowFactory.AppendProgress(_progressTextBox, "QoS restaurado.");

                WindowFactory.AppendProgress(_progressTextBox, "Restaurando parâmetros TCP/IP padrão...");
                var tcpSettings = new[] { "TcpAckFrequency", "TCPNoDelay", "MaxConnectionsPerServer", "DefaultTTL", "TcpMaxDataRetransmissions" };
                string tcpKeyPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters";
                foreach (var setting in tcpSettings)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(tcpKeyPath, true))
                    {
                        key?.DeleteValue(setting, false);
                    }
                }
                WindowFactory.AppendProgress(_progressTextBox, "Parâmetros TCP/IP restaurados.");

                WindowFactory.AppendProgress(_progressTextBox, "Restaurando MTU para automático...");
                string adapterName = await GetActiveNetworkAdapterNameAsync();
                if (!string.IsNullOrEmpty(adapterName))
                {
                    await WindowFactory.ExecuteCommandWithOutputAsync($"netsh interface ipv4 set subinterface \"{adapterName}\" mtu=auto store=persistent", _progressTextBox, true);
                    WindowFactory.AppendProgress(_progressTextBox, $"MTU restaurado para {adapterName}.");
                }
                else
                {
                    WindowFactory.AppendProgress(_progressTextBox, "Adaptador de rede não encontrado.");
                }

                WindowFactory.AppendProgress(_progressTextBox, "Reativando Energy-Efficient Ethernet...");
                await WindowFactory.ExecuteCommandWithOutputAsync("powershell -Command \"Set-NetAdapterAdvancedProperty -Name * -DisplayName 'Energy-Efficient Ethernet' -DisplayValue 'Enabled' -ErrorAction SilentlyContinue\"", _progressTextBox, true);
                WindowFactory.AppendProgress(_progressTextBox, "EEE reativado.");

                WindowFactory.AppendProgress(_progressTextBox, "Limpando cache ARP...");
                await WindowFactory.ExecuteCommandWithOutputAsync("netsh interface ip delete arpcache", _progressTextBox, true);
                WindowFactory.AppendProgress(_progressTextBox, "Cache ARP limpo.");

                WindowFactory.AppendProgress(_progressTextBox, "Restaurando Receive Side Scaling (RSS) para padrão...");
                await WindowFactory.ExecuteCommandWithOutputAsync("netsh int tcp set global rss=default", _progressTextBox, true);
                await WindowFactory.ExecuteCommandWithOutputAsync("powershell -Command \"Set-NetAdapterRss -Name * -NumberOfReceiveQueues 2 -ErrorAction SilentlyContinue\"", _progressTextBox, true);
                WindowFactory.AppendProgress(_progressTextBox, "RSS restaurado.");

                await Task.Delay(500);
            });

            WindowFactory.AppendProgress(_progressTextBox, "Reversão das configurações de rede concluída com sucesso!");

            var owner = _progressWindow.Owner;

            var ownerState = WindowState.Normal;
            var isOwnerVisible = false;

            await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
            {
                if (owner != null && owner.IsLoaded)
                {
                    ownerState = owner.WindowState;
                    isOwnerVisible = owner.IsVisible;
                }

                MessageBox.Show(
                    "Configurações de rede restauradas para os padrões do Windows!\nReinicie o sistema para aplicar todas as alterações.",
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                if (owner != null && owner.IsLoaded)
                {
                    if (isOwnerVisible)
                    {
                        owner.WindowState = ownerState;
                        owner.Activate();
                        owner.Focus();
                    }
                }
            }));
        }

        private async Task<string> GetActiveNetworkAdapterNameAsync()
        {
            string output = await WindowFactory.ExecuteCommandWithOutputAsync("netsh interface show interface", _progressTextBox);
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

        private void SetRegistryValue(string keyPath, string name, object value, RegistryValueKind kind)
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(keyPath, true) ?? Registry.LocalMachine.CreateSubKey(keyPath))
                {
                    key.SetValue(name, value, kind);
                }
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, $"Erro ao definir valor de registro {name}: {ex.Message}");
            }
        }
    }
}