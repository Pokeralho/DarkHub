using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class ProgramUninstaller
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public ProgramUninstaller(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.UninstallingProgramTitle);
            _progressWindow.Owner = owner;
        }

        public async Task UninstallProgramAsync()
        {
            _button.IsEnabled = false;

            try
            {
                var installedPrograms = await GetInstalledProgramsAsync();
                if (installedPrograms.Count == 0)
                {
                    // Captura o owner na thread de UI
                    var noProgramsOwner = _progressWindow.Owner;
                    WindowState noProgramsOwnerState = WindowState.Normal;
                    bool noProgramsOwnerVisible = false;

                    await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                    {
                        // Capturar estado atual da janela owner
                        if (noProgramsOwner != null && noProgramsOwner.IsLoaded)
                        {
                            noProgramsOwnerState = noProgramsOwner.WindowState;
                            noProgramsOwnerVisible = noProgramsOwner.IsVisible;
                        }

                        MessageBox.Show(ResourceManagerHelper.Instance.NoProgramsFound,
                            ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                        // Restaurar estado da janela principal
                        if (noProgramsOwner != null && noProgramsOwner.IsLoaded)
                        {
                            if (noProgramsOwnerVisible)
                            {
                                noProgramsOwner.WindowState = noProgramsOwnerState;
                                noProgramsOwner.Activate();
                                noProgramsOwner.Focus();
                            }
                        }
                    }));
                    _button.IsEnabled = true;
                    return;
                }

                // Usando TaskCompletionSource para controlar a janela de seleção
                var selectionTcs = new TaskCompletionSource<InstalledProgram>();

                // Captura o owner na thread de UI
                var selectionOwner = _progressWindow.Owner;
                WindowState selectionOwnerState = WindowState.Normal;
                bool selectionOwnerVisible = false;

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    // Capturar estado atual da janela owner
                    if (selectionOwner != null && selectionOwner.IsLoaded)
                    {
                        selectionOwnerState = selectionOwner.WindowState;
                        selectionOwnerVisible = selectionOwner.IsVisible;
                    }

                    var selectionWindow = CreateProgramSelectionWindow(installedPrograms);

                    // Quando a janela for fechada
                    selectionWindow.Closed += (s, args) =>
                    {
                        if (selectionWindow.DialogResult == true && selectionWindow.Tag != null)
                        {
                            selectionTcs.SetResult((InstalledProgram)selectionWindow.Tag);
                        }
                        else
                        {
                            selectionTcs.SetResult(null);
                        }

                        // Restaurar estado da janela principal
                        if (selectionOwner != null && selectionOwner.IsLoaded)
                        {
                            if (selectionOwnerVisible)
                            {
                                selectionOwner.WindowState = selectionOwnerState;
                                selectionOwner.Activate();
                                selectionOwner.Focus();
                            }
                        }
                    };

                    selectionWindow.ShowDialog();
                }));

                var selectedProgram = await selectionTcs.Task;
                if (selectedProgram == null)
                {
                    _button.IsEnabled = true;
                    return;
                }

                string programName = selectedProgram.Name;
                string? uninstallString = selectedProgram.UninstallString;
                string? installLocation = selectedProgram.InstallLocation;

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.StartingUninstallation, programName));

                bool uninstallerExecuted = false;
                if (!string.IsNullOrEmpty(uninstallString))
                {
                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.RunningOfficialUninstaller);
                    uninstallerExecuted = await ExecuteUninstallerAsync(uninstallString, installLocation, _progressTextBox);
                    if (!uninstallerExecuted)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, "Uninstaller failed or not found. Proceeding with deep cleanup.");
                    }
                    else
                    {
                        WindowFactory.AppendProgress(_progressTextBox, "Uninstaller executed. Verifying removal...");
                        if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                        {
                            WindowFactory.AppendProgress(_progressTextBox, "Program files still detected. Forcing deep cleanup.");
                            uninstallerExecuted = false;
                        }
                    }
                }
                else
                {
                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.NoUninstallerFound);
                    WindowFactory.AppendProgress(_progressTextBox, "Proceeding with deep cleanup due to missing uninstaller.");
                }

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.ScanningLeftovers);
                var leftovers = await ScanForLeftoversAsync(programName, installLocation, _progressTextBox);
                if (leftovers.HasLeftovers || !uninstallerExecuted)
                {
                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.FoundLeftovers);
                    await CleanupLeftoversAsync(leftovers, _progressTextBox);
                }
                else
                {
                    WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.NoLeftoversFound);
                }

                await RemoveFromInstalledProgramsAsync(programName, _progressTextBox);

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.UninstallationCompleted);

                // Captura o owner na thread de UI
                var resultOwner = _progressWindow.Owner;

                // Guardar estado da janela principal
                WindowState resultOwnerState = WindowState.Normal;
                bool resultOwnerVisible = false;

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    // Capturar estado atual da janela owner
                    if (resultOwner != null && resultOwner.IsLoaded)
                    {
                        resultOwnerState = resultOwner.WindowState;
                        resultOwnerVisible = resultOwner.IsVisible;
                    }

                    MessageBox.Show(ResourceManagerHelper.Instance.ProgramUninstalledSuccess,
                        ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                    // Restaurar estado da janela principal
                    if (resultOwner != null && resultOwner.IsLoaded)
                    {
                        if (resultOwnerVisible)
                        {
                            resultOwner.WindowState = resultOwnerState;
                            resultOwner.Activate();
                            resultOwner.Focus();
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorUninstallingProgram, ex.Message));

                // Captura o owner na thread de UI para mensagem de erro
                var errorOwner = _progressWindow.Owner;
                WindowState errorOwnerState = WindowState.Normal;
                bool errorOwnerVisible = false;

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    // Capturar estado atual da janela owner
                    if (errorOwner != null && errorOwner.IsLoaded)
                    {
                        errorOwnerState = errorOwner.WindowState;
                        errorOwnerVisible = errorOwner.IsVisible;
                    }

                    MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorUninstallingProgram, ex.Message),
                        ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);

                    // Restaurar estado da janela principal
                    if (errorOwner != null && errorOwner.IsLoaded)
                    {
                        if (errorOwnerVisible)
                        {
                            errorOwner.WindowState = errorOwnerState;
                            errorOwner.Activate();
                            errorOwner.Focus();
                        }
                    }
                }));
            }
            finally
            {
                _button.IsEnabled = true;

                // Capturar o owner aqui também para garantir que a janela principal permaneça visível
                // ao fechar a janela de progresso
                var closeOwner = _progressWindow.Owner;
                WindowState closeOwnerState = WindowState.Normal;
                bool closeOwnerVisible = false;

                if (closeOwner != null && closeOwner.IsLoaded)
                {
                    closeOwnerState = closeOwner.WindowState;
                    closeOwnerVisible = closeOwner.IsVisible;
                }

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    _progressWindow.Close();

                    // Restaurar estado da janela principal após fechar a janela de progresso
                    if (closeOwner != null && closeOwner.IsLoaded && closeOwnerVisible)
                    {
                        closeOwner.WindowState = closeOwnerState;
                        closeOwner.Activate();
                        closeOwner.Focus();
                    }
                }));
            }
        }

        public class InstalledProgram
        {
            public string Name { get; set; }
            public string? UninstallString { get; set; }
            public string? InstallLocation { get; set; }
        }

        public class Leftovers
        {
            public List<string> Files { get; set; } = new List<string>();
            public List<string> RegistryKeys { get; set; } = new List<string>();
            public bool HasLeftovers => Files.Count > 0 || RegistryKeys.Count > 0;
        }

        private async Task<List<InstalledProgram>> GetInstalledProgramsAsync()
        {
            return await Task.Run(() =>
            {
                var programs = new List<InstalledProgram>();
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"))
                {
                    if (key != null)
                    {
                        foreach (var subKeyName in key.GetSubKeyNames())
                        {
                            using (var subKey = key.OpenSubKey(subKeyName))
                            {
                                var name = subKey?.GetValue("DisplayName")?.ToString();
                                if (!string.IsNullOrEmpty(name))
                                {
                                    programs.Add(new InstalledProgram
                                    {
                                        Name = name,
                                        UninstallString = subKey?.GetValue("UninstallString")?.ToString(),
                                        InstallLocation = subKey?.GetValue("InstallLocation")?.ToString()
                                    });
                                }
                            }
                        }
                    }
                }
                return programs.OrderBy(p => p.Name).ToList();
            });
        }

        private Window CreateProgramSelectionWindow(List<InstalledProgram> programs)
        {
            var window = WindowFactory.CreateWindow(
                title: ResourceManagerHelper.Instance.SelectProgramToUninstall,
                width: 400,
                height: 300,
                owner: _progressWindow.Owner,
                isModal: true,
                resizable: false
            );

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            window.Content = grid;

            var searchBox = new TextBox
            {
                Margin = new Thickness(10, 10, 10, 5),
                Background = WindowFactory.DefaultControlBackground,
                Foreground = WindowFactory.DefaultTextForeground,
                BorderBrush = WindowFactory.DefaultBorderBrush,
                BorderThickness = WindowFactory.DefaultBorderThickness,
                Padding = new Thickness(5),
                FontFamily = WindowFactory.DefaultFontFamily,
                FontSize = WindowFactory.DefaultFontSize,
                Text = "Pesquisar programas..."
            };
            searchBox.GotFocus += (s, e) => { if (searchBox.Text == "Pesquisar programas...") searchBox.Text = ""; };
            searchBox.LostFocus += (s, e) => { if (string.IsNullOrEmpty(searchBox.Text)) searchBox.Text = "Pesquisar programas..."; };
            Grid.SetRow(searchBox, 0);
            grid.Children.Add(searchBox);

            var listBox = WindowFactory.CreateStyledListBox(200);
            listBox.ItemsSource = programs;
            listBox.DisplayMemberPath = "Name";
            listBox.Margin = new Thickness(10, 0, 10, 10);
            Grid.SetRow(listBox, 1);
            grid.Children.Add(listBox);

            searchBox.TextChanged += (s, e) =>
            {
                if (searchBox.Text == "Pesquisar programas..." || string.IsNullOrEmpty(searchBox.Text))
                {
                    listBox.ItemsSource = programs;
                }
                else
                {
                    listBox.ItemsSource = programs.Where(p => p.Name.Contains(searchBox.Text, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            };

            var confirmButton = WindowFactory.CreateStyledButton("Desinstalar", 100, (s, e) =>
            {
                window.DialogResult = true;
                window.Close();
            });
            confirmButton.HorizontalAlignment = HorizontalAlignment.Right;
            confirmButton.Margin = new Thickness(0, 0, 10, 10);
            Grid.SetRow(confirmButton, 2);
            grid.Children.Add(confirmButton);

            window.Tag = null;
            listBox.SelectionChanged += (s, e) =>
            {
                if (listBox.SelectedItem != null)
                {
                    window.Tag = listBox.SelectedItem as InstalledProgram;
                }
            };

            return window;
        }

        private async Task<bool> ExecuteUninstallerAsync(string uninstallString, string? installLocation, TextBox progressTextBox)
        {
            try
            {
                WindowFactory.AppendProgress(progressTextBox, $"Raw uninstall string from registry: '{uninstallString}'");
                uninstallString = uninstallString.Trim();

                string fileName = uninstallString;
                string arguments = "";
                if (uninstallString.Contains(" "))
                {
                    if (uninstallString.StartsWith("\""))
                    {
                        int endQuote = uninstallString.IndexOf("\"", 1);
                        if (endQuote != -1)
                        {
                            fileName = uninstallString.Substring(1, endQuote - 1);
                            arguments = uninstallString.Substring(endQuote + 1).Trim();
                        }
                    }
                    else
                    {
                        fileName = uninstallString.Substring(0, uninstallString.IndexOf(" ")).Trim();
                        arguments = uninstallString.Substring(uninstallString.IndexOf(" ")).Trim();
                    }
                }

                WindowFactory.AppendProgress(progressTextBox, $"Parsed fileName: '{fileName}'");
                WindowFactory.AppendProgress(progressTextBox, $"Parsed arguments: '{arguments}'");

                if (!File.Exists(fileName))
                {
                    WindowFactory.AppendProgress(progressTextBox, $"Uninstaller file not found at: '{fileName}'");

                    if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                    {
                        WindowFactory.AppendProgress(progressTextBox, $"Checking InstallLocation: '{installLocation}'");
                        var possibleUninstaller = Path.Combine(installLocation, "tesseract-uninstall.exe");
                        if (!File.Exists(possibleUninstaller))
                        {
                            possibleUninstaller = Path.Combine(installLocation, "uninstall.exe");
                        }

                        if (File.Exists(possibleUninstaller))
                        {
                            fileName = possibleUninstaller;
                            arguments = "";
                            WindowFactory.AppendProgress(progressTextBox, $"Found alternative uninstaller: '{fileName}'");
                        }
                        else
                        {
                            WindowFactory.AppendProgress(progressTextBox, "No alternative uninstaller found in InstallLocation.");
                            return false;
                        }
                    }
                    else
                    {
                        WindowFactory.AppendProgress(progressTextBox, "InstallLocation not provided or invalid.");
                        return false;
                    }
                }

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        UseShellExecute = true,
                        CreateNoWindow = false,
                        Verb = "runas"
                    }
                };

                WindowFactory.AppendProgress(progressTextBox, $"Starting uninstaller: '{fileName} {arguments}'");
                process.Start();
                await process.WaitForExitAsync();

                WindowFactory.AppendProgress(progressTextBox, $"Uninstaller finished with exit code: {process.ExitCode}");
                bool initialSuccess = process.ExitCode == 0 || process.ExitCode == 3010;

                if (initialSuccess && !string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    WindowFactory.AppendProgress(progressTextBox, $"Installation directory still exists: '{installLocation}'");
                    return false;
                }

                return initialSuccess;
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(progressTextBox, $"Uninstaller error: {ex.Message}");
                if (ex.Message.Contains("access is denied"))
                {
                    WindowFactory.AppendProgress(progressTextBox, "Please run the application as administrator.");
                }
                return false;
            }
        }

        private async Task<Leftovers> ScanForLeftoversAsync(string programName, string? installLocation, TextBox progressTextBox)
        {
            var leftovers = new Leftovers();
            await Task.Run(() =>
            {
                WindowFactory.AppendProgress(progressTextBox, $"Scanning for leftovers of '{programName}'...");

                string[] commonPaths = {
                    @"C:\Program Files",
                    @"C:\Program Files (x86)",
                    @"C:\ProgramData",
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFiles),
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonProgramFilesX86)
                };

                if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
                {
                    WindowFactory.AppendProgress(progressTextBox, $"Explicitly scanning InstallLocation: '{installLocation}'");
                    try
                    {
                        var dirs = Directory.GetDirectories(installLocation, $"*{programName}*", SearchOption.AllDirectories);
                        leftovers.Files.AddRange(dirs);
                        WindowFactory.AppendProgress(progressTextBox, $"Found {dirs.Length} directories in InstallLocation");

                        var files = Directory.GetFiles(installLocation, "*.*", SearchOption.AllDirectories)
                            .Where(f => f.Contains(programName, StringComparison.OrdinalIgnoreCase) ||
                                       Path.GetExtension(f).ToLower() is ".log" or ".tmp");
                        leftovers.Files.AddRange(files);
                        WindowFactory.AppendProgress(progressTextBox, $"Found {files.Count()} files in InstallLocation");
                    }
                    catch (Exception ex)
                    {
                        WindowFactory.AppendProgress(progressTextBox, $"Error scanning InstallLocation: {ex.Message}");
                    }
                }

                foreach (var path in commonPaths)
                {
                    if (Directory.Exists(path))
                    {
                        try
                        {
                            var dirs = Directory.GetDirectories(path, $"*{programName}*", SearchOption.AllDirectories);
                            leftovers.Files.AddRange(dirs);
                            WindowFactory.AppendProgress(progressTextBox, $"Found {dirs.Length} directories in '{path}'");

                            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories)
                                .Where(f => f.Contains(programName, StringComparison.OrdinalIgnoreCase) ||
                                           Path.GetExtension(f).ToLower() is ".log" or ".tmp");
                            leftovers.Files.AddRange(files);
                            WindowFactory.AppendProgress(progressTextBox, $"Found {files.Count()} files in '{path}'");
                        }
                        catch (Exception ex)
                        {
                            WindowFactory.AppendProgress(progressTextBox, $"Error scanning '{path}': {ex.Message}");
                        }
                    }
                }

                string[] regPaths = { @"SOFTWARE", @"SOFTWARE\Wow6432Node" };
                foreach (var regPath in regPaths)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(regPath))
                    {
                        if (key != null)
                        {
                            try
                            {
                                var matchingKeys = key.GetSubKeyNames()
                                    .Where(k => k.Contains(programName, StringComparison.OrdinalIgnoreCase));
                                leftovers.RegistryKeys.AddRange(matchingKeys.Select(k => $@"HKEY_LOCAL_MACHINE\{regPath}\{k}"));
                                WindowFactory.AppendProgress(progressTextBox, $"Found {matchingKeys.Count()} registry keys in '{regPath}'");
                            }
                            catch (Exception ex)
                            {
                                WindowFactory.AppendProgress(progressTextBox, $"Error scanning registry '{regPath}': {ex.Message}");
                            }
                        }
                    }
                }
            });
            return leftovers;
        }

        private async Task RemoveFromInstalledProgramsAsync(string programName, TextBox progressTextBox)
        {
            await Task.Run(() =>
            {
                WindowFactory.AppendProgress(progressTextBox, $"Removing {programName} from installed programs list...");
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", true))
                    {
                        if (key != null)
                        {
                            foreach (var subKeyName in key.GetSubKeyNames())
                            {
                                using (var subKey = key.OpenSubKey(subKeyName, true))
                                {
                                    if (subKey != null)
                                    {
                                        var name = subKey.GetValue("DisplayName")?.ToString();
                                        if (!string.IsNullOrEmpty(name) && name.Contains(programName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            key.DeleteSubKeyTree(subKeyName);
                                            WindowFactory.AppendProgress(progressTextBox, $"Removed registry entry: HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{subKeyName}");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall", true))
                    {
                        if (key != null)
                        {
                            foreach (var subKeyName in key.GetSubKeyNames())
                            {
                                using (var subKey = key.OpenSubKey(subKeyName, true))
                                {
                                    if (subKey != null)
                                    {
                                        var name = subKey.GetValue("DisplayName")?.ToString();
                                        if (!string.IsNullOrEmpty(name) && name.Contains(programName, StringComparison.OrdinalIgnoreCase))
                                        {
                                            key.DeleteSubKeyTree(subKeyName);
                                            WindowFactory.AppendProgress(progressTextBox, $"Removed registry entry: HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{subKeyName}");
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WindowFactory.AppendProgress(progressTextBox, $"Error removing from installed programs: {ex.Message}");
                }
            });
        }

        private async Task CleanupLeftoversAsync(Leftovers leftovers, TextBox progressTextBox)
        {
            await Task.Run(() =>
            {
                foreach (var file in leftovers.Files.Where(f => File.Exists(f)))
                {
                    try
                    {
                        File.Delete(file);
                        WindowFactory.AppendProgress(progressTextBox, $"Removed file: '{file}'");
                    }
                    catch (Exception ex)
                    {
                        WindowFactory.AppendProgress(progressTextBox, $"Failed to remove file '{file}': {ex.Message}");
                    }
                }

                foreach (var dir in leftovers.Files.Where(f => Directory.Exists(f)))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        WindowFactory.AppendProgress(progressTextBox, $"Removed directory and contents: '{dir}'");
                    }
                    catch (Exception ex)
                    {
                        WindowFactory.AppendProgress(progressTextBox, $"Failed to remove directory '{dir}': {ex.Message}");
                        try
                        {
                            foreach (var subFile in Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories))
                            {
                                try
                                {
                                    File.Delete(subFile);
                                    WindowFactory.AppendProgress(progressTextBox, $"Forced removal of file: '{subFile}'");
                                }
                                catch (Exception subEx)
                                {
                                    WindowFactory.AppendProgress(progressTextBox, $"Failed to force remove '{subFile}': {subEx.Message}");
                                }
                            }
                            Directory.Delete(dir, true);
                            WindowFactory.AppendProgress(progressTextBox, $"Successfully forced removal of directory: '{dir}'");
                        }
                        catch (Exception subEx)
                        {
                            WindowFactory.AppendProgress(progressTextBox, $"Final failure to remove '{dir}': {subEx.Message}");
                        }
                    }
                }

                foreach (var keyPath in leftovers.RegistryKeys)
                {
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE", true))
                        {
                            if (key != null && keyPath.StartsWith(@"HKEY_LOCAL_MACHINE\SOFTWARE\"))
                            {
                                key.DeleteSubKeyTree(keyPath.Replace(@"HKEY_LOCAL_MACHINE\SOFTWARE\", ""), false);
                                WindowFactory.AppendProgress(progressTextBox, $"Removed registry key: '{keyPath}'");
                            }
                        }
                        using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node", true))
                        {
                            if (key != null && keyPath.StartsWith(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\"))
                            {
                                key.DeleteSubKeyTree(keyPath.Replace(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\", ""), false);
                                WindowFactory.AppendProgress(progressTextBox, $"Removed registry key: '{keyPath}'");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        WindowFactory.AppendProgress(progressTextBox, $"Failed to remove registry key '{keyPath}': {ex.Message}");
                    }
                }
            });
        }
    }
}