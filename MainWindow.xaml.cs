using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace DarkHub
{
    public partial class MainWindow : Window
    {
        private Type _currentPageType;

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar MainWindow: {ex.Message}\nStackTrace: {ex.StackTrace}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show("Erro ao tentar abrir o link: " + ex.Message);
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
                MessageBox.Show($"Erro ao navegar para página: {ex.Message}", "Erro de Navegação", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erro ao mudar para inglês: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erro ao mudar para português: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SetActiveButton(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
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
                btnDllInjector.Tag = null;
                btnCrunchyrollAcc.Tag = null;
                btnSystemMonitor.Tag = null;

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao navegar: {ex.Message}\nStackTrace: {ex.StackTrace}", "Erro de Navegação", MessageBoxButton.OK, MessageBoxImage.Error);
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

            var paginasOcultas = new List<string> { "YoutubeVideoDownloader", "TextEditor", "MetaDataEditor", "ImageTextExtractor", "SystemMonitor" };

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
    }
}