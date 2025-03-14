using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class RegistryCleaner
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public RegistryCleaner(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.CleaningRegistryTitle);
            _progressWindow.Owner = owner;
        }

        public async Task CleanRegistryAsync()
        {
            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingRegistryCleanup);
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
                    string[] registryPaths =
                    {
                        @"Software\Microsoft\Windows\CurrentVersion\Run",
                        @"Software\Microsoft\Windows\CurrentVersion\RunOnce"
                    };

                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ScanningRegistry);

                    List<(string KeyPath, string Name, string Value)> invalidEntries = new();
                    foreach (var path in registryPaths)
                    {
                        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
                        {
                            string fullPath = root == Registry.CurrentUser ? $"HKEY_CURRENT_USER\\{path}" : $"HKEY_LOCAL_MACHINE\\{path}";
                            using (var key = root.OpenSubKey(path))
                            {
                                if (key != null)
                                {
                                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.CheckingRegistryPath, fullPath));
                                    foreach (var valueName in key.GetValueNames())
                                    {
                                        string? value = key.GetValue(valueName)?.ToString();
                                        if (!string.IsNullOrEmpty(value) && value.Contains(".exe") && !File.Exists(value))
                                        {
                                            invalidEntries.Add((fullPath, valueName, value));
                                            WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.InvalidEntryDetail, fullPath, valueName, value));
                                        }
                                    }
                                }
                                else
                                {
                                    WindowFactory.AppendProgress(_progressTextBox, $"{fullPath} não acessível ou inexistente.\n");
                                }
                            }
                        }
                    }

                    if (invalidEntries.Count == 0)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, "Nenhuma entrada inválida encontrada.\n");
                        return;
                    }

                    WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.InvalidEntriesFound, invalidEntries.Count));
                    foreach (var entry in invalidEntries)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.InvalidEntryDetail, entry.KeyPath, entry.Name, entry.Value));
                    }

                    bool confirmed = await _progressTextBox.Dispatcher.InvokeAsync(() =>
                    {
                        var result = MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ConfirmRemoveInvalidEntries, invalidEntries.Count),
                            ResourceManagerHelper.Instance.ConfirmationTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
                        return result == MessageBoxResult.Yes;
                    });

                    if (confirmed)
                    {
                        foreach (var entry in invalidEntries)
                        {
                            try
                            {
                                using (var key = entry.KeyPath.StartsWith("HKEY_CURRENT_USER") ?
                                    Registry.CurrentUser.OpenSubKey(entry.KeyPath.Replace("HKEY_CURRENT_USER\\", ""), writable: true) :
                                    Registry.LocalMachine.OpenSubKey(entry.KeyPath.Replace("HKEY_LOCAL_MACHINE\\", ""), writable: true))
                                {
                                    if (key != null)
                                    {
                                        key.DeleteValue(entry.Name);
                                        WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.EntryRemoved, entry.KeyPath, entry.Name));
                                    }
                                    else
                                    {
                                        WindowFactory.AppendProgress(_progressTextBox, $"Erro: Não foi possível abrir {entry.KeyPath} para escrita.\n");
                                    }
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.PermissionDeniedRemovingEntry, entry.KeyPath, entry.Name));
                            }
                            catch (Exception ex)
                            {
                                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorRemovingEntry, entry.KeyPath, entry.Name, ex.Message));
                            }
                        }
                    }
                    else
                    {
                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.CleanupCancelledByUser);
                    }

                    await Task.Delay(100);
                });

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.RegistryCleanupCompleted);

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

                    MessageBox.Show(ResourceManagerHelper.Instance.RegistryCleanupCompleted,
                        ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                    if (owner != null && owner.IsLoaded && isOwnerVisible)
                    {
                        owner.WindowState = ownerState;
                        owner.Activate();
                        owner.Focus();
                    }
                }));
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorCleaningRegistry, ex.Message));
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCleaningRegistry, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _button.IsEnabled = true;

                var owner = _progressWindow.Owner;
                var ownerState = WindowState.Normal;
                var isOwnerVisible = false;

                if (owner != null && owner.IsLoaded)
                {
                    ownerState = owner.WindowState;
                    isOwnerVisible = owner.IsVisible;
                }

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Close()));

                if (owner != null && owner.IsLoaded && isOwnerVisible)
                {
                    owner.Dispatcher.Invoke(() =>
                    {
                        owner.WindowState = ownerState;
                        owner.Activate();
                        owner.Focus();
                    });
                }
            }
        }
    }
}