using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YoutubeTranscriptApi;

namespace DarkHub
{
    public partial class YoutubeVideoDownloader : Page
    {
        private readonly YoutubeDL? ytdl;

        public YoutubeVideoDownloader()
        {
            try
            {
                InitializeComponent();
                this.Unloaded += YoutubeVideoDownloader_Unloaded;
                ytdl = new YoutubeDL
                {
                    YoutubeDLPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "yt-dlp.exe"),
                    FFmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "ffmpeg.exe"),
                    OutputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "DarkHubDownloads"),
                    OverwriteFiles = true
                };
                if (!File.Exists(ytdl.YoutubeDLPath))
                    throw new FileNotFoundException("yt-dlp.exe não encontrado em 'assets'.");
                if (!File.Exists(ytdl.FFmpegPath))
                    throw new FileNotFoundException("ffmpeg.exe não encontrado em 'assets'.");
                if (!Directory.Exists(ytdl.OutputFolder))
                    Directory.CreateDirectory(ytdl.OutputFolder);
                Debug.WriteLine("YoutubeVideoDownloader inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorInitializingYoutubeDownloader, ex.Message),
                                ResourceManagerHelper.Instance.CriticalErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao inicializar YoutubeVideoDownloader: {ex.Message}\nStackTrace: {ex.StackTrace}");
                ytdl = null;
            }
        }

        private void UrlTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Text == ResourceManagerHelper.Instance.UrlPlaceholder)
            {
                textBox.Text = string.Empty;
            }
        }

        private async void Download_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                if (ytdl == null)
                {
                    await Dispatcher.InvokeAsync(() =>
                        statusTextBox.Text = ResourceManagerHelper.Instance.YoutubeDLNotInitializedError);
                    return;
                }

                Debug.WriteLine("Download_Click iniciado.");
                string url = urlTextBox.Text.Trim();
                if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
                {
                    await Dispatcher.InvokeAsync(() =>
                        statusTextBox.Text = ResourceManagerHelper.Instance.InvalidUrlError);
                    Debug.WriteLine("URL inválida fornecida.");
                    return;
                }

                if (formatComboBox.SelectedItem is not ComboBoxItem selectedItem)
                {
                    await Dispatcher.InvokeAsync(() =>
                        statusTextBox.Text = ResourceManagerHelper.Instance.NoFormatSelectedError);
                    return;
                }

                string selectedFormat = selectedItem.Content.ToString().ToLower();

                await Dispatcher.InvokeAsync(() =>
                {
                    statusTextBox.Text = ResourceManagerHelper.Instance.StartingDownload;
                    downloadListBox.Items.Clear();
                    downloadListBox.Items.Add(ResourceManagerHelper.Instance.InitialProgress);
                });

                var progress = new Progress<DownloadProgress>(p =>
                {
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (downloadListBox.Items.Count > 0)
                        {
                            downloadListBox.Items[0] = string.Format(ResourceManagerHelper.Instance.ProgressUpdate, p.Progress.ToString("P0"), p.DownloadSpeed);
                        }
                    });
                });

                await Task.Run(async () =>
                {
                    try
                    {
                        Debug.WriteLine($"Iniciando download da URL: {url} no formato {selectedFormat}");
                        var options = new OptionSet
                        {
                            Verbose = true,
                            Format = selectedFormat == "mp3" ? "bestaudio/best" : "bestvideo+bestaudio/best",
                        };

                        switch (selectedFormat)
                        {
                            case "mp3":
                                options.ExtractAudio = true;
                                options.AudioFormat = AudioConversionFormat.Mp3;
                                break;

                            case "mp4":
                                options.MergeOutputFormat = DownloadMergeFormat.Mp4;
                                break;

                            case "mkv":
                                options.MergeOutputFormat = DownloadMergeFormat.Mkv;
                                break;

                            default:
                                throw new ArgumentException("Formato não suportado.");
                        }

                        var fetch = await ytdl.RunVideoDownload(
                            url: url,
                            progress: progress,
                            overrideOptions: options
                        );

                        if (fetch.Success)
                        {
                            await Dispatcher.InvokeAsync(() =>
                            {
                                statusTextBox.Text = string.Format(ResourceManagerHelper.Instance.DownloadCompleted, fetch.Data);
                                if (downloadListBox.Items.Count > 0)
                                    downloadListBox.Items[0] = ResourceManagerHelper.Instance.ProgressCompleted;
                            });
                            Debug.WriteLine($"Download concluído: {fetch.Data}");
                        }
                        else
                        {
                            string errorDetails = string.Join("\n", fetch.ErrorOutput);
                            await Dispatcher.InvokeAsync(() =>
                            {
                                statusTextBox.Text = string.Format(ResourceManagerHelper.Instance.DownloadError, errorDetails);
                                if (downloadListBox.Items.Count > 0)
                                    downloadListBox.Items[0] = ResourceManagerHelper.Instance.ProgressError;
                            });
                            Debug.WriteLine($"Erro no download reportado pelo YoutubeDL: {errorDetails}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            statusTextBox.Text = string.Format(ResourceManagerHelper.Instance.DownloadFailed, ex.Message);
                            if (downloadListBox.Items.Count > 0)
                                downloadListBox.Items[0] = ResourceManagerHelper.Instance.ProgressError;
                        });
                        Debug.WriteLine($"Erro interno ao baixar vídeo: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    statusTextBox.Text = string.Format(ResourceManagerHelper.Instance.GeneralDownloadError, ex.Message);
                    if (downloadListBox.Items.Count > 0)
                        downloadListBox.Items[0] = ResourceManagerHelper.Instance.ProgressError;
                });
                Debug.WriteLine($"Erro em Download_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void Transcribe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Transcribe_Click iniciado.");
                string url = urlTextBox.Text.Trim();
                if (string.IsNullOrEmpty(url) || !Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult))
                {
                    await Dispatcher.InvokeAsync(() =>
                        statusTextBox.Text = ResourceManagerHelper.Instance.InvalidUrlError);
                    Debug.WriteLine("URL inválida fornecida para transcrição.");
                    return;
                }

                string videoId = ExtractVideoId(url);
                if (string.IsNullOrEmpty(videoId))
                {
                    await Dispatcher.InvokeAsync(() =>
                        statusTextBox.Text = ResourceManagerHelper.Instance.YtVideoIDNotFound);
                    Debug.WriteLine("ID do vídeo não extraído.");
                    return;
                }

                await Dispatcher.InvokeAsync(() =>
                {
                    statusTextBox.Text = ResourceManagerHelper.Instance.YtVideoStartedTranscripting;
                    downloadListBox.Items.Clear();
                    downloadListBox.Items.Add(ResourceManagerHelper.Instance.YtVideoGettingTranscript);
                });

                await Task.Run(async () =>
                {
                    try
                    {
                        using (var youtubeTranscriptApi = new YouTubeTranscriptApi())
                        {
                            var transcriptItems = youtubeTranscriptApi.GetTranscript(videoId, new[] { "pt", "en" });
                            if (transcriptItems == null || !transcriptItems.Any())
                            {
                                await Dispatcher.InvokeAsync(() =>
                                    statusTextBox.Text = ResourceManagerHelper.Instance.YtVideoNoTranscript);
                                Debug.WriteLine("Nenhuma transcrição encontrada para o vídeo.");
                                return;
                            }

                            StringBuilder transcriptText = new StringBuilder();
                            foreach (var item in transcriptItems)
                            {
                                transcriptText.AppendLine(item.Text);
                            }

                            await Dispatcher.InvokeAsync(() =>
                            {
                                statusTextBox.Text = transcriptText.ToString();
                                downloadListBox.Items[0] = ResourceManagerHelper.Instance.YtVideoTranscriptObtained;
                            });
                            Debug.WriteLine("Transcrição obtida com sucesso.");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            statusTextBox.Text = $"Erro ao transcrever o vídeo: {ex.Message}";
                            if (downloadListBox.Items.Count > 0)
                                downloadListBox.Items[0] = "Erro na transcrição.";
                        });
                        Debug.WriteLine($"Erro ao obter transcrição: {ex.Message}\nStackTrace: {ex.StackTrace}");
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                    statusTextBox.Text = $"Erro geral na transcrição: {ex.Message}");
                Debug.WriteLine($"Erro em Transcribe_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void CopyTranscript_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(statusTextBox.Text))
                {
                    Clipboard.SetText(statusTextBox.Text);
                    Debug.WriteLine(ResourceManagerHelper.Instance.TranscribeCopied);
                    MessageBox.Show(ResourceManagerHelper.Instance.TranscribeCopied, "Sucess", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.CopyTranscriptError, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error when copying the transcript: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao copiar transcrição: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private string ExtractVideoId(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string query = uri.Query;
                var queryParams = System.Web.HttpUtility.ParseQueryString(query);
                string videoId = queryParams["v"];
                if (!string.IsNullOrEmpty(videoId))
                    return videoId;

                if (uri.Host.Contains("youtu.be"))
                {
                    return uri.Segments.Last();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private void YoutubeVideoDownloader_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                if (mainWindow.btnYTDownloader != null)
                {
                    mainWindow.btnYTDownloader.Tag = null;
                }
            }
        }
    }
}