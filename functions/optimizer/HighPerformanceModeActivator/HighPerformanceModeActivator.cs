using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;

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
                await Task.Run(async () =>
                {
                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ActivatingPowerPlan);
                    string result = ExecuteCommandWithOutput("powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c", _progressTextBox);
                    if (result.Contains("sucesso") || string.IsNullOrEmpty(result))
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                    else
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.PowerPlanAlreadyActive);

                    await Task.Delay(100);
                });

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.HighPerformanceModeSuccess);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.HighPerformanceModeActivated,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
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
    }
}