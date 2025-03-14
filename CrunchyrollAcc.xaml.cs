using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub
{
    public partial class CrunchyrollAcc : Page
    {
        public CrunchyrollAcc()
        {
            InitializeComponent();
            InitializeWebViewAsync();
        }

        private async void InitializeWebViewAsync()
        {
            try
            {
                string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DarkHub");
                if (!Directory.Exists(userDataFolder))
                {
                    Directory.CreateDirectory(userDataFolder);
                }

                var environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

                await GoogleSheetView.EnsureCoreWebView2Async(environment);

                GoogleSheetView.Source = new Uri("https://docs.google.com/spreadsheets/d/1jAhc4FiczoKC2TNygBuA9yc3Rep9w5ewUEiIOU3mRbw/edit?usp=sharing");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Erro ao inicializar o WebView2: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}