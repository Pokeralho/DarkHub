using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class ProcessPriorityChanger
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public ProcessPriorityChanger(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.ChangingProcessPriorityTitle);
            _progressWindow.Owner = owner;
        }

        public async Task ChangePriorityAsync()
        {
            _button.IsEnabled = false;

            try
            {
                var fileDialog = new OpenFileDialog
                {
                    Filter = "Executáveis (*.exe)|*.exe",
                    Title = ResourceManagerHelper.Instance.SelectExecutableForPriority
                };

                if (fileDialog.ShowDialog() != true)
                {
                    _button.IsEnabled = true;
                    return;
                }

                string filePath = fileDialog.FileName;
                string processName = Path.GetFileName(filePath);

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.StartingPriorityChange, processName));

                await Task.Run(async () =>
                {
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.CheckingProcessRunning, processName));
                    string checkResult = ExecuteCommandWithOutput($"wmic process where name='{processName}' get processid", _progressTextBox);
                    if (string.IsNullOrEmpty(checkResult) || !checkResult.Contains("ProcessId"))
                    {
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ProcessNotFound);
                        return;
                    }

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ChangingPriorityToHigh);
                    string result = ExecuteCommandWithOutput($"wmic process where name='{processName}' CALL setpriority 256", _progressTextBox);
                    if (result.Contains("ReturnValue = 0"))
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                    else
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.PriorityChangeFailed);

                    await Task.Delay(100);
                });

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.PriorityChangeCompleted);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.PriorityChangedToHigh,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorChangingPriority, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorChangingPriority, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _button.IsEnabled = true;
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Close()));
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