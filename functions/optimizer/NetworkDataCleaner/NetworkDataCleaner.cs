using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class NetworkDataCleaner
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public NetworkDataCleaner(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.CleaningNetworkDataTitle);
            _progressWindow.Owner = owner;
        }

        public async Task CleanNetworkDataAsync()
        {
            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingNetworkDataCleanup);
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
                        WindowFactory.AppendProgress(_progressTextBox, $"Executando: {command}...\n");
                        string result = await RunCommandAsync(command);
                        WindowFactory.AppendProgress(_progressTextBox, result + "\n");
                        await Task.Delay(100);
                    }
                });

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.NetworkDataCleanupCompleted);

                // Captura o owner na thread de UI
                var owner = _progressWindow.Owner;

                // Guardar estado da janela principal
                WindowState ownerState = WindowState.Normal;
                bool isOwnerVisible = false;

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    // Capturar estado atual da janela owner
                    if (owner != null && owner.IsLoaded)
                    {
                        ownerState = owner.WindowState;
                        isOwnerVisible = owner.IsVisible;
                    }

                    MessageBox.Show(ResourceManagerHelper.Instance.NetworkDataCleanupSuccess,
                        ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                    // Restaurar estado da janela principal
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
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningNetworkData, ex.Message));

                // Captura o owner na thread de UI para mensagem de erro
                var owner = _progressWindow.Owner;
                WindowState ownerState = WindowState.Normal;
                bool isOwnerVisible = false;

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    // Capturar estado atual da janela owner
                    if (owner != null && owner.IsLoaded)
                    {
                        ownerState = owner.WindowState;
                        isOwnerVisible = owner.IsVisible;
                    }

                    MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCleaningNetworkData, ex.Message),
                        ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);

                    // Restaurar estado da janela principal
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
            finally
            {
                _button.IsEnabled = true;

                // Capturar o owner aqui também para garantir que a janela principal permaneça visível
                // ao fechar a janela de progresso
                var owner = _progressWindow.Owner;
                WindowState ownerState = WindowState.Normal;
                bool isOwnerVisible = false;

                if (owner != null && owner.IsLoaded)
                {
                    ownerState = owner.WindowState;
                    isOwnerVisible = owner.IsVisible;
                }

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    _progressWindow.Close();

                    // Restaurar estado da janela principal após fechar a janela de progresso
                    if (owner != null && owner.IsLoaded && isOwnerVisible)
                    {
                        owner.WindowState = ownerState;
                        owner.Activate();
                        owner.Focus();
                    }
                }));
            }
        }

        private async Task<string> RunCommandAsync(string command)
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

                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(output))
                    WindowFactory.AppendProgress(_progressTextBox, output);
                if (!string.IsNullOrEmpty(error))
                    WindowFactory.AppendProgress(_progressTextBox, $"Erro: {error}");

                return output + error;
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, $"Erro ao executar comando: {ex.Message}");
                return string.Empty;
            }
        }
    }

    public static class ProcessExtensions
    {
        public static Task WaitForExitAsync(this Process process)
        {
            var tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) => tcs.TrySetResult(true);
            if (process.HasExited) tcs.TrySetResult(true);
            return tcs.Task;
        }
    }
}