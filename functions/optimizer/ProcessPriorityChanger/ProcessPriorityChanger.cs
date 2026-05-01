using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class ProcessPriorityChanger
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public ProcessPriorityChanger(Window? owner, Button button)
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
                    string processNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                    var processes = Process.GetProcessesByName(processNameWithoutExtension);
                    if (processes.Length == 0)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ProcessNotFound);
                        return;
                    }

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ChangingPriorityToHigh);
                    int changedCount = 0;

                    foreach (var process in processes)
                    {
                        try
                        {
                            using (process)
                            {
                                process.PriorityClass = ProcessPriorityClass.High;
                                changedCount++;
                                WindowFactory.AppendProgress(_progressTextBox, $"{process.ProcessName} ({process.Id}) -> High");
                            }
                        }
                        catch (Exception ex)
                        {
                            WindowFactory.AppendProgress(_progressTextBox, $"Erro ao alterar {process.ProcessName} ({process.Id}): {ex.Message}");
                        }
                    }

                    if (changedCount > 0)
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
    }
}
