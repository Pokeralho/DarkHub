using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class SpaceSnifferRunner
    {
        private Window _progressWindow;
        private TextBox _progressTextBox;
        private readonly Button _button;

        public SpaceSnifferRunner(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.RunningSpaceSnifferTitle);
            _progressWindow.Owner = owner;
        }

        public async Task RunSpaceSnifferAsync()
        {
            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingSpaceSniffer);

                await ExecuteSpaceSnifferAsync();

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.SpaceSnifferExecutedSuccess);
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorRunningSpaceSniffer, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorRunningSpaceSniffer, ex.Message),
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

        private async Task ExecuteSpaceSnifferAsync()
        {
            await Task.Run(async () =>
            {
                string assetsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
                string executablePath = Path.Combine(assetsFolder, "SpaceSniffer.exe");

                if (!Directory.Exists(assetsFolder))
                {
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.AssetsFolderNotFound, AppDomain.CurrentDomain.BaseDirectory));
                    throw new DirectoryNotFoundException("Pasta 'assets' não encontrada.");
                }

                if (!File.Exists(executablePath))
                {
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.SpaceSnifferNotFound, assetsFolder));
                    throw new FileNotFoundException($"Executável 'SpaceSniffer.exe' não encontrado em {assetsFolder}.");
                }

                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ExecutingSpaceSniffer, executablePath));

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
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.FailedToStartProcess);
                        return;
                    }

                    await process.WaitForExitAsync();
                    int exitCode = process.ExitCode;
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ProcessCompletedWithExitCode, exitCode));
                }
            });
        }
    }
}