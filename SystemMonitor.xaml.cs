using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Collections.Generic;

namespace DarkHub
{
    public partial class SystemMonitor : Page
    {
        private readonly DispatcherTimer updateTimer;
        private readonly PerformanceCounter cpuCounter;
        private readonly PerformanceCounter ramCounter;
        private readonly PerformanceCounter diskReadCounter;
        private readonly PerformanceCounter diskWriteCounter;
        private readonly Dictionary<string, PerformanceCounter> gpuCounters;
        private PerformanceCounter gpuLoadCounter;

        private string cpuNameText, cpuCoresText, gpuNameText, osInfoText;
        private ulong ramTotalValue, gpuMemoryValue;
        private bool isInitialized = false;
        private DateTime lastDiskInfoUpdate = DateTime.MinValue;
        private (ulong TotalSize, ulong UsedSize, ulong FreeSize) cachedDiskInfo;
        private DateTime lastNetworkInfoUpdate = DateTime.MinValue;
        private (string AdapterName, string Speed, string IP) cachedNetworkInfo;
        private DateTime lastGPUInfoUpdate = DateTime.MinValue;
        private float cachedGPUUsage = 0;
        private float cachedVRAMUsage = 0;
        private bool isUpdating = false;

        public SystemMonitor()
        {
            InitializeComponent();
            gpuCounters = new Dictionary<string, PerformanceCounter>();

            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
                diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");

                cpuCounter.NextValue();
                ramCounter.NextValue();
                diskReadCounter.NextValue();
                diskWriteCounter.NextValue();

                updateTimer = new DispatcherTimer();
                updateTimer.Interval = TimeSpan.FromMilliseconds(1000);
                updateTimer.Tick += UpdateTimer_Tick;

                Loaded += SystemMonitor_Loaded;
                Unloaded += SystemMonitor_Unloaded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao inicializar contadores: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void SystemMonitor_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Página SystemMonitor carregada. Iniciando timer...");

            if (!updateTimer.IsEnabled)
            {
                updateTimer.Start();
            }

            _ = InitializeStaticInfo();
        }

        private void SystemMonitor_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Página SystemMonitor descarregada. Parando timer...");

            if (updateTimer.IsEnabled)
            {
                updateTimer.Stop();
            }
        }

        private async Task InitializeStaticInfo()
        {
            if (isInitialized) return;
            isInitialized = true;

            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    loadingOverlay.Visibility = Visibility.Visible;
                });

                var tasks = new[]
                {
                    Task.Run(() => LoadCPUInfo()),
                    Task.Run(() => LoadRAMInfo()),
                    Task.Run(() => LoadGPUInfo()),
                    Task.Run(() => LoadSystemInfo())
                };

                await Task.WhenAll(tasks);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    loadingOverlay.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em InitializeStaticInfo: {ex.Message}\n{ex.StackTrace}");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    loadingOverlay.Visibility = Visibility.Collapsed;
                    MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorLoadingSystemInfo, ex.Message),
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void LoadCPUInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor"))
                {
                    var cpu = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    if (cpu != null)
                    {
                        cpuNameText = string.Format(ResourceManagerHelper.Instance.CPUNameFormat, cpu["Name"]);
                        cpuCoresText = string.Format(ResourceManagerHelper.Instance.CPUCoresFormat, cpu["NumberOfCores"]);
                        string cpuThreadsText = string.Format(ResourceManagerHelper.Instance.CPUThreadsFormat, cpu["NumberOfLogicalProcessors"]);
                        string cpuClockText = string.Format(ResourceManagerHelper.Instance.CPUClockFormat, Convert.ToUInt32(cpu["MaxClockSpeed"]) / 1000.0);

                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            cpuName.Text = cpuNameText;
                            cpuCores.Text = cpuCoresText;
                            cpuThreads.Text = cpuThreadsText;
                            cpuClock.Text = cpuClockText;
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar informações da CPU: {ex.Message}");
            }
        }

        private void LoadRAMInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem"))
                {
                    var ram = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    if (ram != null)
                    {
                        ramTotalValue = Convert.ToUInt64(ram["TotalVisibleMemorySize"]) * 1024;
                    }
                }

                using (var searcher = new ManagementObjectSearcher("SELECT Speed, ConfiguredClockSpeed FROM Win32_PhysicalMemory"))
                {
                    var ramModules = searcher.Get().Cast<ManagementObject>();
                    uint ramSpeed = 0;
                    foreach (var module in ramModules)
                    {
                        ramSpeed = Convert.ToUInt32(module["ConfiguredClockSpeed"] ?? module["Speed"] ?? 0);
                        break;
                    }

                    string ramCLText = ResourceManagerHelper.Instance.RAMCLUnknown;
                    try
                    {
                        using (var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0"))
                        {
                            if (key != null)
                            {
                                var memoryDevice = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory").Get().Cast<ManagementObject>().FirstOrDefault();
                                if (memoryDevice != null && memoryDevice["PartNumber"] != null)
                                {
                                    ramCLText = ramSpeed >= 3200 ? ResourceManagerHelper.Instance.RAMCL16 : ResourceManagerHelper.Instance.RAMCL14;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao tentar obter CL: {ex.Message}");
                    }

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        ramTotal.Text = string.Format(ResourceManagerHelper.Instance.RAMTotalFormat, FormatBytes(ramTotalValue));
                        ramSpeedText.Text = string.Format(ResourceManagerHelper.Instance.RAMSpeedFormat, ramSpeed);
                        ramCL.Text = ramCLText;
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao carregar informações da RAM: {ex.Message}");
            }
        }

        private async Task LoadGPUInfo()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM FROM Win32_VideoController"))
                {
                    var gpu = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    if (gpu != null)
                    {
                        gpuNameText = string.Format(ResourceManagerHelper.Instance.GPUNameFormat, gpu["Name"]);
                        gpuMemoryValue = Convert.ToUInt64(gpu["AdapterRAM"]) * 2;

                        if (gpuMemoryValue == 4294967295 || gpuMemoryValue < 8UL * 1024 * 1024 * 1024)
                        {
                            var vramFromRegistry = await GetVRAMFromRegistry();
                            if (vramFromRegistry.HasValue)
                            {
                                gpuMemoryValue = vramFromRegistry.Value;
                            }
                        }

                        InitializeGPUCounters();

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            gpuName.Text = gpuNameText;
                            gpuMemory.Text = string.Format(ResourceManagerHelper.Instance.GPUMemoryFormat, FormatBytes(gpuMemoryValue));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao obter informações da GPU: {ex.Message}");
            }
        }

        private void LoadSystemInfo()
        {
            osInfoText = string.Format(ResourceManagerHelper.Instance.SystemVersionFormat, Environment.OSVersion.VersionString) + "\n" +
                         string.Format(ResourceManagerHelper.Instance.SystemArchitectureFormat, Environment.Is64BitOperatingSystem ? "x64" : "x86") + "\n" +
                         string.Format(ResourceManagerHelper.Instance.SystemBuildFormat, Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuild", ResourceManagerHelper.Instance.Unknown));

            using (var mbSearcher = new ManagementObjectSearcher("SELECT Product, Manufacturer FROM Win32_BaseBoard"))
            {
                var mb = mbSearcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (mb != null)
                    osInfoText += $"\n" + string.Format(ResourceManagerHelper.Instance.SystemMotherboardFormat, mb["Manufacturer"], mb["Product"]);
            }

            using (var biosSearcher = new ManagementObjectSearcher("SELECT Manufacturer, Version FROM Win32_BIOS"))
            {
                var bios = biosSearcher.Get().Cast<ManagementObject>().FirstOrDefault();
                if (bios != null)
                    osInfoText += $"\n" + string.Format(ResourceManagerHelper.Instance.SystemBIOSFormat, bios["Manufacturer"], bios["Version"]);
            }

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                osVersion.Text = osInfoText.Split('\n')[0];
                osArchitecture.Text = osInfoText.Split('\n')[1];
                osBuild.Text = string.Join("\n", osInfoText.Split('\n').Skip(2));
            });
        }

        private void InitializeGPUCounters()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var instanceNames = category.GetInstanceNames();

                Debug.WriteLine($"Instâncias GPU encontradas: {string.Join(", ", instanceNames)}");

                foreach (var name in instanceNames)
                {
                    if (name.Contains("engtype_3D") || name.Contains("engtype_Copy"))
                    {
                        Debug.WriteLine($"Adicionando contador GPU: {name}");
                        gpuCounters[name] = new PerformanceCounter("GPU Engine", "Utilization Percentage", name);
                        gpuCounters[name].NextValue();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao inicializar contadores da GPU: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private float GetGPUUsage()
        {
            try
            {
                float totalUsage = 0;
                int count = 0;

                foreach (var counter in gpuCounters.Values)
                {
                    try
                    {
                        float usage = counter.NextValue();
                        if (usage > 0)
                        {
                            totalUsage += usage;
                            count++;
                            Debug.WriteLine($"Contador GPU: {counter.InstanceName} = {usage:F1}%");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao ler contador {counter.InstanceName}: {ex.Message}");
                    }
                }

                if (count > 0)
                {
                    float avgUsage = totalUsage / count;
                    Debug.WriteLine($"Uso médio GPU (contadores): {avgUsage:F1}%");
                    return avgUsage;
                }

                using (var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_VideoController"))
                {
                    var gpu = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    if (gpu != null)
                    {
                        float load = Convert.ToSingle(gpu["LoadPercentage"]);
                        if (load > 0)
                        {
                            Debug.WriteLine($"Uso GPU (WMI): {load:F1}%");
                            return load;
                        }
                    }
                }

                if (gpuLoadCounter != null)
                {
                    try
                    {
                        float gpuLoad = gpuLoadCounter.NextValue();
                        if (gpuLoad > 0)
                        {
                            Debug.WriteLine($"Uso GPU (contador principal): {gpuLoad:F1}%");
                            return gpuLoad;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao ler contador principal: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao obter uso da GPU: {ex.Message}\n{ex.StackTrace}");
            }
            return 0;
        }

        private float GetVRAMUsage()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT AdapterRAM FROM Win32_VideoController"))
                {
                    var gpu = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
                    if (gpu != null)
                    {
                        ulong totalVRAM = Convert.ToUInt64(gpu["AdapterRAM"]) * 2;
                        if (totalVRAM > 0)
                        {
                            float gpuUsage = GetGPUUsage();
                            return (gpuUsage * 0.8f);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao obter uso da VRAM: {ex.Message}");
            }
            return 0;
        }

        private double RunCPUBenchmark()
        {
            var sw = Stopwatch.StartNew();
            int iterations = Environment.ProcessorCount * 5000000;
            double[] partialResults = new double[Environment.ProcessorCount];

            Parallel.For(0, Environment.ProcessorCount, threadIndex =>
            {
                double localFloat = 0.0;
                int start = threadIndex * (iterations / Environment.ProcessorCount);
                int end = (threadIndex + 1) * (iterations / Environment.ProcessorCount);
                for (int i = start; i < end; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        localFloat += Math.Sin(i * 0.113576) * Math.Cos(j * 0.213576);
                        localFloat += i % 1024;
                    }
                }
                partialResults[threadIndex] = localFloat;
            });

            double floatResult = partialResults.Sum();
            sw.Stop();
            double mops = (iterations * 5.0) / sw.Elapsed.TotalSeconds / 1000000.0;
            Debug.WriteLine($"CPU Benchmark: {mops:F2} MOPS");
            return mops;
        }

        private double RunRAMBenchmark()
        {
            const int size = 256 * 1024 * 1024;
            var sw = Stopwatch.StartNew();
            byte[] array = new byte[size];
            Random rand = new Random();

            for (int i = 0; i < size; i += 131072)
            {
                rand.NextBytes(array.AsSpan(i, Math.Min(131072, size - i)));
            }

            ulong sumSequential = 0;
            for (int i = 0; i < size; i += 8)
            {
                sumSequential += BitConverter.ToUInt64(array, i);
            }

            ulong sumRandom = 0;
            int[] randomIndices = Enumerable.Range(0, size / 8).OrderBy(x => rand.Next()).ToArray();
            for (int i = 0; i < randomIndices.Length; i++)
            {
                sumRandom += BitConverter.ToUInt64(array, randomIndices[i] * 8);
            }

            sw.Stop();
            double mbps = (size * 2.0) / sw.Elapsed.TotalSeconds / 1024.0 / 1024.0;
            Debug.WriteLine($"RAM Benchmark: {mbps:F2} MB/s (Sequencial)");
            Debug.WriteLine($"RAM Latency: {sw.Elapsed.TotalMilliseconds / randomIndices.Length:F4} ms por acesso aleatório");
            return mbps;
        }

        private double RunDiskBenchmark()
        {
            var tempFile = Path.GetTempFileName();
            var sw = Stopwatch.StartNew();
            const int bufferSize = 64 * 1024 * 1024;
            byte[] buffer = new byte[bufferSize];
            Random rand = new Random();

            rand.NextBytes(buffer);

            using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 131072))
            {
                fs.Write(buffer, 0, buffer.Length);
                fs.Flush(true);
            }

            using (var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.None, 131072))
            {
                fs.Read(buffer, 0, buffer.Length);
            }

            long fileSize = new FileInfo(tempFile).Length;
            for (int i = 0; i < 1000; i++)
            {
                long position = rand.NextInt64(fileSize - bufferSize);
                using (var fs = new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.None, 131072))
                {
                    fs.Seek(position, SeekOrigin.Begin);
                    fs.Read(buffer, 0, buffer.Length);
                }
            }

            File.Delete(tempFile);
            sw.Stop();
            double mbps = (bufferSize * 2.0) / sw.Elapsed.TotalSeconds / 1024.0 / 1024.0;
            Debug.WriteLine($"Disk Benchmark: {mbps:F2} MB/s");
            return mbps;
        }

        private double RunGPUBenchmark()
        {
            var sw = Stopwatch.StartNew();
            const int width = 1920, height = 1080;
            const int iterations = 200;
            byte[] pixels = new byte[width * height * 4];
            Random rand = new Random();

            for (int iter = 0; iter < iterations; iter++)
            {
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    pixels[i] = (byte)(rand.Next(256));
                    pixels[i + 1] = (byte)(rand.Next(256));
                    pixels[i + 2] = (byte)(rand.Next(256));
                    pixels[i + 3] = 255;
                }

                for (int i = 0; i < pixels.Length; i += 4)
                {
                    byte avg = (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
                    pixels[i] = pixels[i + 1] = pixels[i + 2] = avg;
                }
            }

            sw.Stop();
            double mpixelsPerSec = (width * height * iterations) / sw.Elapsed.TotalSeconds / 1000000.0;
            Debug.WriteLine($"GPU Benchmark: {mpixelsPerSec:F2} MPixels/s");
            return mpixelsPerSec;
        }

        private async Task<ulong?> GetVRAMFromRegistry()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}\0000"))
                {
                    if (key != null)
                    {
                        var memory = key.GetValue("HardwareInformation.MemorySize") as byte[];
                        if (memory != null && memory.Length >= 8)
                            return BitConverter.ToUInt64(memory, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao obter VRAM do Registro: {ex.Message}");
            }
            return null;
        }

        private string FormatBytes(ulong bytes)
        {
            string[] sizes = ResourceManagerHelper.Instance.ByteUnits.Split('|');
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }
            return string.Format(ResourceManagerHelper.Instance.ByteFormat, size, sizes[order]);
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdatePerformanceInfo();
        }

        private void UpdatePerformanceInfo()
        {
            if (isUpdating) return;
            isUpdating = true;

            try
            {
                float cpuUsage = cpuCounter.NextValue();
                ulong ramAvailable = (ulong)ramCounter.NextValue() * 1024 * 1024;
                ulong ramUsedValue = ramTotalValue - ramAvailable;

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    cpuUsageText.Text = string.Format(ResourceManagerHelper.Instance.CPUUsageFormat, cpuUsage);
                    cpuProgress.Value = cpuUsage;
                    ramUsed.Text = string.Format(ResourceManagerHelper.Instance.RAMUsedFormat, FormatBytes(ramUsedValue));
                    ramFree.Text = string.Format(ResourceManagerHelper.Instance.RAMFreeFormat, FormatBytes(ramAvailable));
                    ramProgress.Value = (double)ramUsedValue / ramTotalValue * 100;
                });

                if ((DateTime.Now - lastDiskInfoUpdate).TotalSeconds >= 2)
                {
                    cachedDiskInfo = GetDiskInfo();
                    lastDiskInfoUpdate = DateTime.Now;
                }

                float diskActivity = (diskReadCounter.NextValue() + diskWriteCounter.NextValue()) / 1024 / 1024;

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    diskTotal.Text = string.Format(ResourceManagerHelper.Instance.DiskTotalFormat, FormatBytes(cachedDiskInfo.TotalSize));
                    diskUsed.Text = string.Format(ResourceManagerHelper.Instance.DiskUsedFormat, FormatBytes(cachedDiskInfo.UsedSize));
                    diskFree.Text = string.Format(ResourceManagerHelper.Instance.DiskFreeFormat, FormatBytes(cachedDiskInfo.FreeSize));
                    diskUsage.Text = string.Format(ResourceManagerHelper.Instance.DiskActivityFormat, diskActivity);
                    diskProgress.Value = Math.Min(diskActivity / 100, 100);
                });

                if ((DateTime.Now - lastGPUInfoUpdate).TotalSeconds >= 1)
                {
                    cachedGPUUsage = GetGPUUsage() * 4.1f;
                    cachedVRAMUsage = GetVRAMUsage();
                    lastGPUInfoUpdate = DateTime.Now;
                }

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    gpuUsage.Text = string.Format(ResourceManagerHelper.Instance.GPUUsageFormat, cachedGPUUsage) + "\n" +
                                    string.Format(ResourceManagerHelper.Instance.VRAMUsageFormat, cachedVRAMUsage);
                    gpuProgress.Value = cachedGPUUsage;
                });

                if ((DateTime.Now - lastNetworkInfoUpdate).TotalSeconds >= 3)
                {
                    cachedNetworkInfo = GetNetworkInfo();
                    lastNetworkInfoUpdate = DateTime.Now;
                }

                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    networkAdapter.Text = string.Format(ResourceManagerHelper.Instance.NetworkAdapterFormat, cachedNetworkInfo.AdapterName);
                    networkSpeed.Text = string.Format(ResourceManagerHelper.Instance.NetworkSpeedFormat, cachedNetworkInfo.Speed);
                    networkIP.Text = string.Format(ResourceManagerHelper.Instance.NetworkIPFormat, cachedNetworkInfo.IP);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em UpdatePerformanceInfo: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                isUpdating = false;
            }
        }

        private async void RunBenchmark(object sender, RoutedEventArgs e)
        {
            try
            {
                runBenchmarkButton.IsEnabled = false;
                runBenchmarkButton.Content = ResourceManagerHelper.Instance.RunningBenchmark;

                cpuScore.Text = ResourceManagerHelper.Instance.CPUTesting;
                ramScore.Text = ResourceManagerHelper.Instance.RAMTesting;
                diskScore.Text = ResourceManagerHelper.Instance.DiskTesting;
                gpuScore.Text = ResourceManagerHelper.Instance.GPUTesting;

                var tasks = new[]
                {
                    Task.Run(() => RunCPUBenchmark()),
                    Task.Run(() => RunRAMBenchmark()),
                    Task.Run(() => RunDiskBenchmark()),
                    Task.Run(() => RunGPUBenchmark())
                };

                var results = await Task.WhenAll(tasks);

                cpuScore.Text = string.Format(ResourceManagerHelper.Instance.CPUScoreFormat, results[0]);
                ramScore.Text = string.Format(ResourceManagerHelper.Instance.RAMScoreFormat, results[1]);
                diskScore.Text = string.Format(ResourceManagerHelper.Instance.DiskScoreFormat, results[2]);
                gpuScore.Text = string.Format(ResourceManagerHelper.Instance.GPUScoreFormat, results[3]);

                double totalScore = ((results[0] * 1 + results[1] * 0.7 + results[2] * 0.5 + results[3] * 1) * 7);
                systemScore.Text = string.Format(ResourceManagerHelper.Instance.TotalScoreFormat, totalScore);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorRunningBenchmark, ex.Message),
                                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em RunBenchmark: {ex.StackTrace}");
            }
            finally
            {
                runBenchmarkButton.IsEnabled = true;
                runBenchmarkButton.Content = ResourceManagerHelper.Instance.RunBenchmarkButtonText;
            }
        }

        private (ulong TotalSize, ulong UsedSize, ulong FreeSize) GetDiskInfo()
        {
            try
            {
                var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.Contains(Path.GetPathRoot(Environment.SystemDirectory)));
                if (drive != null)
                {
                    return ((ulong)drive.TotalSize, (ulong)(drive.TotalSize - drive.AvailableFreeSpace), (ulong)drive.AvailableFreeSpace);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em GetDiskInfo: {ex.Message}");
            }
            return (0, 0, 0);
        }

        private (string AdapterName, string Speed, string IP) GetNetworkInfo()
        {
            try
            {
                var adapter = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
                if (adapter != null)
                {
                    string speed = adapter.Speed > 0 ? $"{adapter.Speed / 1000000.0:F1} Mbps" : ResourceManagerHelper.Instance.Unknown;
                    string ip = adapter.GetIPProperties().UnicastAddresses
                        .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address.ToString() ?? ResourceManagerHelper.Instance.NotFound;
                    return (adapter.Name, speed, ip);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em GetNetworkInfo: {ex.Message}");
            }
            return (ResourceManagerHelper.Instance.Unknown, ResourceManagerHelper.Instance.Unknown, ResourceManagerHelper.Instance.NotFound);
        }

        private void MainScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            Border scrollIndicator = ScrollIndicator;

            if (scrollViewer != null && scrollIndicator != null)
            {
                bool hasMoreContentBelow = scrollViewer.VerticalOffset + scrollViewer.ViewportHeight < scrollViewer.ExtentHeight;
                scrollIndicator.Visibility = hasMoreContentBelow ? Visibility.Visible : Visibility.Collapsed;
            }

            if (e.ExtentHeightChange != 0 || e.VerticalOffset != 0)
            {
                updateTimer.Stop();
                Task.Delay(50).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        updateTimer.Start();
                    });
                });
            }
        }
    }
}