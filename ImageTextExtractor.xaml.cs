using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Tesseract;

namespace DarkHub
{
    public partial class ImageTextExtractor : System.Windows.Controls.Page
    {
        private string? selectedImagePath;

        public ImageTextExtractor()
        {
            try
            {
                InitializeComponent();
                Debug.WriteLine("ImageTextExtractor inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao inicializar ImageTextExtractor: {ex.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao inicializar ImageTextExtractor: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void SelectImage_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("SelectImage_Click iniciado.");
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Arquivos de Imagem (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Todos os Arquivos (*.*)|*.*",
                    Title = "Selecionar Imagem para Extrair Texto"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    selectedImagePath = openFileDialog.FileName;
                    await Dispatcher.InvokeAsync(() =>
                        extractedTextBox.Text = $"Imagem selecionada: {selectedImagePath}\nClique em 'Extrair Texto' para processar.");
                    Debug.WriteLine($"Imagem selecionada: {selectedImagePath}");
                }
                else
                {
                    Debug.WriteLine("Nenhuma imagem selecionada pelo usuário.");
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                    extractedTextBox.Text = $"Erro ao selecionar imagem: {ex.Message}");
                Debug.WriteLine($"Erro em SelectImage_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void ExtractText_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("ExtractText_Click iniciado.");
                if (string.IsNullOrEmpty(selectedImagePath) || !File.Exists(selectedImagePath))
                {
                    await Dispatcher.InvokeAsync(() =>
                        extractedTextBox.Text = "Por favor, selecione uma imagem válida primeiro.");
                    Debug.WriteLine("Nenhuma imagem válida selecionada para extração.");
                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                    extractedTextBox.Text = "Extraindo texto da imagem...");

                string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                if (!Directory.Exists(tessDataPath))
                {
                    await Dispatcher.InvokeAsync(() =>
                        extractedTextBox.Text = "Erro: A pasta 'tessdata' não foi encontrada. Certifique-se de que ela existe no diretório do programa.");
                    Debug.WriteLine($"Pasta tessdata não encontrada: {tessDataPath}");
                    return;
                }

                await Task.Run(async () =>
                {
                    try
                    {
                        Debug.WriteLine($"Iniciando TesseractEngine com tessdata em: {tessDataPath}");
                        using var engine = new TesseractEngine(tessDataPath, "eng+por", EngineMode.Default);
                        using var img = Pix.LoadFromFile(selectedImagePath);
                        using var page = engine.Process(img);
                        string extractedText = page.GetText();

                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (string.IsNullOrWhiteSpace(extractedText))
                            {
                                extractedTextBox.Text = "Nenhum texto foi detectado na imagem.";
                            }
                            else
                            {
                                extractedTextBox.Text = $"Texto extraído:\n\n{extractedText}";
                            }
                        });
                        Debug.WriteLine("Extração de texto concluída com sucesso.");
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.InvokeAsync(() =>
                            extractedTextBox.Text = $"Erro ao extrair texto: {ex.Message}\nCertifique-se de que a pasta 'tessdata' contém os arquivos 'eng.traineddata', 'por.traineddata' e 'osd.traineddata'.");
                        Debug.WriteLine($"Erro interno na extração de texto: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                    extractedTextBox.Text = $"Erro geral ao extrair texto: {ex.Message}");
                Debug.WriteLine($"Erro em ExtractText_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }
    }
}