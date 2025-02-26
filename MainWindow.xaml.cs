using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace DarkHub
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();

                if (MainFrame == null)
                {
                    throw new InvalidOperationException("MainFrame não foi inicializado corretamente no XAML.");
                }

                NavigateToPageAsync(new Optimizer()).ConfigureAwait(false);
                Debug.WriteLine("MainWindow inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar MainWindow: {ex.Message}\nStackTrace: {ex.StackTrace}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao inicializar MainWindow: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Close();
            }
        }

        private async Task NavigateToPageAsync(Page page)
        {
            try
            {
                if (MainFrame != null)
                {
                    await Task.Run(() => MainFrame.Dispatcher.Invoke(() => MainFrame.Navigate(page)));
                    Debug.WriteLine($"Navegado para {page.GetType().Name}");
                }
                else
                {
                    throw new InvalidOperationException("MainFrame é nulo.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao navegar para página: {ex.Message}", "Erro de Navegação", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao navegar para {page?.GetType().Name}: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void SetActiveButton(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;
            if (clickedButton == null)
            {
                Debug.WriteLine("Botão clicado é nulo, evento ignorado.");
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao navegar: {ex.Message}\nStackTrace: {ex.StackTrace}", "Erro de Navegação", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro no SetActiveButton para {clickedButton.Name}: {ex.Message}\nStackTrace: {ex.StackTrace}");
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
                Debug.WriteLine("Janela fechada com sucesso.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao fechar janela: {ex.Message}");
            }
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            Debug.WriteLine($"Navegação concluída para: {e.Uri?.ToString() ?? e.Content?.GetType().Name}");
        }
    }
}