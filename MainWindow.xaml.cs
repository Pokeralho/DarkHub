using DarkHub.UI;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Forms = System.Windows.Forms;

using WPF = System.Windows;

namespace DarkHub
{
    public partial class MainWindow : Window
    {
        private Type? _currentPageType;
        private WindowState _previousWindowState = WindowState.Normal;
        public MainWindow()
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("pt-BR");

                InitializeComponent();

                if (MainFrame == null)
                {
                    throw new InvalidOperationException("MainFrame não foi inicializado corretamente no XAML.");
                }

                NavigateToPageAsync(new Optimizer()).ConfigureAwait(false);
                _currentPageType = typeof(Optimizer);

                MinWidth = 800;
                MinHeight = 600;

                Closing += MainWindow_Closing;
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"Erro ao inicializar MainWindow: {ex.Message}\nStackTrace: {ex.StackTrace}", "Erro Crítico", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
                Close();
            }
        }

        private void OpenCpuZ_Click(object sender, RoutedEventArgs e)
        {
            OpenAssetExecutable("CPU-Z.exe", "CPU-Z");
        }

        private void OpenGpuZ_Click(object sender, RoutedEventArgs e)
        {
            OpenAssetExecutable("GPU-Z.exe", "GPU-Z");
        }

        private void OpenHwinfo_Click(object sender, RoutedEventArgs e)
        {
            OpenAssetExecutable("HWiNFO64.exe", "HWiNFO");
        }

        private void OpenDDU_Click(object sender, RoutedEventArgs e)
        {
            OpenAssetExecutable("DDU.exe", "DDU");
        }

        private void OpenAssetExecutable(string fileName, string displayName)
        {
            try
            {
                string executablePath = Path.Combine(AppContext.BaseDirectory, "assets", fileName);
                if (File.Exists(executablePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = executablePath,
                        WorkingDirectory = Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory,
                        UseShellExecute = true
                    });
                }
                else
                {
                    WindowFactory.ShowMessage(this, $"{displayName} não encontrado em assets. Execute scripts\\Restore-LocalAssets.ps1 ou copie {fileName} para a pasta assets.",
                        "Ferramenta não encontrada", MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                WindowFactory.ShowMessage(this, $"Erro ao abrir {displayName}: {ex.Message}", "Erro", MessageBoxImage.Error);
            }
        }

        private void ActivateCertificateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? executablePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
                {
                    WindowFactory.ShowMessage(this, "Não foi possível localizar o executável atual para validar a assinatura.",
                        "Assinatura digital", MessageBoxImage.Warning);
                    return;
                }

                using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(executablePath));
                bool isValid = certificate.Verify();

                WindowFactory.ShowMessage(this, (isValid ? "Assinatura digital válida.\n" : "Assinatura encontrada, mas a cadeia não foi validada.\n") +
                    $"Subject: {certificate.Subject}\n" +
                    $"Validade: {certificate.NotAfter:d}\n" +
                    $"Thumbprint: {certificate.Thumbprint}",
                    "Assinatura digital", isValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                WindowFactory.ShowMessage(this, $"O executável atual não possui uma assinatura Authenticode válida.\nErro: {ex.Message}",
                    "Assinatura digital", MessageBoxImage.Warning);
            }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Debug.WriteLine("MainWindow está sendo fechada.");
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao fechar MainWindow: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Environment.Exit(0);
            }
        }

        private void DiscordServer(object sender, RoutedEventArgs e)
        {
            string inviteLink = "https://discord.gg/x7F25Xz8Qk";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = inviteLink,
                    UseShellExecute = true
                });
            }
            catch (System.Exception ex)
            {
                WPF.MessageBox.Show("Erro ao tentar abrir o link: " + ex.Message);
            }
        }

        private async Task NavigateToPageAsync(Page page)
        {
            try
            {
                if (MainFrame != null)
                {
                    await Task.Run(() => MainFrame.Dispatcher.Invoke(() => MainFrame.Navigate(page)));
                    _currentPageType = page.GetType();
                }
                else
                {
                    throw new InvalidOperationException("MainFrame é nulo.");
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"Erro ao navegar para página: {ex.Message}", "Erro de Navegação", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
            }
        }

        private void ChangeToEnglish(object sender, RoutedEventArgs e)
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

                ResourceManagerHelper.Instance.NotifyCultureChanged();

                if (_currentPageType != null)
                {
                    if (Activator.CreateInstance(_currentPageType) is Page newPage)
                        NavigateToPageAsync(newPage).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"Erro ao mudar para inglês: {ex.Message}", "Erro", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
            }
        }

        private void ChangeToPortuguese(object sender, RoutedEventArgs e)
        {
            try
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("pt-BR");

                ResourceManagerHelper.Instance.NotifyCultureChanged();

                if (_currentPageType != null)
                {
                    if (Activator.CreateInstance(_currentPageType) is Page newPage)
                        NavigateToPageAsync(newPage).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"Erro ao mudar para português: {ex.Message}", "Erro", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
            }
        }

        public static async Task<bool> CreateRestorePointAsync(string description = "Ponto de Restauração - DarkHub")
        {
            try
            {
                if (!IsRunningAsAdmin())
                {
                    MessageBox.Show("Este recurso requer privilégios de administrador.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }

                string psCommand = $@"
                    Checkpoint-Computer -Description '{description}' -RestorePointType 'MODIFY_SETTINGS' -ErrorAction Stop;
                ";

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                processStartInfo.ArgumentList.Add("-NoProfile");
                processStartInfo.ArgumentList.Add("-ExecutionPolicy");
                processStartInfo.ArgumentList.Add("Bypass");
                processStartInfo.ArgumentList.Add("-Command");
                processStartInfo.ArgumentList.Add(psCommand);

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                await outputTask;

                if (process.ExitCode == 0)
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.RestorePointCreated, ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    string error = await errorTask;
                    MessageBox.Show($"{ResourceManagerHelper.Instance.RestorePointNotCreated}\n{error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"^{ResourceManagerHelper.Instance.RestorePointNotCreated} {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private async void CreateRestorePointButton_Click(object sender, RoutedEventArgs e)
        {
            await CreateRestorePointAsync();
        }

        private static bool IsRunningAsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private async void SetActiveButton(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button clickedButton)
            {
                return;
            }

            try
            {
                btnOptimizer.Tag = null;
                btnClicker.Tag = null;
                btnFileConverter.Tag = null;
                btnMetaDataEditor.Tag = null;
                btnTextEditor.Tag = null;
                btnExtrairTexto.Tag = null;
                btnYTDownloader.Tag = null;
                btnDllInjector.Tag = null;
                btnCrunchyrollAcc.Tag = null;
                btnSystemMonitor.Tag = null;
                btnSummX.Tag = null;
                btnPassMng.Tag = null;
                btnAdvancedSec.Tag = null;
                clickedButton.Tag = "Active";

                if (clickedButton == btnOptimizer)
                    await NavigateToPageAsync(new Optimizer());
                else if (clickedButton == btnClicker)
                    await NavigateToPageAsync(new AutoClicker());
                else if (clickedButton == btnFileConverter)
                    await NavigateToPageAsync(new FileConverter());
                else if (clickedButton == btnMetaDataEditor)
                    await NavigateToPageAsync(new MetaDataEditor());
                else if (clickedButton == btnTextEditor)
                    await NavigateToPageAsync(new TextEditor());
                else if (clickedButton == btnExtrairTexto)
                    await NavigateToPageAsync(new ImageTextExtractor());
                else if (clickedButton == btnYTDownloader)
                    await NavigateToPageAsync(new YoutubeVideoDownloader());
                else if (clickedButton == btnDllInjector)
                    await NavigateToPageAsync(new DllInjector());
                else if (clickedButton == btnCrunchyrollAcc)
                    await NavigateToPageAsync(new CrunchyrollAcc());
                else if (clickedButton == btnSystemMonitor)
                    await NavigateToPageAsync(new SystemMonitor());
                else if (clickedButton == btnSummX)
                    await NavigateToPageAsync(new SummX());
                else if (clickedButton == btnPassMng)
                    await NavigateToPageAsync(new PasswordManager());
                else if (clickedButton == btnAdvancedSec)
                    await NavigateToPageAsync(new AdvancedSecurity());
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"Erro ao navegar: {ex.Message}\nStackTrace: {ex.StackTrace}", "Erro de Navegação", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao mover janela: {ex.Message}");
            }
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            try
            {
                WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao minimizar janela: {ex.Message}");
            }
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao fechar janela: {ex.Message}");
            }
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            string pageName = e.Content.GetType().Name;

            btnOptimizer.Tag = null;
            btnClicker.Tag = null;
            btnFileConverter.Tag = null;
            btnMetaDataEditor.Tag = null;
            btnTextEditor.Tag = null;
            btnExtrairTexto.Tag = null;
            btnYTDownloader.Tag = null;
            btnDllInjector.Tag = null;
            btnCrunchyrollAcc.Tag = null;
            btnSystemMonitor.Tag = null;
            btnSummX.Tag = null;
            btnPassMng.Tag = null;
            btnAdvancedSec.Tag = null;
            switch (pageName)
            {
                case "Optimizer":
                    btnOptimizer.Tag = "Active";
                    break;

                case "AutoClicker":
                    btnClicker.Tag = "Active";
                    break;

                case "FileConverter":
                    btnFileConverter.Tag = "Active";
                    break;

                case "MetaDataEditor":
                    btnMetaDataEditor.Tag = "Active";
                    break;

                case "TextEditor":
                    btnTextEditor.Tag = "Active";
                    break;

                case "ImageTextExtractor":
                    btnExtrairTexto.Tag = "Active";
                    break;

                case "YoutubeVideoDownloader":
                    btnYTDownloader.Tag = "Active";
                    break;

                case "SystemMonitor":
                    btnSystemMonitor.Tag = "Active";
                    break;

                case "SummX":
                    btnSummX.Tag = "Active";
                    break;

                case "PasswordManager":
                    btnPassMng.Tag = "Active";
                    break;

                case "DllInjector":
                    btnDllInjector.Tag = "Active";
                    break;

                case "CrunchyrollAcc":
                    btnCrunchyrollAcc.Tag = "Active";
                    break;

                case "AdvancedSecurity":
                    btnAdvancedSec.Tag = "Active";
                    break;
            }

            var paginasOcultas = new List<string> { "AdvancedSecurity", "PasswordManager", "SummX", "YoutubeVideoDownloader", "TextEditor", "MetaDataEditor", "ImageTextExtractor", "SystemMonitor" };

            if (paginasOcultas.Contains(pageName))
            {
                btnDllInjector.Visibility = Visibility.Collapsed;
                btnCrunchyrollAcc.Visibility = Visibility.Collapsed;
                btnBr.Visibility = Visibility.Collapsed;
                btnEua.Visibility = Visibility.Collapsed;
                btnDiscord.Visibility = Visibility.Collapsed;
                btnPfx.Visibility = Visibility.Collapsed;
                btnBackup.Visibility = Visibility.Collapsed;
                btnCpuZ.Visibility = Visibility.Collapsed;
                btnHwinfo.Visibility = Visibility.Collapsed;
                btnGpuZ.Visibility = Visibility.Collapsed;
                btnDDU.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnDllInjector.Visibility = Visibility.Visible;
                btnCrunchyrollAcc.Visibility = Visibility.Visible;
                btnBr.Visibility = Visibility.Visible;
                btnEua.Visibility = Visibility.Visible;
                btnDiscord.Visibility = Visibility.Visible;
                btnPfx.Visibility = Visibility.Visible;
                btnBackup.Visibility = Visibility.Visible;
                btnCpuZ.Visibility = Visibility.Visible;
                btnHwinfo.Visibility = Visibility.Visible;
                btnGpuZ.Visibility = Visibility.Visible;
                btnDDU.Visibility = Visibility.Visible;
            }
        }

        private void MenuScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer? scrollViewer = sender as ScrollViewer;
            Border scrollIndicator = ScrollIndicator;

            if (scrollViewer != null && scrollIndicator != null)
            {
                bool hasMoreContentBelow = scrollViewer.VerticalOffset + scrollViewer.ViewportHeight < scrollViewer.ExtentHeight;
                scrollIndicator.Visibility = hasMoreContentBelow ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WindowState == WindowState.Normal)
                {
                    _previousWindowState = WindowState;
                    WindowState = WindowState.Maximized;
                    var screen = Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(this).Handle);
                    this.MaxHeight = screen.WorkingArea.Height;
                    this.MaxWidth = screen.WorkingArea.Width;
                }
                else
                {
                    WindowState = WindowState.Normal;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao maximizar/restaurar janela: {ex.Message}");
            }
        }

        private void RestoreWindow(object sender, RoutedEventArgs e)
        {
            try
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = _previousWindowState;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao restaurar janela: {ex.Message}");
            }
        }
    }
}
