using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Forms;
using Forms = System.Windows.Forms;
using WPF = System.Windows;

namespace DarkHub
{
    public partial class MainWindow : Window
    {
        private Type _currentPageType;
        private WindowState _previousWindowState;

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
            }
            catch (Exception ex)
            {
                WPF.MessageBox.Show($"Erro ao inicializar MainWindow: {ex.Message}\nStackTrace: {ex.StackTrace}", "Erro Crítico", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Error);
                Close();
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
                case "DllInjector":
                    btnDllInjector.Tag = "Active";
                    break;
                case "CrunchyrollAcc":
                    btnCrunchyrollAcc.Tag = "Active";
                    break;
            }

            var paginasOcultas = new List<string> { "SummX", "YoutubeVideoDownloader", "TextEditor", "MetaDataEditor", "ImageTextExtractor", "SystemMonitor" };

            if (paginasOcultas.Contains(pageName))
            {
                btnDllInjector.Visibility = Visibility.Collapsed;
                btnCrunchyrollAcc.Visibility = Visibility.Collapsed;
                btnBr.Visibility = Visibility.Collapsed;
                btnEua.Visibility = Visibility.Collapsed;
                btnDiscord.Visibility = Visibility.Collapsed;
            }
            else
            {
                btnDllInjector.Visibility = Visibility.Visible;
                btnCrunchyrollAcc.Visibility = Visibility.Visible;
                btnBr.Visibility = Visibility.Visible;
                btnEua.Visibility = Visibility.Visible;
                btnDiscord.Visibility = Visibility.Visible;
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