using DarkHub;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace DarkHubRmk
{
    public partial class App : Application
    {
        private const string CurrentVersion = "1.1.6";
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string AppUniqueId = "{DarkHub-Single-Instance-GUID-2023}";
        private static Mutex _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, AppUniqueId, out bool createdNew);

            if (!createdNew)
            {
                LogToFile("Outra instância do DarkHub já está em execução. Tentando fechar esta instância.");
                MessageBox.Show("O DarkHub já está em execução.", "Erro", MessageBoxButton.OK, MessageBoxImage.Information);

                Process currentProcess = Process.GetCurrentProcess();
                foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
                {
                    if (process.Id != currentProcess.Id)
                    {
                        LogToFile("Focando a instância existente.");
                        NativeMethods.SetForegroundWindow(process.MainWindowHandle);
                        break;
                    }
                }

                Shutdown(0);
                return;
            }

            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                if (!VerifyDependencies())
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.DependenciesNotFound, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }

                base.OnStartup(e);

                SetupExceptionHandlers();

                LogToFile(ResourceManagerHelper.Instance.AppIniSucc);

                MainWindow = new MainWindow();
                MainWindow.Loaded += async (s, args) =>
                {
                    LogToFile("MainWindow carregado. Verificando atualizações...");
                    await CheckForUpdatesAsync();
                };
                MainWindow.Closing += (s, args) => CleanupAndExit();

                MainWindow.Show();
                LogToFile("MainWindow exibida com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting the application: {ex.Message}\n\nPlease check that all the requirements have been installed.",
                    "Initialization error", MessageBoxButton.OK, MessageBoxImage.Error);
                LogToFile($"Initialization error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                CleanupAndExit();
            }
        }

        private bool VerifyDependencies()
        {
            try
            {
                string[] requiredDirs = new[] { "assets", "tessdata", "fonts" };
                foreach (var dir in requiredDirs)
                {
                    if (!Directory.Exists(dir))
                    {
                        LogToFile($"Required directory not found: {dir}");
                        return false;
                    }
                }

                string[] criticalFiles = new[]
                {
                    "assets\\ffmpeg.exe",
                    "assets\\yt-dlp.exe",
                    "tessdata\\eng.traineddata",
                    "tessdata\\por.traineddata"
                };

                foreach (var file in criticalFiles)
                {
                    if (!File.Exists(file))
                    {
                        LogToFile($"Critical file not found: {file}");
                        return false;
                    }
                }

                if (!RuntimeInformation.FrameworkDescription.Contains("8.0"))
                {
                    LogToFile("Runtime .NET 8.0 not found");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogToFile($"Error checking dependencies: {ex.Message}");
                return false;
            }
        }

        private void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception ex = (Exception)args.ExceptionObject;
                string errorMessage = $"Fatal Error: {ex.Message}\nStackTrace: {ex.StackTrace}\nHResult: {ex.HResult}";
                MessageBox.Show(errorMessage, "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(errorMessage);
                LogToFile(errorMessage);
                CleanupAndExit();
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                string errorMessage = $"Ui Error: {args.Exception.Message}\nStackTrace: {args.Exception.StackTrace}\nHResult: {args.Exception.HResult}";
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(errorMessage);
                LogToFile(errorMessage);
                args.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                string errorMessage = $"Error in Task not observed: {args.Exception.Message}";
                LogToFile(errorMessage);
                args.SetObserved();
            };
        }

        private void CleanupAndExit()
        {
            try
            {
                LogToFile("Iniciando limpeza e encerramento do aplicativo.");

                if (_mutex != null)
                {
                    _mutex.ReleaseMutex();
                    _mutex.Dispose();
                    _mutex = null;
                    LogToFile("Mutex liberado e descartado.");
                }

                httpClient?.Dispose();
                LogToFile("HttpClient descartado.");

                Shutdown(0);
                LogToFile("Shutdown chamado.");

                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                LogToFile($"Erro durante a limpeza: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Environment.Exit(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            CleanupAndExit();
        }

        private static void LogToFile(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                    LogToFile("Diretório de logs criado.");
                }

                string logFile = Path.Combine(logPath, "app_log.txt");
                File.AppendAllText(logFile, $"{DateTime.Now}: {message}\n");
                Debug.WriteLine($"Log gravado: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao salvar log: {ex.Message}");
            }
        }

        public static void RunOnUIThread(Action action)
        {
            if (Current?.Dispatcher != null)
            {
                if (Current.Dispatcher.CheckAccess())
                    action();
                else
                    Current.Dispatcher.Invoke(action);
            }
            else
            {
                LogToFile("Dispatcher not available for RunOnUIThread.");
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            LogToFile("CheckForUpdatesAsync iniciado.");
            try
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", "DarkHub-Update-Checker");
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                LogToFile("Fazendo requisição à API do GitHub...");
                string jsonResponse = await httpClient.GetStringAsync("https://api.github.com/repos/Pokeralho/DarkHub/releases/latest");
                LogToFile("Resposta da API do GitHub recebida.");

                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                JsonElement root = doc.RootElement;

                string latestVersion = root.GetProperty("tag_name").GetString()?.TrimStart('v');
                LogToFile($"Versão atual: {CurrentVersion}, Versão mais recente: {latestVersion}");

                if (string.IsNullOrEmpty(latestVersion))
                {
                    LogToFile("Não foi possível obter a versão mais recente do GitHub.");
                    return;
                }

                if (IsNewerVersion(latestVersion, CurrentVersion))
                {
                    LogToFile("Nova versão encontrada. Procurando arquivo de atualização...");
                    string downloadUrl = null;
                    foreach (var asset in root.GetProperty("assets").EnumerateArray())
                    {
                        string assetName = asset.GetProperty("name").GetString();
                        LogToFile($"Asset encontrado: {assetName}");

                        if (assetName == "DarkHub.Setup.exe")
                        {
                            downloadUrl = asset.GetProperty("browser_download_url").GetString();
                            break;
                        }
                    }

                    if (downloadUrl != null)
                    {
                        LogToFile($"Nova versão disponível: {latestVersion}. URL de download: {downloadUrl}");
                        ShowUpdateNotification(downloadUrl, latestVersion);
                    }
                    else
                    {
                        LogToFile("Nova versão encontrada, mas 'DarkHub.Setup.exe' não está disponível nos assets.");
                    }
                }
                else
                {
                    LogToFile($"O aplicativo está atualizado. Versão atual: {CurrentVersion}, Versão mais recente: {latestVersion}");
                }
            }
            catch (HttpRequestException ex)
            {
                LogToFile($"Erro de rede ao verificar atualizações: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            catch (JsonException ex)
            {
                LogToFile($"Erro ao processar JSON da API do GitHub: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                LogToFile($"Erro inesperado em CheckForUpdatesAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
            LogToFile("CheckForUpdatesAsync concluído.");
        }

        private bool IsNewerVersion(string latestVersion, string currentVersion)
        {
            try
            {
                Version vLatest = new Version(latestVersion);
                Version vCurrent = new Version(currentVersion);
                bool isNewer = vLatest > vCurrent;
                LogToFile($"Comparando versões: {currentVersion} vs {latestVersion}. É mais recente? {isNewer}");
                return isNewer;
            }
            catch (Exception ex)
            {
                LogToFile($"Erro ao comparar versões: {ex.Message}");
                return false;
            }
        }

        private void ShowUpdateNotification(string downloadUrl, string latestVersion)
        {
            RunOnUIThread(() =>
            {
                var window = Current.MainWindow;
                if (window == null)
                {
                    LogToFile("MainWindow is null when trying to show notification.");
                    MessageBox.Show("MainWindow is not available.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LogToFile("Exibindo MessageBox de atualização.");

                string message = $"{ResourceManagerHelper.Instance.NewVersionAvailable1} (v{latestVersion}). {ResourceManagerHelper.Instance.NewVersionAvailable2}";
                MessageBoxResult result = MessageBox.Show(message, ResourceManagerHelper.Instance.NewVersion, MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    LogToFile("O usuário escolheu baixar a atualização.");
                    Task.Run(async () => await DownloadUpdateAsync(downloadUrl));
                }
                else
                {
                    LogToFile("O usuário recusou a atualização.");
                }
            });
        }

        private async Task DownloadUpdateAsync(string downloadUrl)
        {
            try
            {
                LogToFile("Iniciando DownloadUpdateAsync.");
                LogToFile($"Tentando baixar de: {downloadUrl}");

                string downloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "DarkHub.Setup.exe");
                LogToFile($"Caminho de destino: {downloadPath}");

                string directory = Path.GetDirectoryName(downloadPath);
                if (!Directory.Exists(directory))
                {
                    LogToFile("Diretório de downloads não existe. Criando...");
                    Directory.CreateDirectory(directory);
                }

                using var response = await httpClient.GetAsync(downloadUrl);
                LogToFile($"Resposta HTTP recebida. Status: {response.StatusCode}");
                response.EnsureSuccessStatusCode();

                using var fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream);
                fileStream.Close();

                LogToFile($"Atualização baixada com sucesso em: {downloadPath}");

                LogToFile("Iniciando o instalador, fechando o aplicativo e agendando a exclusão do arquivo.");

                string cmdArguments = $"/C start \"\" \"{downloadPath}\" && timeout /t 5 && del \"{downloadPath}\"";
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = cmdArguments,
                    UseShellExecute = true,
                    CreateNoWindow = true
                });

                CleanupAndExit();
            }
            catch (HttpRequestException ex)
            {
                LogToFile($"Erro de rede ao baixar atualização: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show("Erro de rede ao baixar a atualização. Verifique sua conexão e tente novamente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (UnauthorizedAccessException ex)
            {
                LogToFile($"Erro de permissão ao salvar arquivo: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show("Sem permissão para salvar o arquivo. Execute o aplicativo como administrador.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                LogToFile($"Erro inesperado ao baixar atualização: {ex.Message}\nStackTrace: {ex.StackTrace}");
                MessageBox.Show("Erro ao baixar a atualização. Tente novamente mais tarde.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}