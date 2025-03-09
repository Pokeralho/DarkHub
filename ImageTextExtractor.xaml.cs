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
                const int maxDimension = 2000;
                double scale = 1.0;
                if (width > maxDimension || height > maxDimension)
                {
                    scale = Math.Min((double)maxDimension / width, (double)maxDimension / height);
                    width = (int)(width * scale);
                    height = (int)(height * scale);
                }
                else if (width < 800 || height < 800)
                {
                    scale = Math.Max(800.0 / width, 800.0 / height);
                    width = (int)(width * scale);
                    height = (int)(height * scale);
                }

                var processedImage = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(processedImage))
                {
                    g.Clear(Color.White);
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    g.DrawImage(originalImage, 0, 0, width, height);
                }

                using (var adjustedImage = new Bitmap(width, height))
                {
                    using (var g = Graphics.FromImage(adjustedImage))
                    {
                        var colorMatrix = new System.Drawing.Imaging.ColorMatrix(
                            new float[][]
                            {
                                new float[] {1.5f, 0, 0, 0, 0},
                                new float[] {0, 1.5f, 0, 0, 0},
                                new float[] {0, 0, 1.5f, 0, 0},
                                new float[] {0, 0, 0, 1, 0},
                                new float[] {-0.2f, -0.2f, -0.2f, 0, 1}
                            });

                        var attributes = new System.Drawing.Imaging.ImageAttributes();
                        attributes.SetColorMatrix(colorMatrix);

                        g.DrawImage(processedImage,
                            new Rectangle(0, 0, width, height),
                            0, 0, width, height,
                            GraphicsUnit.Pixel,
                            attributes);
                    }
                    return new Bitmap(adjustedImage);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro no pré-processamento: {ex.Message}");
                throw;
            }
        }

        private void ProcessImageBlock(byte[] rgbValues)
        {
            byte min = 255, max = 0;
            for (int i = 0; i < rgbValues.Length; i += 3)
            {
                byte gray = (byte)((rgbValues[i] * 0.299 + rgbValues[i + 1] * 0.587 + rgbValues[i + 2] * 0.114));
                min = Math.Min(min, gray);
                max = Math.Max(max, gray);
            }

            if (max == min) return;

            byte threshold = (byte)((max + min) / 2);
            for (int i = 0; i < rgbValues.Length; i += 3)
            {
                byte gray = (byte)((rgbValues[i] * 0.299 + rgbValues[i + 1] * 0.587 + rgbValues[i + 2] * 0.114));
                gray = (byte)(((gray - min) * 255.0) / (max - min));
                byte value = gray < threshold ? (byte)0 : (byte)255;
                rgbValues[i] = rgbValues[i + 1] = rgbValues[i + 2] = value;
            }
        }

        private async void ExtractText_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("ExtractText_Click iniciado.");
                btnExtractText.IsEnabled = false;
                btnSelectImage.IsEnabled = false;

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
                    try
                    {
                        Debug.WriteLine("Processando imagem em background...");
                        string text;
                        float confidence;

                        using (var processedImage = PreProcessImage(selectedImagePath))
                        {
                            await Dispatcher.InvokeAsync(() => extractedTextBox.Text = ResourceManagerHelper.Instance.ExtractingText);

                            using var engine = new TesseractEngine(tessDataPath, "por+eng", EngineMode.Default);
                            ConfigureEngine(engine);

                            using var stream = new MemoryStream();
                            processedImage.Save(stream, System.Drawing.Imaging.ImageFormat.Bmp);
                            stream.Position = 0;

                            using var pix = Pix.LoadFromMemory(stream.ToArray());
                            using var page = engine.Process(pix);
                            text = page.GetText()?.Trim() ?? "";
                            confidence = page.GetMeanConfidence();
                        }

                        GC.Collect(2, GCCollectionMode.Forced, true);
                        await Task.Delay(100);

                        await Dispatcher.InvokeAsync(() =>
                        {
                            extractedTextBox.Text = string.IsNullOrWhiteSpace(text)
                                ? ResourceManagerHelper.Instance.NoTextDetected
                                : string.Format(ResourceManagerHelper.Instance.TextExtracted, confidence.ToString("P"), text);
                        });
                        Debug.WriteLine("Extração concluída com sucesso.");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Erro durante o processamento da imagem: {ex.Message}", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => extractedTextBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorExtractingText, ex.Message));
                Debug.WriteLine($"Erro em ExtractText_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            finally
            {
                btnExtractText.IsEnabled = true;
                btnSelectImage.IsEnabled = true;
            }
        }

        private void ConfigureEngine(TesseractEngine engine)
        {
            try
            {
                Debug.WriteLine("Configurando engine Tesseract...");

                engine.SetVariable("tessedit_pageseg_mode", "6");
                engine.SetVariable("tessedit_ocr_engine_mode", "2");
                engine.SetVariable("lstm_choice_mode", "2");
                engine.SetVariable("tessedit_char_blacklist", "{}[]()$~");
                engine.SetVariable("tessedit_enable_dict_correction", "1");
                engine.SetVariable("tessedit_enable_bigram_correction", "1");
                engine.SetVariable("classify_bln_numeric_mode", "0");
                engine.SetVariable("edges_childarea", "0.5");
                engine.SetVariable("edges_max_children_per_outline", "50");
                engine.SetVariable("textord_heavy_nr", "0");
                engine.SetVariable("textord_noise_sizelimit", "0.2");
                engine.SetVariable("load_system_dawg", "1");
                engine.SetVariable("load_freq_dawg", "1");
                engine.SetVariable("language_model_penalty_non_dict_word", "0.2");
                engine.SetVariable("language_model_penalty_non_freq_dict_word", "0.1");
                engine.SetVariable("tessedit_char_whitelist",
                    "0123456789abcdefghijklmnopqrstuvwxyzáàâãéèêíìîóòôõúùûçABCDEFGHIJKLMNOPQRSTUVWXYZÁÀÂÃÉÈÊÍÌÎÓÒÔÕÚÙÛÇ.,!?-_:;/\\@#$%&*+=<>|\"' ");

                Debug.WriteLine("Engine configurada com sucesso.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao configurar engine: {ex.Message}");
                throw;
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