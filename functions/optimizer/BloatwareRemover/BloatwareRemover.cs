﻿using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class BloatwareRemover
    {
        private Window _progressWindow;
        private TextBox _progressTextBox;
        private readonly Button _button;

        public BloatwareRemover(Window owner, Button button)
        {
            _button = button ?? throw new ArgumentNullException(nameof(button));
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.CheckingInstalledAppsTitle);
            _progressWindow.Owner = owner;
        }

        public async Task RemoveWindowsBloatwareAsync()
        {
            if (!IsRunningAsAdmin())
            {
                MessageBox.Show("Este programa precisa ser executado como administrador para remover bloatware.",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _button.IsEnabled = false;
            try
            {
                await _progressWindow.Dispatcher.InvokeAsync(() => _progressWindow.Show());
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.CheckingInstalledApps);

                var potentialBloatware = new Dictionary<string, string>
                {
                    {"Microsoft.OneDrive", "OneDrive"},
                    {"Microsoft.Edge", "Microsoft Edge"},
                    {"Microsoft.BingNews", "Bing News"},
                    {"Microsoft.BingWeather", "Bing Weather"},
                    {"Microsoft.GetHelp", "Get Help"},
                    {"Microsoft.Getstarted", "Get Started"},
                    {"Microsoft.MicrosoftOfficeHub", "Office Hub"},
                    {"Microsoft.MicrosoftSolitaireCollection", "Solitaire Collection"},
                    {"Microsoft.People", "People"},
                    {"Microsoft.WindowsFeedbackHub", "Feedback Hub"},
                    {"Microsoft.WindowsMaps", "Windows Maps"},
                    {"Microsoft.Xbox.TCUI", "Xbox TCUI"},
                    {"Microsoft.XboxApp", "Xbox App"},
                    {"Microsoft.XboxGameOverlay", "Xbox Game Overlay"},
                    {"Microsoft.XboxGamingOverlay", "Xbox Gaming Overlay"},
                    {"Microsoft.XboxIdentityProvider", "Xbox Identity Provider"},
                    {"Microsoft.XboxSpeechToTextOverlay", "Xbox Speech To Text Overlay"},
                    {"Microsoft.ZuneMusic", "Groove Music"},
                    {"Microsoft.ZuneVideo", "Movies & TV"}
                };

                var installedApps = await DetectBloatwareAsync(potentialBloatware);

                await _progressWindow.Dispatcher.InvokeAsync(() =>
                {
                    _progressWindow.Close();
                    if (_progressWindow.Owner != null && _progressWindow.Owner.IsLoaded)
                    {
                        _progressWindow.Owner.WindowState = WindowState.Normal;
                        _progressWindow.Owner.Activate();
                        _progressWindow.Owner.Focus();
                    }
                });

                if (!installedApps.Any())
                {
                    await ShowMessageAsync(ResourceManagerHelper.Instance.NoBloatwareFound, ResourceManagerHelper.Instance.InfoTitle, MessageBoxImage.Information);
                    _button.IsEnabled = true;
                    return;
                }

                var selectedApps = await ShowBloatwareSelectionWindowAsync(installedApps);
                if (!selectedApps.Any())
                {
                    await ShowMessageAsync(ResourceManagerHelper.Instance.NoAppsSelectedForRemoval, ResourceManagerHelper.Instance.WarningTitle, MessageBoxImage.Information);
                    _button.IsEnabled = true;
                    return;
                }

                (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.RemovingBloatwareTitle);
                await _progressWindow.Dispatcher.InvokeAsync(() => _progressWindow.Show());
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingBloatwareRemoval);

                await RemoveSelectedAppsAsync(selectedApps);

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.BloatwareRemovalCompleted);
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.RestartRecommended);

                if (await ShowYesNoMessageAsync(ResourceManagerHelper.Instance.BloatwareRemovalDone, ResourceManagerHelper.Instance.CompletedTitle))
                {
                    Process.Start("shutdown.exe", "/r /t 10");
                    await ShowMessageAsync(ResourceManagerHelper.Instance.RestartingIn10Seconds, ResourceManagerHelper.Instance.RestartingTitle, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, $"Erro ao remover bloatware: {ex.Message}");
                await ShowMessageAsync($"Erro ao remover bloatware: {ex.Message}", ResourceManagerHelper.Instance.ErrorTitle, MessageBoxImage.Error);
            }
            finally
            {
                _button.IsEnabled = true;
                if (_progressWindow != null)
                {
                    await _progressWindow.Dispatcher.InvokeAsync(() =>
                    {
                        _progressWindow.Close();
                        if (_progressWindow.Owner != null && _progressWindow.Owner.IsLoaded)
                        {
                            _progressWindow.Owner.WindowState = WindowState.Normal;
                            _progressWindow.Owner.Activate();
                            _progressWindow.Owner.Focus();
                        }
                    });
                }
            }
        }

        private async Task<Dictionary<string, string>> DetectBloatwareAsync(Dictionary<string, string> potentialBloatware)
        {
            var installedApps = new Dictionary<string, string>();

            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive")) ||
                Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive")))
            {
                installedApps.Add("Microsoft.OneDrive", potentialBloatware["Microsoft.OneDrive"]);
                WindowFactory.AppendProgress(_progressTextBox, "Encontrado: OneDrive\n");
            }

            if (Directory.Exists(@"C:\Program Files (x86)\Microsoft\Edge") ||
                Directory.Exists(@"C:\Program Files\Microsoft\Edge"))
            {
                installedApps.Add("Microsoft.Edge", potentialBloatware["Microsoft.Edge"]);
                WindowFactory.AppendProgress(_progressTextBox, "Encontrado: Microsoft Edge\n");
            }

            string result = await RunCommandAsync("powershell -Command \"Get-AppxPackage -AllUsers | Select-Object -Property Name\"");
            string provisionedResult = await RunCommandAsync("powershell -Command \"Get-AppxProvisionedPackage -Online | Select-Object -Property DisplayName\"");
            var installedPackages = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var provisionedPackages = provisionedResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var app in potentialBloatware)
            {
                if (app.Key != "Microsoft.OneDrive" && app.Key != "Microsoft.Edge")
                {
                    if (installedPackages.Any(p => p.Contains(app.Key, StringComparison.OrdinalIgnoreCase)) ||
                        provisionedPackages.Any(p => p.Contains(app.Key, StringComparison.OrdinalIgnoreCase)))
                    {
                        installedApps.Add(app.Key, app.Value);
                        WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.AppFound, app.Value));
                    }
                }
            }

            return installedApps;
        }

        private async Task RemoveSelectedAppsAsync(Dictionary<string, string> selectedApps)
        {
            if (selectedApps.ContainsKey("Microsoft.OneDrive"))
            {
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.RemovingOneDrive);
                await RemoveOneDriveAsync();
            }

            if (selectedApps.ContainsKey("Microsoft.Edge"))
            {
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.RemovingMicrosoftEdge);
                await RemoveEdgeAsync();
            }

            foreach (var app in selectedApps.Where(a => a.Key != "Microsoft.OneDrive" && a.Key != "Microsoft.Edge"))
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.RemovingApp, app.Value));

                await RunCommandAsync($"powershell -Command \"Get-AppxPackage *{app.Key}* | Remove-AppxPackage -ErrorAction SilentlyContinue\"");

                await RunCommandAsync($"powershell -Command \"Get-AppxPackage -AllUsers *{app.Key}* | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue\"");

                string removeProvisionedCommand = @"
                    $packages = Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -like '*" + app.Key + @"*' };
                    foreach ($pkg in $packages) { Remove-AppxProvisionedPackage -Online -PackageName $pkg.PackageName -ErrorAction SilentlyContinue; }
                ";
                await RunCommandAsync($"powershell -Command \"{removeProvisionedCommand}\"", true);

                string forceRemoveCommand = @"
                    $packages = Get-AppxPackage -AllUsers | Where-Object { $_.Name -like '*" + app.Key + @"*' };
                    foreach ($pkg in $packages) { Remove-AppxPackage -Package $pkg.PackageFullName -AllUsers -ErrorAction SilentlyContinue; }
                ";
                await RunCommandAsync($"powershell -Command \"{forceRemoveCommand}\"", true);

                await Task.Delay(200);
            }

            await CleanResidualFilesAsync(selectedApps);
        }

        private async Task RemoveOneDriveAsync()
        {
            await RunCommandAsync("taskkill /f /im OneDrive.exe", true);

            string[] possiblePaths = {
                @"%SystemRoot%\System32\OneDriveSetup.exe",
                @"%SystemRoot%\SysWOW64\OneDriveSetup.exe",
                @"%LOCALAPPDATA%\Microsoft\OneDrive\OneDriveSetup.exe",
                @"%ProgramFiles%\Microsoft OneDrive\OneDriveSetup.exe",
                @"%ProgramFiles(x86)%\Microsoft OneDrive\OneDriveSetup.exe"
            };

            bool uninstallSuccess = false;
            foreach (string path in possiblePaths)
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(path);
                if (File.Exists(expandedPath))
                {
                    WindowFactory.AppendProgress(_progressTextBox, $"Executando desinstalador do OneDrive em: {expandedPath}");
                    await RunCommandAsync($"\"{expandedPath}\" /uninstall", true);
                    uninstallSuccess = true;
                    break;
                }
            }

            if (!uninstallSuccess)
            {
                WindowFactory.AppendProgress(_progressTextBox, "Desinstalador do OneDrive não encontrado. Usando método alternativo...");
                await RunCommandAsync("reg export HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\OneDrive backup_onedrive.reg /y", true);
                string psCommand = @"
                    New-Item -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\OneDrive' -Force -ErrorAction SilentlyContinue;
                    Set-ItemProperty -Path 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\OneDrive' -Name 'DisableFileSyncNGSC' -Value 1 -Type DWord -Force;
                    Set-ItemProperty -Path 'HKCR:\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}' -Name 'System.IsPinnedToNameSpaceTree' -Value 0 -Force -ErrorAction SilentlyContinue;
                    Set-ItemProperty -Path 'HKCR:\Wow6432Node\CLSID\{018D5C66-4533-4307-9B53-224DE2ED1FE6}' -Name 'System.IsPinnedToNameSpaceTree' -Value 0 -Force -ErrorAction SilentlyContinue;
                    Stop-Process -Name 'OneDrive' -Force -ErrorAction SilentlyContinue;
                    Stop-Service -Name 'OneSyncSvc' -Force -ErrorAction SilentlyContinue;
                    Set-Service -Name 'OneSyncSvc' -StartupType Disabled -ErrorAction SilentlyContinue;
                ";
                await RunCommandAsync($"powershell -Command \"{psCommand}\"", true);
                await RunCommandAsync("rd \"%UserProfile%\\OneDrive\" /Q /S", true);
                await RunCommandAsync("rd \"%LocalAppData%\\Microsoft\\OneDrive\" /Q /S", true);
                await RunCommandAsync("rd \"%ProgramData%\\Microsoft OneDrive\" /Q /S", true);
                WindowFactory.AppendProgress(_progressTextBox, "OneDrive desabilitado e removido via políticas do sistema.");
            }
        }

        private async Task RemoveEdgeAsync()
        {
            WindowFactory.AppendProgress(_progressTextBox, "Tentando remover o Microsoft Edge...");

            await RunCommandAsync("taskkill /f /im msedge.exe", true);
            await RunCommandAsync("taskkill /f /im MicrosoftEdgeUpdate.exe", true);

            string removeEdgeCommand = @"
                Get-AppxPackage -AllUsers *MicrosoftEdge* | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue;
                Get-AppxPackage -AllUsers *Edge* | Remove-AppxPackage -AllUsers -ErrorAction SilentlyContinue;
            ";
            await RunCommandAsync($"powershell -Command \"{removeEdgeCommand}\"", true);

            string[] edgePaths = {
                @"C:\Program Files (x86)\Microsoft\Edge\Application",
                @"C:\Program Files\Microsoft\Edge\Application"
            };

            foreach (string edgePath in edgePaths)
            {
                if (Directory.Exists(edgePath))
                {
                    string[] installerPaths = Directory.GetFiles(edgePath, "setup.exe", SearchOption.AllDirectories);
                    foreach (string installer in installerPaths)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, $"Executando desinstalador do Edge em: {installer}");
                        await RunCommandAsync($"\"{installer}\" --uninstall --system-level --verbose-logging --force-uninstall", true);
                    }
                }
            }

            await RunCommandAsync("reg add \"HKLM\\SOFTWARE\\Microsoft\\EdgeUpdate\" /v DoNotUpdateToEdgeWithChromium /t REG_DWORD /d 1 /f", true);
            await RunCommandAsync("reg add \"HKLM\\SOFTWARE\\Policies\\Microsoft\\EdgeUpdate\" /v AutoUpdateEnabled /t REG_DWORD /d 0 /f", true);

            foreach (string edgePath in edgePaths)
            {
                if (Directory.Exists(edgePath))
                {
                    WindowFactory.AppendProgress(_progressTextBox, "Edge ainda detectado. Tentando remoção agressiva...");
                    await RunCommandAsync($"rd \"{edgePath}\" /S /Q", true);
                }
            }

            WindowFactory.AppendProgress(_progressTextBox, "Remoção do Microsoft Edge concluída. Reinicie o sistema para confirmar.");
        }

        private async Task CleanResidualFilesAsync(Dictionary<string, string> selectedApps)
        {
            WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.CleaningResidualFiles);
            var foldersToDelete = new List<string>();

            if (selectedApps.ContainsKey("Microsoft.Edge"))
            {
                foldersToDelete.Add(@"%LOCALAPPDATA%\Microsoft\Edge");
                foldersToDelete.Add(@"%LOCALAPPDATA%\Microsoft\EdgeUpdate");
                foldersToDelete.Add(@"C:\Program Files (x86)\Microsoft\Edge");
                foldersToDelete.Add(@"C:\Program Files\Microsoft\Edge");
                foldersToDelete.Add(@"%ProgramData%\Microsoft\Edge");
            }

            if (selectedApps.ContainsKey("Microsoft.OneDrive"))
            {
                foldersToDelete.Add(@"%LOCALAPPDATA%\Microsoft\OneDrive");
                foldersToDelete.Add(@"%PROGRAMDATA%\Microsoft\OneDrive");
                foldersToDelete.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "OneDrive"));
            }

            foreach (var folder in foldersToDelete)
            {
                string expandedPath = Environment.ExpandEnvironmentVariables(folder);
                if (Directory.Exists(expandedPath))
                {
                    try
                    {
                        Directory.Delete(expandedPath, true);
                        WindowFactory.AppendProgress(_progressTextBox, $"Pasta {expandedPath} removida.");
                    }
                    catch (Exception ex)
                    {
                        await RunCommandAsync($"rd \"{expandedPath}\" /Q /S", true);
                        WindowFactory.AppendProgress(_progressTextBox, $"Erro ao remover {expandedPath} localmente, removido via comando: {ex.Message}");
                    }
                }
            }
        }

        private async Task<Dictionary<string, string>> ShowBloatwareSelectionWindowAsync(Dictionary<string, string> installedApps)
        {
            var tcs = new TaskCompletionSource<Dictionary<string, string>>();
            await _progressWindow.Dispatcher.InvokeAsync(() =>
            {
                var selectionWindow = WindowFactory.CreateWindow(
                    title: ResourceManagerHelper.Instance.SelectAppsToRemoveTitle,
                    width: 700,
                    height: 600,
                    owner: _progressWindow.Owner,
                    isModal: true,
                    resizable: false
                );

                var mainGrid = new Grid();
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var titleText = new TextBlock
                {
                    Text = string.Format(ResourceManagerHelper.Instance.AppsFoundToRemove, installedApps.Count),
                    Foreground = WindowFactory.DefaultTextForeground,
                    FontSize = WindowFactory.DefaultFontSize + 2,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(10),
                    TextWrapping = TextWrapping.Wrap
                };
                Grid.SetRow(titleText, 0);

                var scrollViewer = new ScrollViewer
                {
                    Margin = new Thickness(10),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                };
                Grid.SetRow(scrollViewer, 1);

                var checkBoxPanel = new StackPanel { Margin = new Thickness(10) };
                var checkBoxes = new Dictionary<string, CheckBox>();

                foreach (var app in installedApps)
                {
                    var checkBox = new CheckBox
                    {
                        Content = app.Value,
                        Foreground = WindowFactory.DefaultTextForeground,
                        Margin = new Thickness(5),
                        Tag = app.Key
                    };
                    checkBoxes[app.Key] = checkBox;
                    checkBoxPanel.Children.Add(checkBox);
                }

                scrollViewer.Content = checkBoxPanel;

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                };
                Grid.SetRow(buttonPanel, 2);

                var selectAllButton = WindowFactory.CreateStyledButton(ResourceManagerHelper.Instance.SelectAllButton, 150, (s, e) =>
                {
                    foreach (var cb in checkBoxes.Values) cb.IsChecked = true;
                });
                var deselectAllButton = WindowFactory.CreateStyledButton(ResourceManagerHelper.Instance.DeselectAllButton, 150, (s, e) =>
                {
                    foreach (var cb in checkBoxes.Values) cb.IsChecked = false;
                });
                var confirmButton = WindowFactory.CreateStyledButton(ResourceManagerHelper.Instance.RemoveSelectedButton, 170, (s, e) =>
                {
                    selectionWindow.DialogResult = true;
                    selectionWindow.Close();
                });

                selectAllButton.Margin = new Thickness(0, 0, 10, 0);
                deselectAllButton.Margin = new Thickness(0, 0, 10, 0);

                buttonPanel.Children.Add(selectAllButton);
                buttonPanel.Children.Add(deselectAllButton);
                buttonPanel.Children.Add(confirmButton);

                mainGrid.Children.Add(titleText);
                mainGrid.Children.Add(scrollViewer);
                mainGrid.Children.Add(buttonPanel);

                selectionWindow.Content = mainGrid;

                selectionWindow.Closed += (s, args) =>
                {
                    if (selectionWindow.DialogResult == true)
                    {
                        var selectedApps = checkBoxes.Where(cb => cb.Value.IsChecked == true)
                            .ToDictionary(cb => cb.Key, cb => installedApps[cb.Key]);
                        tcs.SetResult(selectedApps);
                    }
                    else
                    {
                        tcs.SetResult(new Dictionary<string, string>());
                    }
                };

                selectionWindow.ShowDialog();
            });

            return await tcs.Task;
        }

        private async Task<string> RunCommandAsync(string command, bool requiresAdmin = false)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = requiresAdmin,
                    RedirectStandardOutput = !requiresAdmin,
                    RedirectStandardError = !requiresAdmin,
                    CreateNoWindow = !requiresAdmin,
                    Verb = requiresAdmin ? "runas" : string.Empty
                };

                if (!requiresAdmin)
                {
                    processStartInfo.StandardOutputEncoding = Encoding.GetEncoding(850);
                    processStartInfo.StandardErrorEncoding = Encoding.GetEncoding(850);
                }

                using var process = new Process { StartInfo = processStartInfo };
                WindowFactory.AppendProgress(_progressTextBox, $"Executando: {command}" + (requiresAdmin ? " (como administrador)" : ""));
                process.Start();

                string output = requiresAdmin ? "" : await process.StandardOutput.ReadToEndAsync();
                string error = requiresAdmin ? "" : await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0 && !requiresAdmin)
                {
                    WindowFactory.AppendProgress(_progressTextBox, $"Erro ao executar comando: {error}");
                    return $"Erro: {error}";
                }

                if (requiresAdmin)
                {
                    WindowFactory.AppendProgress(_progressTextBox, "Comando executado com privilégios de administrador.");
                    return "Comando executado.";
                }

                if (!string.IsNullOrEmpty(output))
                    WindowFactory.AppendProgress(_progressTextBox, output);
                if (!string.IsNullOrEmpty(error))
                    WindowFactory.AppendProgress(_progressTextBox, $"Erro: {error}");

                return output + error;
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, $"Erro ao executar comando: {ex.Message}");
                return $"Erro: {ex.Message}";
            }
        }

        private bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private Task ShowMessageAsync(string message, string title, MessageBoxImage icon)
        {
            return _progressWindow.Dispatcher.InvokeAsync(() => MessageBox.Show(message, title, MessageBoxButton.OK, icon)).Task;
        }

        private Task<bool> ShowYesNoMessageAsync(string message, string title)
        {
            return _progressWindow.Dispatcher.InvokeAsync(() => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes).Task;
        }
    }
}