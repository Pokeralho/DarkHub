using System;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace DarkHubRmk
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                base.OnStartup(e);
            }

            base.OnStartup(e);

            LogToFile("Aplicação iniciada.");

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

            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
            {
                LogToFile("Aplicação encerrada.");
            };

            Dispatcher.UnhandledExceptionFilter += (sender, args) =>
            {
                LogToFile("Filtro de exceção no Dispatcher acionado.");
            };
        }

        private void LogToFile(string message)
        {
            try
            {
                File.AppendAllText("crash_log.txt", $"{DateTime.Now}: {message}\n");
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
                Current.Dispatcher.Invoke(action);
            }
        }
    }
}