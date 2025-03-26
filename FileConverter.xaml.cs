using Microsoft.Win32;
using PdfSharp.Drawing;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DarkHub
{
    public partial class FileConverter : Page
    {
        private List<string> inputFiles = new();
        private string outputDir = string.Empty;

        public FileConverter()
        {
            try
            {
                InitializeComponent();
                OutputFormatCombo.SelectedIndex = 0;
                Debug.WriteLine(ResourceManagerHelper.Instance.FileConverterInitialized);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorInitializingFileConverter, ex.Message),
                                ResourceManagerHelper.Instance.CriticalErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao inicializar FileConverter: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SelectFiles_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                OpenFileDialog openFileDialog = new()
                {
                    Multiselect = true,
                    Title = "Selecionar Arquivos para Conversão",
                    Filter = "Todos os Arquivos (*.*)|*.*|Imagens (*.png;*.jpg;*.raw)|*.png;*.jpg;*.raw|Binários (*.bin)|*.bin|PDF (*.pdf)|*.pdf|Vídeo (*.mp4;*.avi)|*.mp4;*.avi|Áudio (*.mp3;*.wav)|*.mp3;*.wav"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    inputFiles = new List<string>(openFileDialog.FileNames);
                    SelectedFilesText.Text = string.Format(ResourceManagerHelper.Instance.SelectedFilesText, inputFiles.Count);
                    Debug.WriteLine(string.Format(ResourceManagerHelper.Instance.FilesSelectedLog, inputFiles.Count));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorSelectingFiles, ex.Message),
                                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em SelectFiles_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SelectOutputDir_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                OpenFolderDialog folderDialog = new()
                {
                    Multiselect = false,
                    Title = "Escolher Diretório de Saída"
                };
                if (folderDialog.ShowDialog() == true)
                {
                    outputDir = folderDialog.FolderName;
                    OutputDirText.Text = string.Format(ResourceManagerHelper.Instance.OutputDirText, outputDir);
                    Debug.WriteLine(string.Format(ResourceManagerHelper.Instance.OutputDirSelectedLog, outputDir));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorSelectingOutputDir, ex.Message),
                                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em SelectOutputDir_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void ConvertFiles_Click(object? sender, RoutedEventArgs? e)
        {
            Window? progressWindow = null;
            try
            {
                if (inputFiles.Count == 0 || OutputFormatCombo.SelectedItem == null)
                {
                    Dispatcher.Invoke(() => MessageBox.Show(ResourceManagerHelper.Instance.SelectFilesAndFormatWarning,
                                                            ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                string? outputFormat = ((ComboBoxItem)OutputFormatCombo.SelectedItem)?.Content?.ToString()?.ToLower();
                if (string.IsNullOrEmpty(outputFormat))
                {
                    Dispatcher.Invoke(() => MessageBox.Show(ResourceManagerHelper.Instance.InvalidOutputFormatWarning,
                                                            ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Warning));
                    return;
                }

                progressWindow = CreateProgressWindow(inputFiles.Count);
                await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Show()));
                TextBox? progressTextBox = progressWindow.FindName("ProgressTextBox") as TextBox;

                int completed = 0;

                if (IsDocumentFormat(outputFormat) && outputFormat == "pdf")
                {
                    string outputFile = Path.Combine(string.IsNullOrEmpty(outputDir) ? Path.GetDirectoryName(inputFiles[0]) ?? string.Empty : outputDir,
                                                     $"CombinedImages_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ConvertingFileProgress,
                                                                  "Imagens selecionadas", "pdf"));

                    await Task.Run(() =>
                    {
                        try
                        {
                            ConvertImagesToPdf(inputFiles, outputFile);
                            AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.CompletedFileProgress,
                                                                          Path.GetFileName(outputFile)));
                        }
                        catch (Exception ex)
                        {
                            AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorConvertingFileProgress,
                                                                          "Imagens selecionadas", ex.Message));
                            Debug.WriteLine($"Erro ao converter imagens para PDF: {ex.Message}\nStackTrace: {ex.StackTrace}");
                        }
                        finally
                        {
                            completed = inputFiles.Count;
                            AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ProgressUpdateText,
                                                                          completed, inputFiles.Count));
                        }
                    });
                }
                else
                {
                    await Task.Run(() =>
                    {
                        Parallel.ForEach(inputFiles, inputFile =>
                        {
                            try
                            {
                                string outputFile = GetOutputFilePath(inputFile, outputFormat);
                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ConvertingFileProgress,
                                                                              Path.GetFileName(inputFile), outputFormat));

                                string inputExtension = Path.GetExtension(inputFile).ToLower();

                                if ((outputFormat == "gif" || IsVideoFormat(outputFormat) || IsAudioFormat(outputFormat)) &&
                                    (IsVideoFormat(inputExtension.Replace(".", "")) || IsAudioFormat(inputExtension.Replace(".", ""))))
                                {
                                    ConvertMedia(inputFile, outputFile, outputFormat);
                                }
                                else if (IsImageFormat(outputFormat))
                                {
                                    if (inputExtension == ".pdf")
                                        ConvertPdfToImages(inputFile, outputDir, outputFormat);
                                    else if (inputExtension == ".bin" || inputExtension == ".raw")
                                        ConvertBinOrRawToImage(inputFile, outputFile, outputFormat);
                                    else if (IsImageFormat(inputExtension.Replace(".", "")))
                                        ConvertImage(inputFile, outputFile);
                                    else
                                        throw new ArgumentException($"Formato de entrada {inputExtension} não suportado para conversão para {outputFormat}");
                                }
                                else if (outputFormat == "raw" || outputFormat == "bin")
                                {
                                    ConvertToRawOrBin(inputFile, outputFile, outputFormat);
                                }

                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.CompletedFileProgress,
                                                                              Path.GetFileName(outputFile)));
                            }
                            catch (Exception ex)
                            {
                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ErrorConvertingFileProgress,
                                                                              Path.GetFileName(inputFile), ex.Message));
                                Debug.WriteLine($"Erro na conversão de {inputFile}: {ex.Message}\nStackTrace: {ex.StackTrace}");
                            }
                            finally
                            {
                                Interlocked.Increment(ref completed);
                                AppendProgress(progressTextBox, string.Format(ResourceManagerHelper.Instance.ProgressUpdateText,
                                                                              completed, inputFiles.Count));
                            }
                        });
                    });
                }

                AppendProgress(progressTextBox, ResourceManagerHelper.Instance.ConversionCompleted);
                await Task.Run(() => Dispatcher.Invoke(() => MessageBox.Show(ResourceManagerHelper.Instance.ConversionCompleted,
                                                                             ResourceManagerHelper.Instance.SuccessTitle,
                                                                             MessageBoxButton.OK, MessageBoxImage.Information)));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show(string.Format(ResourceManagerHelper.Instance.GeneralConversionError, ex.Message),
                                                        ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error));
                Debug.WriteLine($"Erro em ConvertFiles_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            finally
            {
                if (progressWindow != null)
                {
                    await Task.Run(() => progressWindow.Dispatcher.Invoke(() => progressWindow.Close()));
                }
            }
        }

        private string GetOutputFilePath(string inputFile, string outputFormat)
        {
            try
            {
                string dir = string.IsNullOrEmpty(outputDir) ? Path.GetDirectoryName(inputFile) ?? string.Empty : outputDir;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string fileName = Path.GetFileNameWithoutExtension(inputFile);
                string outputPath = Path.Combine(dir, $"{fileName}.{outputFormat}");
                Debug.WriteLine($"Generated output path: {outputPath}");
                return outputPath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao gerar caminho de saída: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Path.ChangeExtension(inputFile, outputFormat);
            }
        }

        private static bool IsImageFormat(string format) => format switch
        {
            "png" => true,
            "jpg" => true,
            "webp" => true,
            "bmp" => true,
            "gif" => true,
            "tiff" => true,
            _ => false
        };

        private static bool IsVideoFormat(string format) => format switch
        {
            "mp4" => true,
            "avi" => true,
            "mkv" => true,
            _ => false
        };

        private static bool IsAudioFormat(string format) => format switch
        {
            "mp3" => true,
            "wav" => true,
            "aac" => true,
            _ => false
        };

        private static bool IsDocumentFormat(string format) => format switch
        {
            "pdf" => true,
            _ => false
        };

        private static bool IsRawOrBinFormat(string format) => format switch
        {
            "raw" => true,
            "bin" => true,
            _ => false
        };

        private void ConvertImage(string inputFile, string outputFile)
        {
            try
            {
                using (var image = System.Drawing.Image.FromFile(inputFile))
                {
                    image.Save(outputFile, GetImageFormat(Path.GetExtension(outputFile).ToLower()));
                    Debug.WriteLine($"Imagem convertida: {inputFile} -> {outputFile}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em ConvertImage: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void ConvertMedia(string inputFile, string outputFile, string outputFormat)
        {
            try
            {
                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "ffmpeg.exe");
                if (!File.Exists(ffmpegPath))
                    throw new FileNotFoundException($"FFmpeg (ffmpeg.exe) não encontrado em: {ffmpegPath}");

                string arguments = outputFormat switch
                {
                    "mp3" => $"-i \"{inputFile}\" -vn -acodec mp3 -ab 192k \"{outputFile}\"",
                    "wav" => $"-i \"{inputFile}\" -vn -acodec pcm_s16le \"{outputFile}\"",
                    "aac" => $"-i \"{inputFile}\" -vn -acodec aac -ab 128k \"{outputFile}\"",
                    "mp4" => $"-i \"{inputFile}\" -c:v libx264 -preset fast -c:a aac \"{outputFile}\"",
                    "avi" => $"-i \"{inputFile}\" -c:v mpeg4 -c:a mp3 \"{outputFile}\"",
                    "mkv" => $"-i \"{inputFile}\" -c:v copy -c:a copy \"{outputFile}\"",
                    "gif" => $"-i \"{inputFile}\" -vf \"fps=10,scale=320:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" -loop 0 \"{outputFile}\"",
                    _ => throw new ArgumentException($"Formato de mídia não suportado: {outputFormat}")
                };

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new Exception($"Erro na conversão de mídia: {error}\nSaída: {output}");
                Debug.WriteLine($"Mídia convertida: {inputFile} -> {outputFile}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em ConvertMedia: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void ConvertBinOrRawToImage(string inputFile, string outputFile, string outputFormat)
        {
            try
            {
                byte[] rawData = File.ReadAllBytes(inputFile);

                using (var stream = new MemoryStream(rawData))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        int width = reader.ReadInt32();
                        int height = reader.ReadInt32();
                        int bytesPerPixel = 3;
                        int expectedSize = width * height * bytesPerPixel;
                        byte[] imageData = reader.ReadBytes(expectedSize);

                        if (imageData.Length != expectedSize)
                            throw new Exception($"Dados insuficientes no arquivo {inputFile}. Esperado: {expectedSize} bytes, encontrado: {imageData.Length} bytes");

                        using (var bitmap = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                        {
                            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                                                             ImageLockMode.WriteOnly, bitmap.PixelFormat);
                            IntPtr ptr = bitmapData.Scan0;
                            int stride = bitmapData.Stride;

                            for (int y = 0; y < height; y++)
                            {
                                int sourceOffset = y * width * bytesPerPixel;
                                IntPtr destPtr = IntPtr.Add(ptr, y * stride);
                                System.Runtime.InteropServices.Marshal.Copy(imageData, sourceOffset, destPtr, width * bytesPerPixel);
                            }

                            bitmap.UnlockBits(bitmapData);
                            bitmap.Save(outputFile, GetImageFormat(outputFormat));
                        }
                    }
                }
                Debug.WriteLine($"Arquivo .bin/.raw convertido para imagem: {inputFile} -> {outputFile}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em ConvertBinOrRawToImage: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void ConvertBinOrRawToMedia(string inputFile, string outputFile, string outputFormat)
        {
            try
            {
                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "ffmpeg.exe");
                if (!File.Exists(ffmpegPath))
                    throw new FileNotFoundException($"FFmpeg (ffmpeg.exe) não encontrado em: {ffmpegPath}");

                string arguments = outputFormat switch
                {
                    "mp3" => $"-f s16le -ar 44100 -ac 1 -i \"{inputFile}\" -acodec mp3 -ab 192k \"{outputFile}\"",
                    "wav" => $"-f s16le -ar 44100 -ac 1 -i \"{inputFile}\" -acodec pcm_s16le \"{outputFile}\"",
                    "aac" => $"-f s16le -ar 44100 -ac 1 -i \"{inputFile}\" -acodec aac -ab 128k \"{outputFile}\"",
                    "mp4" => $"-f rawvideo -pix_fmt yuv420p -s 1920x1080 -r 30 -i \"{inputFile}\" -c:v libx264 -preset fast -c:a aac \"{outputFile}\"",
                    "avi" => $"-f rawvideo -pix_fmt yuv420p -s 1920x1080 -r 30 -i \"{inputFile}\" -c:v mpeg4 -c:a mp3 \"{outputFile}\"",
                    "mkv" => $"-f rawvideo -pix_fmt yuv420p -s 1920x1080 -r 30 -i \"{inputFile}\" -c:v copy -c:a copy \"{outputFile}\"",
                    "gif" => $"-f rawvideo -pix_fmt rgb24 -s 1920x1080 -r 30 -i \"{inputFile}\" -vf \"fps=10,scale=320:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" -loop 0 \"{outputFile}\"",
                    _ => throw new ArgumentException($"Formato de mídia não suportado para .bin/.raw: {outputFormat}")
                };

                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new Exception($"Erro na conversão de .bin/.raw para mídia: {error}\nSaída: {output}");
                Debug.WriteLine($"Arquivo .bin/.raw convertido para mídia: {inputFile} -> {outputFile}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em ConvertBinOrRawToMedia: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void ConvertToRawOrBin(string inputFile, string outputFile, string outputFormat)
        {
            try
            {
                string inputExtension = Path.GetExtension(inputFile).ToLower();
                byte[] rawData;

                if (IsImageFormat(inputExtension.Replace(".", "")))
                {
                    using (var image = System.Drawing.Image.FromFile(inputFile))
                    {
                        using (var bitmap = new System.Drawing.Bitmap(image))
                        {
                            int width = bitmap.Width;
                            int height = bitmap.Height;
                            int bytesPerPixel = 3;
                            int dataSize = width * height * bytesPerPixel;
                            rawData = new byte[dataSize];

                            var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                                                             ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                            IntPtr ptr = bitmapData.Scan0;
                            int stride = bitmapData.Stride;
                            for (int y = 0; y < height; y++)
                            {
                                IntPtr rowPtr = IntPtr.Add(ptr, y * stride);
                                int rowOffset = y * width * bytesPerPixel;
                                System.Runtime.InteropServices.Marshal.Copy(rowPtr, rawData, rowOffset, width * bytesPerPixel);
                            }
                            bitmap.UnlockBits(bitmapData);

                            using (var stream = new FileStream(outputFile, FileMode.Create))
                            {
                                using (var writer = new BinaryWriter(stream))
                                {
                                    writer.Write(width);
                                    writer.Write(height);
                                    writer.Write(rawData);
                                }
                            }
                        }
                    }
                }
                else if (IsAudioFormat(inputExtension.Replace(".", "")) || IsVideoFormat(inputExtension.Replace(".", "")))
                {
                    string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "ffmpeg.exe");
                    if (!File.Exists(ffmpegPath))
                        throw new FileNotFoundException($"FFmpeg (ffmpeg.exe) não encontrado em: {ffmpegPath}");

                    string tempFile = Path.GetTempFileName();
                    string arguments = inputExtension switch
                    {
                        ".mp3" or ".wav" or ".aac" => $"-i \"{inputFile}\" -f s16le -acodec pcm_s16le \"{tempFile}\"",
                        ".mp4" or ".avi" or ".mkv" => $"-i \"{inputFile}\" -f rawvideo -pix_fmt yuv420p \"{tempFile}\"",
                        _ => throw new ArgumentException($"Formato de entrada não suportado para conversão para {outputFormat}: {inputExtension}")
                    };

                    using (var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = ffmpegPath,
                            Arguments = arguments,
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    })
                    {
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                            throw new Exception($"Erro ao extrair dados brutos: {error}\nSaída: {output}");
                    }

                    rawData = File.ReadAllBytes(tempFile);
                    File.Delete(tempFile);
                    File.WriteAllBytes(outputFile, rawData);
                }
                else if (inputExtension == ".pdf")
                {
                    using (var document = PdfiumViewer.PdfDocument.Load(inputFile))
                    {
                        using (var image = document.Render(0, 300, 300, true))
                        {
                            using (var bitmap = new System.Drawing.Bitmap(image))
                            {
                                int width = bitmap.Width;
                                int height = bitmap.Height;
                                int bytesPerPixel = 3;
                                int dataSize = width * height * bytesPerPixel;
                                rawData = new byte[dataSize];

                                var bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                                                                 ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                                IntPtr ptr = bitmapData.Scan0;
                                int stride = bitmapData.Stride;
                                for (int y = 0; y < height; y++)
                                {
                                    IntPtr rowPtr = IntPtr.Add(ptr, y * stride);
                                    int rowOffset = y * width * bytesPerPixel;
                                    System.Runtime.InteropServices.Marshal.Copy(rowPtr, rawData, rowOffset, width * bytesPerPixel);
                                }
                                bitmap.UnlockBits(bitmapData);

                                using (var stream = new FileStream(outputFile, FileMode.Create))
                                {
                                    using (var writer = new BinaryWriter(stream))
                                    {
                                        writer.Write(width);
                                        writer.Write(height);
                                        writer.Write(rawData);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Conversão de {inputExtension} para {outputFormat} não suportada.");
                }

                Debug.WriteLine($"Arquivo convertido para {outputFormat}: {inputFile} -> {outputFile}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em ConvertToRawOrBin: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void ConvertPdfToImages(string inputFile, string outputDir, string imageFormat)
        {
            try
            {
                string outputBase = Path.Combine(string.IsNullOrEmpty(outputDir) ? Path.GetDirectoryName(inputFile) ?? string.Empty : outputDir,
                                                 Path.GetFileNameWithoutExtension(inputFile));
                if (!Directory.Exists(Path.GetDirectoryName(outputBase)))
                    Directory.CreateDirectory(Path.GetDirectoryName(outputBase));

                using (var document = PdfiumViewer.PdfDocument.Load(inputFile))
                {
                    for (int i = 0; i < document.PageCount; i++)
                    {
                        using (var image = document.Render(i, 300, 300, true))
                        {
                            string outputFile = $"{outputBase}-{i}.{imageFormat}";
                            image.Save(outputFile, GetImageFormat(imageFormat));
                            Debug.WriteLine($"Página {i + 1} convertida: {outputFile}");
                        }
                    }
                }
                Debug.WriteLine($"PDF convertido para imagens: {inputFile} -> {outputBase}-*.{imageFormat}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em ConvertPdfToImages: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void ConvertImagesToPdf(List<string> inputFiles, string outputFile)
        {
            try
            {
                string outputDir = Path.GetDirectoryName(outputFile);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                using (var pdf = new PdfSharp.Pdf.PdfDocument())
                {
                    foreach (string imageFile in inputFiles)
                    {
                        if (!File.Exists(imageFile))
                            throw new FileNotFoundException($"Imagem não encontrada: {imageFile}");

                        XImage xImage = XImage.FromFile(imageFile);
                        PdfSharp.Pdf.PdfPage page = pdf.AddPage();

                        using (XGraphics gfx = XGraphics.FromPdfPage(page))
                        {
                            gfx.DrawImage(xImage, 0, 0, xImage.PointWidth, xImage.PointHeight);
                        }
                    }
                    pdf.Save(outputFile);
                }
                Debug.WriteLine($"Imagens convertidas para PDF: {inputFiles.Count} arquivos -> {outputFile}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em ConvertImagesToPdf: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private ImageFormat GetImageFormat(string format) => format.ToLower() switch
        {
            ".png" => ImageFormat.Png,
            "png" => ImageFormat.Png,
            ".jpg" => ImageFormat.Jpeg,
            "jpg" => ImageFormat.Jpeg,
            ".webp" => ImageFormat.Webp,
            "webp" => ImageFormat.Webp,
            ".bmp" => ImageFormat.Bmp,
            "bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            "gif" => ImageFormat.Gif,
            ".tiff" => ImageFormat.Tiff,
            "tiff" => ImageFormat.Tiff,
            _ => throw new ArgumentException($"Formato de imagem não suportado: {format}")
        };

        private Window CreateProgressWindow(int totalFiles)
        {
            try
            {
                var window = new Window
                {
                    Title = ResourceManagerHelper.Instance.ConversionProgressTitle,
                    Width = 400,
                    Height = 300,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(System.Windows.Media.Colors.White),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(115, 69, 161)),
                    BorderThickness = new Thickness(2)
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                var title = new TextBlock
                {
                    Text = ResourceManagerHelper.Instance.ConvertingFilesTitle,
                    FontFamily = new System.Windows.Media.FontFamily("JetBrains Mono"),
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = System.Windows.Media.Brushes.Black,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(title, 0);
                grid.Children.Add(title);

                var textBox = new TextBox
                {
                    Name = "ProgressTextBox",
                    IsReadOnly = true,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245)),
                    Foreground = System.Windows.Media.Brushes.Black,
                    Margin = new Thickness(10),
                    Padding = new Thickness(5),
                    FontFamily = new System.Windows.Media.FontFamily("JetBrains Mono"),
                    FontSize = 12,
                    Text = string.Format(ResourceManagerHelper.Instance.ProgressInitialText, totalFiles)
                };
                Grid.SetRow(textBox, 1);
                grid.Children.Add(textBox);

                window.Content = new Border
                {
                    CornerRadius = new CornerRadius(10),
                    Background = window.Background,
                    Child = grid,
                    Margin = new Thickness(2)
                };

                Debug.WriteLine(ResourceManagerHelper.Instance.ProgressWindowCreated);
                return window;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao criar janela de progresso: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void AppendProgress(TextBox? textBox, string message)
        {
            try
            {
                if (textBox != null)
                {
                    textBox.Dispatcher.Invoke(() =>
                    {
                        textBox.AppendText(message + Environment.NewLine);
                        textBox.ScrollToEnd();
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao atualizar ProgressTextBox: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }
    }
}