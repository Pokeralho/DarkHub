using DarkHub.UI;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub
{
    public partial class Optimizer : Page
    {
        public Optimizer()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorInitializingOptimizer, ex.Message + "\nStackTrace: " + ex.StackTrace),
                ResourceManagerHelper.Instance.CriticalErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DisableUnnecessaryServices(object sender, RoutedEventArgs e)
        {
            var serviceManagerWindow = new ServiceManagerWindow(Window.GetWindow(this));
            serviceManagerWindow.ShowDialog();
        }

        private async void SystemInfo(object sender, RoutedEventArgs e)
        {
            try
            {
                var systemInfoWindow = new SystemInfoWindow(Window.GetWindow(this));
                await systemInfoWindow.ShowDialogAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorShowingInfoWindow, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ClearTempFilesAndLogs(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var cleaner = new TempFilesCleaner(Window.GetWindow(this), button);
            await cleaner.StartCleanupAsync();
        }

        private void DisableVisualEffects(object sender, RoutedEventArgs e)
        {
            var manager = new VisualEffectsManager(Window.GetWindow(this));
            manager.ShowDialog();
        }

        private async void RepairWindows(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var repairer = new WindowsRepairer(Window.GetWindow(this), button);
            await repairer.StartRepairAsync();
        }

        private async void EnableHighPerformanceMode(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var activator = new HighPerformanceModeActivator(Window.GetWindow(this), button);
            await activator.ActivateHighPerformanceModeAsync();
        }

        private async void AdjustTimerResolution(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var adjuster = new TimerResolutionAdjuster(Window.GetWindow(this), button);
            await adjuster.AdjustTimerResolutionAsync();
        }

        private async void ChangePriority(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var changer = new ProcessPriorityChanger(Window.GetWindow(this), button);
            await changer.ChangePriorityAsync();
        }

        private async void UninstallProgram(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var uninstaller = new ProgramUninstaller(Window.GetWindow(this), button);
            await uninstaller.UninstallProgramAsync();
        }

        private async void AtivarWindows(object sender, RoutedEventArgs e)
        {
            try
            {
                string comando = "irm https://massgrave.dev/get | iex";
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoExit -Command \"{comando}\"",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                await Task.Run(() => Process.Start(psi));
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorActivatingWindows, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CleanRegistry(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var cleaner = new RegistryCleaner(Window.GetWindow(this), button);
            await cleaner.CleanRegistryAsync();
        }

        private async void ManageStartupPrograms(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var manager = new StartupProgramManager(Window.GetWindow(this), button);
            await manager.ManageStartupProgramsAsync();
        }

        private async void CleanNetworkData(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var cleaner = new NetworkDataCleaner(Window.GetWindow(this), button);
            await cleaner.CleanNetworkDataAsync();
        }

        private async void RunSpaceSniffer(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var runner = new SpaceSnifferRunner(Window.GetWindow(this), button);
            await runner.RunSpaceSnifferAsync();
        }

        private void OpenWindowsDefender(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("C:\\Windows\\System32\\mrt.exe");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorOpeningMRT, ex.Message),
                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveWindowsBloatware(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var remover = new BloatwareRemover(Window.GetWindow(this), button);
            await remover.RemoveWindowsBloatwareAsync();
        }

        private async void OptimizeMemory(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var optimizer = new MemoryOptimizer(Window.GetWindow(this), button);
            await optimizer.OptimizeMemoryAsync();
        }

        private async void DNSBenchmark(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var benchmarker = new DNSBenchmarker(Window.GetWindow(this), button);
            await benchmarker.BenchmarkDNSAsync();
        }

        private async void OptimizeGameRoute(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var optimizer = new GameRouteOptimizer(Window.GetWindow(this), button);
            await optimizer.OptimizeGameRouteAsync();
        }

        private async void OptimizeAdvancedNetworkSettings(object sender, RoutedEventArgs e)
        {
            Button? button = sender as Button;
            if (button == null)
            {
                return;
            }

            var optimizer = new AdvancedNetworkOptimizer(Window.GetWindow(this), button);
            await optimizer.OptimizeAdvancedNetworkSettingsAsync();
        }
    }
}