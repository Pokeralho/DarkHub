using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class MemoryOptimizer
    {
        private Window _progressWindow;
        private TextBox _progressTextBox;
        private readonly Button _button;

        public MemoryOptimizer(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.OptimizingMemoryTitle);
            _progressWindow.Owner = owner;
        }

        public async Task OptimizeMemoryAsync()
        {
            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingMemoryOptimization);

                await Task.Run(async () =>
                {
                    var initialMemoryInfo = await GetMemoryInfo();
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryUsageBefore, initialMemoryInfo.UsedMemory.ToString("N2")));
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryAvailableBefore, initialMemoryInfo.AvailableMemory.ToString("N2")));

                    var safeProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "chrome", "firefox", "msedge", "iexplore", "opera",
                        "notepad", "wordpad", "mspaint", "calc",
                        "winrar", "7zg", "vlc", "wmplayer",
                        "explorer", "powershell", "cmd"
                    };

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.OptimizingNonEssentialProcesses);
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
                                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ProcessesOptimized, processesOptimized));
                                }
                            }
                        }
                        catch { }
                    }

                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.TotalProcessesOptimized, processesOptimized));
                    await Task.Delay(1000);

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ClearingFileSystemCache);
                    await WindowFactory.ExecuteCommandWithOutputAsync("powershell -Command \"Clear-RecycleBin -Force -ErrorAction SilentlyContinue\"", _progressTextBox);
                    await Task.Delay(1000);

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ClearingTempFiles);
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

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.RunningGarbageCollection);
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(1000);

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ClearingDNSCacheMemory);
                    await WindowFactory.ExecuteCommandWithOutputAsync("ipconfig /flushdns", _progressTextBox);
                    await Task.Delay(1000);

                    var finalMemoryInfo = await GetMemoryInfo();
                    double memorySaved = initialMemoryInfo.UsedMemory - finalMemoryInfo.UsedMemory;

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.OptimizationResults);
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryUsageAfter, finalMemoryInfo.UsedMemory.ToString("N2")));
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryAvailableAfter, finalMemoryInfo.AvailableMemory.ToString("N2")));
                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.MemoryFreed, memorySaved.ToString("N2")));
                });

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

                    MessageBox.Show(ResourceManagerHelper.Instance.MemoryOptimizationSuccess,
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
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorOptimizingMemory, ex.Message));

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

                    MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorOptimizingMemory, ex.Message),
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
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Close()));
            }
        }

        private async Task<(double UsedMemory, double AvailableMemory)> GetMemoryInfo()
        {
            try
            {
                string result = await WindowFactory.ExecuteCommandWithOutputAsync("powershell -Command \"Get-CimInstance Win32_OperatingSystem | Select-Object -Property TotalVisibleMemorySize,FreePhysicalMemory | ConvertTo-Json\"", _progressTextBox);

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
                Debug.WriteLine($"Erro em GetMemoryInfo (PowerShell): {ex.Message}");
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
                Debug.WriteLine($"Erro em GetMemoryInfo (ComputerInfo): {ex.Message}");
            }

            return (0, 0);
        }

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwProc);
    }
}