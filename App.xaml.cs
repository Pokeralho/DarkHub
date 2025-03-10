using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace DarkHubRmk
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                if (!VerifyDependencies())
                {
                    MessageBox.Show("Algumas dependências necessárias não foram encontradas. Por favor, reinstale o aplicativo.",
                        "Erro de Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(1);
                    return;
                }

                base.OnStartup(e);

                SetupExceptionHandlers();

                LogToFile("Aplicação iniciada com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar a aplicação: {ex.Message}\n\nPor favor, verifique se todos os requisitos foram instalados.",
                    "Erro de Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
                LogToFile($"Erro na inicialização: {ex.Message}\nStackTrace: {ex.StackTrace}");
                Environment.Exit(1);
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
                        LogToFile($"Diretório necessário não encontrado: {dir}");
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
                        LogToFile($"Arquivo crítico não encontrado: {file}");
                        return false;
                    }
                }

                if (!RuntimeInformation.FrameworkDescription.Contains("8.0"))
                {
                    LogToFile("Runtime .NET 8.0 não encontrado");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogToFile($"Erro ao verificar dependências: {ex.Message}");
                return false;
            }
        }

        private void SetupExceptionHandlers()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception ex = (Exception)args.ExceptionObject;
                string errorMessage = $"Erro fatal: {ex.Message}\nStackTrace: {ex.StackTrace}\nHResult: {ex.HResult}";
                MessageBox.Show(errorMessage, "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(errorMessage);
                LogToFile(errorMessage);
                Environment.Exit(1);
            };

            DispatcherUnhandledException += (sender, args) =>
            {
                string errorMessage = $"Erro na UI: {args.Exception.Message}\nStackTrace: {args.Exception.StackTrace}\nHResult: {args.Exception.HResult}";
                MessageBox.Show(errorMessage, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine(errorMessage);
                LogToFile(errorMessage);
                args.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                string errorMessage = $"Erro em Task não observada: {args.Exception.Message}";
                LogToFile(errorMessage);
                args.SetObserved();
            };
        }

        private void LogToFile(string message)
        {
            try
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(logPath);
                string logFile = Path.Combine(logPath, "app_log.txt");

                File.AppendAllText(logFile, $"{DateTime.Now}: {message}\n");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao gravar log: {ex.Message}");
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
        }
    }
}