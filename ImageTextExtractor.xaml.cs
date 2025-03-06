using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Tesseract;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace DarkHub
{
    public partial class ImageTextExtractor : System.Windows.Controls.Page
    {
        private string? selectedImagePath;
        private readonly Dictionary<string, string> languageMap = new();
        private List<string> availableLanguages = new();

        public ImageTextExtractor()
        {
            try
            {
                InitializeComponent();
                LoadAvailableLanguages();
                Debug.WriteLine("ImageTextExtractor inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorInitializingImageTextExtractor, ex.Message),
                                ResourceManagerHelper.Instance.CriticalErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao inicializar: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void LoadAvailableLanguages()
        {
            try
            {
                Debug.WriteLine("Carregando idiomas disponíveis...");
                string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

                if (!Directory.Exists(tessDataPath))
                {
                    Debug.WriteLine("Pasta tessdata não encontrada. Criando...");
                    Directory.CreateDirectory(tessDataPath);
                    MessageBox.Show(ResourceManagerHelper.Instance.TessdataFolderCreated,
                                    ResourceManagerHelper.Instance.FolderCreatedTitle, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                languageMap.Clear();
                availableLanguages.Clear();

                var essentialLanguages = new Dictionary<string, string>
                {
                    {"por", "Português"},
                    {"eng", "Inglês"}
                };

                foreach (var lang in essentialLanguages)
                {
                    if (File.Exists(Path.Combine(tessDataPath, $"{lang.Key}.traineddata")))
                    {
                        languageMap[lang.Key] = lang.Value;
                        availableLanguages.Add(lang.Key);
                        Debug.WriteLine($"Idioma carregado: {lang.Key} - {lang.Value}");
                    }
                }

                if (availableLanguages.Count == 0)
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.NoTrainedDataFilesFound,
                                    ResourceManagerHelper.Instance.FilesMissingTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                Debug.WriteLine($"Total de idiomas carregados: {availableLanguages.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar idiomas: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorLoadingLanguages, ex.Message),
                                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Bitmap PreProcessImage(string imagePath)
        {
            try
            {
                Debug.WriteLine("Iniciando pré-processamento da imagem...");
                if (!File.Exists(imagePath))
                {
                    throw new FileNotFoundException("Imagem não encontrada.", imagePath);
                }

                using var originalImage = new Bitmap(imagePath);
                int width = originalImage.Width;
                int height = originalImage.Height;
                const int minDimension = 1200;
                const int maxDimension = 4000;

                if (width < minDimension || height < minDimension)
                {
                    float scale = Math.Max((float)minDimension / width, (float)minDimension / height);
                    width = (int)(width * scale);
                    height = (int)(height * scale);
                }
                else if (width > maxDimension || height > maxDimension)
                {
                    float scale = Math.Min((float)maxDimension / width, (float)maxDimension / height);
                    width = (int)(width * scale);
                    height = (int)(height * scale);
                }

                width = Math.Max(1, Math.Min(width, maxDimension));
                height = Math.Max(1, Math.Min(height, maxDimension));

                using var processedImage = new Bitmap(width, height);
                using (var g = Graphics.FromImage(processedImage))
                {
                    g.Clear(Color.White);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(originalImage, 0, 0, width, height);
                }

                using var grayscaleImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(grayscaleImage))
                {
                    g.DrawImage(processedImage, 0, 0);
                }

                var imageData = grayscaleImage.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

                int bytes = Math.Abs(imageData.Stride) * height;
                byte[] rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(imageData.Scan0, rgbValues, 0, bytes);

                for (int i = 0; i < rgbValues.Length - 2; i += 3)
                {
                    byte gray = (byte)((rgbValues[i] + rgbValues[i + 1] + rgbValues[i + 2]) / 3);
                    byte value = gray < 128 ? (byte)0 : (byte)255;
                    rgbValues[i] = rgbValues[i + 1] = rgbValues[i + 2] = value;
                }

                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, imageData.Scan0, bytes);
                grayscaleImage.UnlockBits(imageData);

                return (Bitmap)grayscaleImage.Clone();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro no pré-processamento: {ex.Message}");
                throw;
            }
        }

        private async void ExtractText_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("ExtractText_Click iniciado.");
                await Dispatcher.InvokeAsync(() => extractedTextBox.Text = ResourceManagerHelper.Instance.StartingProcessing);

                if (string.IsNullOrEmpty(selectedImagePath) || !File.Exists(selectedImagePath))
                {
                    await Dispatcher.InvokeAsync(() => extractedTextBox.Text = ResourceManagerHelper.Instance.SelectValidImage);
                    return;
                }

                if (new FileInfo(selectedImagePath).Length > 50 * 1024 * 1024)
                {
                    await Dispatcher.InvokeAsync(() => extractedTextBox.Text = ResourceManagerHelper.Instance.ImageTooLarge);
                    return;
                }

                string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                if (!Directory.Exists(tessDataPath) || availableLanguages.Count == 0)
                {
                    await Dispatcher.InvokeAsync(() => extractedTextBox.Text = ResourceManagerHelper.Instance.MissingTrainingFiles);
                    return;
                }

                await Task.Run(async () =>
                {
                    Debug.WriteLine("Processando imagem em background...");
                    using var processedImage = PreProcessImage(selectedImagePath);
                    using var stream = new MemoryStream();
                    processedImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);   
                    stream.Position = 0;

                    using var pix = Pix.LoadFromMemory(stream.ToArray());
                    using var engine = new TesseractEngine(tessDataPath, "por+eng", EngineMode.Default);
                    ConfigureEngine(engine);

                    await Dispatcher.InvokeAsync(() => extractedTextBox.Text = ResourceManagerHelper.Instance.ExtractingText);
                    using Tesseract.Page page = engine.Process(pix);   
                    string text = page.GetText()?.Trim() ?? "";
                    float confidence = page.GetMeanConfidence();

                    await Dispatcher.InvokeAsync(() =>
                    {
                        extractedTextBox.Text = string.IsNullOrWhiteSpace(text)
                            ? ResourceManagerHelper.Instance.NoTextDetected
                            : string.Format(ResourceManagerHelper.Instance.TextExtracted, confidence.ToString("P"), text);
                    });
                    Debug.WriteLine("Extração concluída com sucesso.");
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => extractedTextBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorExtractingText, ex.Message));
                Debug.WriteLine($"Erro em ExtractText_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void ConfigureEngine(TesseractEngine engine)
        {
            try
            {
                Debug.WriteLine("Configurando engine Tesseract...");
                engine.SetVariable("tessedit_pageseg_mode", "3");
                engine.SetVariable("tessedit_ocr_engine_mode", "3");
                engine.SetVariable("tessedit_char_whitelist",
                    "0123456789abcdefghijklmnopqrstuvwxyzáàâãéèêíìîóòôõúùûçABCDEFGHIJKLMNOPQRSTUVWXYZÁÀÂÃÉÈÊÍÌÎÓÒÔÕÚÙÛÇ.,!?()-_:;/\\[]{}@#$%&*+=<>|\"' ");
                Debug.WriteLine("Engine configurada com sucesso.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao configurar engine: {ex.Message}");
            }
        }

        private async void SelectImage_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("SelectImage_Click iniciado.");
                OpenFileDialog openFileDialog = new()
                {
                    Filter = ResourceManagerHelper.Instance.ImageFileFilter,
                    Title = ResourceManagerHelper.Instance.SelectImageDialogTitle
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    selectedImagePath = openFileDialog.FileName;
                    await Dispatcher.InvokeAsync(() =>
                        extractedTextBox.Text = string.Format(ResourceManagerHelper.Instance.ImageSelected, selectedImagePath));
                    Debug.WriteLine($"Imagem selecionada: {selectedImagePath}");
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => extractedTextBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorSelectingImage, ex.Message));
                Debug.WriteLine($"Erro em SelectImage_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }
    }
}