using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class TimerResolutionAdjuster
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public TimerResolutionAdjuster(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.TimerResolutionTitle);
            _progressWindow.Owner = owner;
        }

        public async Task AdjustTimerResolutionAsync()
        {
            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingTimerAdjustment);
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
                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.AdjustingTimer);
                    try
                    {
                        uint currentResolution, desiredResolution = 5000, actualResolution;
                        NtQueryTimerResolution(out currentResolution, out _, out _);
                        NtSetTimerResolution(desiredResolution, true, out actualResolution);
                        WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.TimerAdjusted, (actualResolution / 10000f).ToString("F2")));
                    }
                    catch (Exception ex)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorAdjustingTimerAPI, ex.Message));
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.RunningPowercfgFallback);
                        ExecuteCommandWithOutput("powercfg /energy", _progressTextBox);
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.FallbackCompleted);
                    }

                    await Task.Delay(100);
                });

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.TimerAdjustmentCompleted);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.TimerResolutionAdjusted,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorAdjustingTimerResolution, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorAdjustingTimerResolution, ex.Message),
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

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetTimerResolution(uint DesiredResolution, bool Set, out uint CurrentResolution);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtQueryTimerResolution(out uint MinimumResolution, out uint MaximumResolution, out uint CurrentResolution);
    }
}