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
        private Type _currentPageType;
        private WindowState _previousWindowState;
        private static readonly string CertificatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "darkhub.pfx");
        private static readonly string PasswordFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "certificate_password.txt");

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
            try
            {
                string cpuZPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "CPU-Z.exe");
                if (File.Exists(cpuZPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = cpuZPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("CPU-Z executable not found in assets folder!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening CPU-Z: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenGpuZ_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string gpuZPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "GPU-Z.exe");
                if (File.Exists(gpuZPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = gpuZPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("GPU-Z executable not found in assets folder!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening GPU-Z: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenHwinfo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string hwinfoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "HWiNFO64.exe");
                if (File.Exists(hwinfoPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = hwinfoPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("HWiNFO executable not found in assets folder!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening HWiNFO: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenDDU_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string hwinfoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "DDU.exe");
                if (File.Exists(hwinfoPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = hwinfoPath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("DDU executable not found in assets folder!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening DDU: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ActivateCertificateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!File.Exists(CertificatePath))
                {
                    WindowFactory.ShowMessage(this, "O arquivo de certificado 'darkhub.pfx' não foi encontrado na pasta assets.",
                        "Erro", MessageBoxImage.Error);
                    return;
                }

                if (!File.Exists(PasswordFilePath))
                {
                    WindowFactory.ShowMessage(this, "O arquivo 'certificate_password.txt' não foi encontrado na pasta assets.",
                        "Erro", MessageBoxImage.Error);
                    return;
                }

                string password = ReadEncryptedPassword();
                if (string.IsNullOrEmpty(password))
                {
                    WindowFactory.ShowMessage(this, "O arquivo 'certificate_password.txt' está vazio. Forneça a senha do certificado.",
                        "Erro", MessageBoxImage.Error);
                    return;
                }

                X509Certificate2 certificate = new X509Certificate2(CertificatePath, password, X509KeyStorageFlags.UserKeySet);

                InstallCertificateToRootStore(certificate);

                WindowFactory.ShowMessage(this, "Certificado ativado e instalado no repositório Root com sucesso!\n" +
                    $"Subject: {certificate.Subject}\n" +
                    $"Validade: {certificate.NotAfter}",
                    "Sucesso", MessageBoxImage.Information);
            }
            catch (System.Security.Cryptography.CryptographicException ex)
            {
                WindowFactory.ShowMessage(this, $"Erro ao carregar o certificado. Verifique a senha ou o arquivo.\nErro: {ex.Message}",
                    "Erro", MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                WindowFactory.ShowMessage(this, $"Erro ao ativar o certificado: {ex.Message}", "Erro", MessageBoxImage.Error);
            }
        }

        private void InstallCertificateToRootStore(X509Certificate2 certificate)
        {
            if (!IsRunningAsAdministrator())
            {
                WindowFactory.ShowMessage(this, "Permissões de administrador são necessárias para instalar um certificado no repositório Root.\n" +
                    "Por favor, execute o programa como administrador.",
                    "Erro", MessageBoxImage.Error);
                throw new UnauthorizedAccessException("Permissões de administrador necessárias.");
            }

            using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);

                var existingCert = store.Certificates.Find(X509FindType.FindByThumbprint, certificate.Thumbprint, false);
                if (existingCert.Count == 0)
                {
                    store.Add(certificate);
                    WindowFactory.ShowMessage(this, "Certificado instalado no repositório de Autoridades Raiz Confiáveis.",
                        "Sucesso", MessageBoxImage.Information);
                }
                else
                {
                    WindowFactory.ShowMessage(this, "O certificado já está instalado no repositório Root.",
                        "Aviso", MessageBoxImage.Information);
                }

                store.Close();
            }
        }

        private string ReadEncryptedPassword()
        {
            try
            {
                byte[] encryptedData = File.ReadAllBytes(PasswordFilePath);
                byte[] decryptedData = System.Security.Cryptography.ProtectedData.Unprotect(
                    encryptedData,
                    null,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);
                string password = System.Text.Encoding.UTF8.GetString(decryptedData);
                return password;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private bool IsRunningAsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
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
                    Page newPage = (Page)Activator.CreateInstance(_currentPageType);
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
                    Page newPage = (Page)Activator.CreateInstance(_currentPageType);
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
                    Arguments = $"-Command \"{psCommand}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.RestorePointCreated, ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                else
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.RestorePointNotCreated, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            System.Windows.Controls.Button clickedButton = sender as System.Windows.Controls.Button;
            if (clickedButton == null)
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
                //btnGameLauncher.Tag = null;
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
                //else if (clickedButton == btnGameLauncher)
                //{
                //    GameLauncher gameLauncher = new GameLauncher(this);
                //    gameLauncher.Show();
                //}
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
            ScrollViewer scrollViewer = sender as ScrollViewer;
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