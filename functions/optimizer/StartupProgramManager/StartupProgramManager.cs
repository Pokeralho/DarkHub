using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class StartupProgramManager
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public StartupProgramManager(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.ManagingStartupProgramsTitle);
            _progressWindow.Owner = owner;
        }

        public async Task ManageStartupProgramsAsync()
        {
            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingStartupManagement);
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
                    string[] startupPaths =
                    {
                        @"Software\Microsoft\Windows\CurrentVersion\Run",
                        @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                        @"Software\Microsoft\Windows\CurrentVersion\RunServices"
                    };

                    string userStartupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                    string allUsersStartupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\Startup");

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ListingStartupPrograms);
                    Dictionary<string, (string Path, string Source, bool IsRegistry, RegistryKey? Root)> startupItems = new();

                    foreach (var path in startupPaths)
                    {
                        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
                        {
                            string fullPath = root == Registry.CurrentUser ? $"HKCU\\{path}" : $"HKLM\\{path}";
                            using (var key = root.OpenSubKey(path, writable: false))
                            {
                                if (key != null)
                                {
                                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.CheckingStartupPath, fullPath));
                                    foreach (var name in key.GetValueNames())
                                    {
                                        string? value = key.GetValue(name)?.ToString();
                                        if (!string.IsNullOrEmpty(value))
                                        {
                                            startupItems[name] = (value, fullPath, true, root);
                                            WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.StartupItemFound, fullPath, name, value));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    foreach (var folder in new[] { userStartupFolder, allUsersStartupFolder })
                    {
                        if (Directory.Exists(folder))
                        {
                            WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.CheckingStartupFolder, folder));
                            foreach (var file in Directory.EnumerateFiles(folder, "*.lnk", SearchOption.TopDirectoryOnly))
                            {
                                string name = Path.GetFileNameWithoutExtension(file);
                                string targetPath = GetShortcutTarget(file);
                                if (!string.IsNullOrEmpty(targetPath))
                                {
                                    startupItems[name] = (targetPath, folder, false, null);
                                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.StartupFolderItemFound, folder, name, targetPath));
                                }
                            }
                        }
                    }

                    if (startupItems.Count == 0)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.NoStartupProgramsFound);
                        return;
                    }

                    await _progressTextBox.Dispatcher.InvokeAsync(() =>
                    {
                        var selectionWindow = WindowFactory.CreateWindow(
                            title: ResourceManagerHelper.Instance.SelectProgramToDisableTitle,
                            width: 400,
                            height: 300,
                            owner: _progressWindow,
                            isModal: true,
                            resizable: false
                        );

                        var stackPanel = new StackPanel { Margin = new Thickness(10) };
                        var listBox = WindowFactory.CreateStyledListBox(200);
                        var disableButton = WindowFactory.CreateStyledButton(
                            ResourceManagerHelper.Instance.DisableSelectedButton,
                            150,
                            (s, ev) => { }
                        );
                        disableButton.Margin = new Thickness(0, 10, 0, 0);

                        foreach (var item in startupItems)
                        {
                            listBox.Items.Add($"{item.Key} ({item.Value.Source}) -> {item.Value.Path}");
                        }

                        disableButton.Click += (s, ev) =>
                        {
                            if (listBox.SelectedItem != null)
                            {
                                string selectedFull = listBox.SelectedItem.ToString();
                                string selectedName = selectedFull.Split(" -> ")[0].Split(" (")[0];
                                if (startupItems.TryGetValue(selectedName, out var item))
                                {
                                    try
                                    {
                                        if (item.IsRegistry && item.Root != null)
                                        {
                                            using (var key = item.Root.OpenSubKey(item.Source.Replace(item.Root.Name + "\\", ""), writable: true))
                                            {
                                                if (key != null)
                                                {
                                                    key.DeleteValue(selectedName);
                                                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ProgramDisabledInRegistry, selectedName));
                                                }
                                                else
                                                {
                                                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorOpeningStartupSource, item.Source));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            string shortcutPath = Path.Combine(item.Source, $"{selectedName}.lnk");
                                            if (File.Exists(shortcutPath))
                                            {
                                                File.Delete(shortcutPath);
                                                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ProgramRemovedFromStartupFolder, selectedName));
                                            }
                                            else
                                            {
                                                WindowFactory.AppendProgress(_progressTextBox, $"Erro: Atalho {shortcutPath} não encontrado.\n");
                                            }
                                        }
                                        listBox.Items.Remove(listBox.SelectedItem);
                                    }
                                    catch (UnauthorizedAccessException)
                                    {
                                        WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.PermissionDeniedDisablingProgram, selectedName));
                                    }
                                    catch (Exception ex)
                                    {
                                        WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorDisablingProgram, selectedName, ex.Message));
                                    }
                                }
                            }
                        };

                        stackPanel.Children.Add(listBox);
                        stackPanel.Children.Add(disableButton);
                        selectionWindow.Content = stackPanel;
                        selectionWindow.ShowDialog();
                    });
                });

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartupManagementCompleted);
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorManagingStartupPrograms, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorManagingStartupPrograms, ex.Message),
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

        private static string GetShortcutTarget(string shortcutPath)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return string.Empty;

                object? shell = Activator.CreateInstance(shellType);
                if (shell == null) return string.Empty;

                object? shortcut = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
                if (shortcut == null) return string.Empty;

                string target = (string)shortcut.GetType().InvokeMember("TargetPath", System.Reflection.BindingFlags.GetProperty, null, shortcut, null);
                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);
                return target ?? string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em GetShortcutTarget {ex.Message}");
                return string.Empty;
            }
        }
    }
}