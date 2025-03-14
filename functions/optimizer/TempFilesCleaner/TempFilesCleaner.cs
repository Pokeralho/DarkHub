using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class TempFilesCleaner
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public TempFilesCleaner(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.CleaningProgressTitle);
            _progressWindow.Owner = owner;
        }

        public async Task StartCleanupAsync()
        {
            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingCleanup);
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
                    string windows = Environment.GetEnvironmentVariable("windir") ?? @"C:\Windows";
                    string systemDrive = Environment.GetEnvironmentVariable("SystemDrive") ?? "C:";
                    string userProfile = Environment.GetEnvironmentVariable("USERPROFILE") ?? @"C:\Users\Default";
                    string temp = Environment.GetEnvironmentVariable("TEMP") ?? Path.Combine(userProfile, "AppData", "Local", "Temp");

                    var tasks = new List<(string Description, Action Action)>
                    {
                        (ResourceManagerHelper.Instance.CleaningWindowsTemp, () => ClearDirectory($"{windows}\\temp", _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningPrefetchExe, () => ClearFilesByExtension($"{windows}\\Prefetch", "*.exe", _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningPrefetchDll, () => ClearFilesByExtension($"{windows}\\Prefetch", "*.dll", _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningPrefetchPf, () => ClearFilesByExtension($"{windows}\\Prefetch", "*.pf", _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningDllCache, () => ClearDirectory($"{windows}\\system32\\dllcache", _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningSystemDriveTemp, () => ClearDirectory($"{systemDrive}\\Temp", _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningUserTemp, () => ClearDirectory(temp, _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningHistory, () => ClearDirectory(Path.Combine(userProfile, "Local Settings", "History"), _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningTempInternetFiles, () => ClearDirectory(Path.Combine(userProfile, "Local Settings", "Temporary Internet Files"), _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningLocalTemp, () => ClearDirectory(Path.Combine(userProfile, "Local Settings", "Temp"), _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningRecent, () => ClearDirectory(Path.Combine(userProfile, "Recent"), _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningCookies, () => ClearDirectory(Path.Combine(userProfile, "Cookies"), _progressTextBox)),
                        (ResourceManagerHelper.Instance.CleaningEventLogs, () => ClearEventLogsWithWevtutil(_progressTextBox))
                    };

                    foreach (var task in tasks)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, task.Description);
                        await Task.Run(task.Action);
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                        await Task.Delay(100);
                    }
                });

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.CleanupCompletedSuccess);
                await Task.Run(() => MessageBox.Show(ResourceManagerHelper.Instance.CleanupCompleted,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.GeneralCleanupError, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.GeneralCleanupError, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _button.IsEnabled = true;
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Close()));
            }
        }

        private static void ClearDirectory(string directoryPath, TextBox progressTextBox)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                WindowFactory.AppendProgress(progressTextBox, ResourceManagerHelper.Instance.DirectoryNotFoundOrInvalid);
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
                        WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.AccessDeniedToFile, file.FullName, ex.Message));
                    }
                    catch (IOException ex)
                    {
                        WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.FileInUse, file.FullName, ex.Message));
                    }
                    catch (Exception ex)
                    {
                        WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorDeletingFile, file.FullName, ex.Message));
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
                        WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorDeletingSubdirectory, dir.FullName, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningDirectory, directoryPath, ex.Message));
            }
        }

        private static void ClearFilesByExtension(string directoryPath, string extension, TextBox progressTextBox)
        {
            if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
            {
                WindowFactory.AppendProgress(progressTextBox, ResourceManagerHelper.Instance.DirectoryNotFoundOrInvalid);
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
                        WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.AccessDeniedToFile, file.FullName, ex.Message));
                    }
                    catch (IOException ex)
                    {
                        WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.FileInUse, file.FullName, ex.Message));
                    }
                    catch (Exception ex)
                    {
                        WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorDeletingFile, file.FullName, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningFilesByExtension, extension, directoryPath, ex.Message));
            }
        }

        private static void ClearEventLogsWithWevtutil(TextBox progressTextBox)
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
                    WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.CleaningLog, logName));
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
                            WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.LogError, error));
                        else
                            WindowFactory.AppendProgress(progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                    }
                    catch (Exception ex)
                    {
                        WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningLog, logName, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningLogs, ex.Message));
            }
        }
    }
}