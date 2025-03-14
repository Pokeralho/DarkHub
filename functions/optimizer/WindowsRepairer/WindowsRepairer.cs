using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class WindowsRepairer
    {
        private readonly Window _progressWindow;
        private readonly TextBox _progressTextBox;
        private readonly Button _button;

        public WindowsRepairer(Window owner, Button button)
        {
            _button = button;
            (_progressWindow, _progressTextBox) = WindowFactory.CreateProgressWindow(ResourceManagerHelper.Instance.RepairingWindowsTitle);
            _progressWindow.Owner = owner;
        }

        public async Task StartRepairAsync()
        {
            _button.IsEnabled = false;

            try
            {
                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() => _progressWindow.Show()));
                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.StartingWindowsRepair);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorCreatingProgressWindow, ex.Message),
                                ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                _button.IsEnabled = true;
                return;
            }

            try
            {
                await Task.Run(async () =>
                {
                    var commands = new List<(string Description, string Command)>
                    {
                        (ResourceManagerHelper.Instance.RunningSFC, "sfc /scannow"),
                        (ResourceManagerHelper.Instance.RunningDISM, "dism /online /cleanup-image /restorehealth"),
                        (ResourceManagerHelper.Instance.SchedulingCHKDSK, "chkdsk /f /r")
                    };

                    foreach (var (description, command) in commands)
                    {
                        WindowFactory.AppendProgress(_progressTextBox, description);
                        string result = await Task.Run(() => WindowFactory.ExecuteCommandWithOutput(command, _progressTextBox));

                        if (command.Contains("chkdsk") && (result.Contains("agendada") || result.Contains("scheduled")))
                        {
                            WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.CheckScheduled);
                        }

                        WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.TaskCompleted);
                        await Task.Delay(100);
                    }
                });

                WindowFactory.AppendProgress(_progressTextBox, ResourceManagerHelper.Instance.RepairsCompleted);

                var owner = _progressWindow.Owner;

                WindowState ownerState = WindowState.Normal;
                bool isOwnerVisible = false;

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    if (owner != null && owner.IsLoaded)
                    {
                        ownerState = owner.WindowState;
                        isOwnerVisible = owner.IsVisible;
                    }

                    MessageBox.Show(ResourceManagerHelper.Instance.RepairCommandsExecuted,
                        ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);

                    if (owner != null && owner.IsLoaded)
                    {
                        if (isOwnerVisible)
                        {
                            owner.WindowState = ownerState;
                            owner.Activate();
                            owner.Focus();
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(_progressTextBox, string.Format(ResourceManagerHelper.Instance.GeneralRepairError, ex.Message));

                var owner = _progressWindow.Owner;
                WindowState ownerState = WindowState.Normal;
                bool isOwnerVisible = false;

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    if (owner != null && owner.IsLoaded)
                    {
                        ownerState = owner.WindowState;
                        isOwnerVisible = owner.IsVisible;
                    }

                    MessageBox.Show(string.Format(ResourceManagerHelper.Instance.GeneralRepairError, ex.Message),
                        ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);

                    if (owner != null && owner.IsLoaded)
                    {
                        if (isOwnerVisible)
                        {
                            owner.WindowState = ownerState;
                            owner.Activate();
                            owner.Focus();
                        }
                    }
                }));
            }
            finally
            {
                _button.IsEnabled = true;

                var owner = _progressWindow.Owner;
                WindowState ownerState = WindowState.Normal;
                bool isOwnerVisible = false;

                if (owner != null && owner.IsLoaded)
                {
                    ownerState = owner.WindowState;
                    isOwnerVisible = owner.IsVisible;
                }

                await Task.Run(() => _progressWindow.Dispatcher.Invoke(() =>
                {
                    _progressWindow.Close();

                    if (owner != null && owner.IsLoaded && isOwnerVisible)
                    {
                        owner.WindowState = ownerState;
                        owner.Activate();
                        owner.Focus();
                    }
                }));
            }
        }
    }
}