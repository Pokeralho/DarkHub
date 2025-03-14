using Microsoft.Win32;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class ServiceManagerWindow
    {
        private readonly Window _servicesWindow;

        public ServiceManagerWindow(Window owner)
        {
            _servicesWindow = WindowFactory.CreateWindow(
                title: ResourceManagerHelper.Instance.DisableServicesTitle,
                width: 900,
                height: 600,
                owner: owner
            );

            InitializeUI();
        }

        private void InitializeUI()
        {
            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            stackPanel.Children.Add(WindowFactory.CreateTitleText(ResourceManagerHelper.Instance.DisableServices));

            var listBox = WindowFactory.CreateStyledListBox(height: 400);
            var unnecessaryServices = GetUnnecessaryServices();

            foreach (var service in unnecessaryServices)
            {
                string statusText = service.Status == ServiceControllerStatus.Running ? ResourceManagerHelper.Instance.Enabled : ResourceManagerHelper.Instance.Disabled;
                var checkBox = WindowFactory.CreateStyledCheckBox(
                    content: $"{service.DisplayName} ({service.ServiceName}) {statusText}",
                    tag: service.ServiceName,
                    isChecked: false
                );
                listBox.Items.Add(checkBox);
            }

            stackPanel.Children.Add(listBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var disableButton = WindowFactory.CreateStyledButton(
                content: ResourceManagerHelper.Instance.DisableSelected,
                width: 200,
                clickHandler: (s, ev) =>
                {
                    var selectedServices = listBox.Items.Cast<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Tag.ToString())
                        .ToList();
                    DisableSelectedServices(selectedServices);
                    _servicesWindow.Close();
                }
            );

            var enableButton = WindowFactory.CreateStyledButton(
                content: ResourceManagerHelper.Instance.EnableSelected,
                width: 200,
                clickHandler: (s, ev) =>
                {
                    var selectedServices = listBox.Items.Cast<CheckBox>()
                        .Where(cb => cb.IsChecked == true)
                        .Select(cb => cb.Tag.ToString())
                        .ToList();
                    EnableSelectedServices(selectedServices);
                    _servicesWindow.Close();
                }
            );

            var cancelButton = WindowFactory.CreateStyledButton(
                content: ResourceManagerHelper.Instance.Cancel,
                width: 100,
                clickHandler: (s, ev) => _servicesWindow.Close()
            );

            buttonPanel.Children.Add(disableButton);
            buttonPanel.Children.Add(enableButton);
            buttonPanel.Children.Add(cancelButton);
            stackPanel.Children.Add(buttonPanel);

            _servicesWindow.Content = stackPanel;
        }

        public void ShowDialog()
        {
            _servicesWindow.ShowDialog();
        }

        private static List<ServiceController> GetUnnecessaryServices()
        {
            var allServices = ServiceController.GetServices();
            var unnecessaryServiceNames = new List<string>
            {
                "WSearch", "SysMain", "WbioSrvc", "MapsBroker", "dmwappushservice",
                "DiagTrack", "GeolocationSvc", "RetailDemo", "XblAuthManager", "XblGameSave",
                "XboxGipSvc", "XboxNetApiSvc", "GamingServices", "GameBarPresenceWriter",
                "PcaSvc", "diagnosticshub.standardcollector.service", "WerSvc", "Fax",
                "Spooler", "TabletInputService", "SensrSvc", "HomeGroupListener",
                "HomeGroupProvider", "RemoteRegistry", "SSDPSRV", "upnphost", "wuauserv",
                "OneSyncSvc", "PushToInstall"
            };
            return allServices
                .Where(s => unnecessaryServiceNames.Contains(s.ServiceName))
                .ToList();
        }

        private static void DisableSelectedServices(List<string> serviceNames)
        {
            try
            {
                foreach (var serviceName in serviceNames)
                {
                    using var service = new ServiceController(serviceName);
                    if (service.Status != ServiceControllerStatus.Stopped)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                    }
                    using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", writable: true);
                    if (key != null)
                    {
                        key.SetValue("Start", 4, RegistryValueKind.DWord);
                    }
                }

                MessageBox.Show(ResourceManagerHelper.Instance.SelectedServicesDisabled,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.NoAccessToDisableService + " " + ex.Message,
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.NoAccessToDisableService + " " + ex.Message,
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void EnableSelectedServices(List<string> serviceNames)
        {
            try
            {
                foreach (var serviceName in serviceNames)
                {
                    using var service = new ServiceController(serviceName);
                    if (service.Status == ServiceControllerStatus.Stopped)
                    {
                        using var key = Registry.LocalMachine.OpenSubKey($@"SYSTEM\CurrentControlSet\Services\{serviceName}", writable: true);
                        if (key != null)
                        {
                            key.SetValue("Start", 2, RegistryValueKind.DWord);
                        }

                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                    }
                }

                MessageBox.Show(ResourceManagerHelper.Instance.SelectedServicesEnabled,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.NoAccessToEnableService + " " + ex.Message,
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ResourceManagerHelper.Instance.NoAccessToEnableService + " " + ex.Message,
                    ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}