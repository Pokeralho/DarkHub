using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;

namespace DarkHub
{
    public partial class AutoClicker : Page, IDisposable
    {
        private bool _isClicking = false;
        private int _clickIntervalMs = 1;
        private readonly object _syncLock = new();
        private CancellationTokenSource? _clickCancellationTokenSource;
        private IntPtr _hWnd;
        private HwndSource? _hwndSource;
        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312;
        private const int DEBOUNCE_DELAY_MS = 500;
        private Key _currentActivationKey = Key.F6;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type;
            public MOUSEINPUT mi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int INPUT_MOUSE = 0;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public AutoClicker()
        {
            InitializeComponent();
            SetupUIComponents();
            Loaded += AutoClicker_Loaded;
            Log(ResourceManagerHelper.Instance.AutoClickerInitialized);
        }

        private void SetupUIComponents()
        {
            try
            {
                cmbActivationKey.SelectedIndex = 0;
                txtInterval.Text = _clickIntervalMs.ToString();
                txtInterval.TextChanged += TxtInterval_TextChanged;
                btnStart.Click += StartClicking;
                btnStop.Click += StopClicking;
                cmbActivationKey.SelectionChanged += CmbActivationKey_SelectionChanged;
                UpdateUIStatus();
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorSettingUpUIComponents, ex);
            }
        }

        private void AutoClicker_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _hWnd = new WindowInteropHelper(Window.GetWindow(this)).Handle;
                _hwndSource = HwndSource.FromHwnd(_hWnd);
                if (_hwndSource != null)
                {
                    _hwndSource.AddHook(HwndHook);
                    RegisterHotkey();
                }
                Log(ResourceManagerHelper.Instance.HotkeyRegisteredSuccess);
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorSettingUpHotkey, ex);
            }
        }

        private void StartClicking(object? sender, RoutedEventArgs? e)
        {
            lock (_syncLock)
            {
                if (_isClicking) return;

                try
                {
                    ValidateAndUpdateInterval();
                    if (_clickIntervalMs <= 0)
                    {
                        ShowWarning("IntervalMustBeGreaterThanZero");
                        return;
                    }

                    _isClicking = true;
                    _clickCancellationTokenSource = new CancellationTokenSource();
                    Task.Run(() => ClickLoop(_clickCancellationTokenSource.Token));

                    UpdateUIStatus();
                    Log(ResourceManagerHelper.Instance.AutoClickerStarted);
                }
                catch (Exception ex)
                {
                    _isClicking = false;
                    HandleError(ResourceManagerHelper.Instance.ErrorStartingAutoClicker, ex);
                }
            }
        }

        private void StopClicking(object? sender, RoutedEventArgs? e)
        {
            lock (_syncLock)
            {
                if (!_isClicking) return;

                try
                {
                    _isClicking = false;
                    _clickCancellationTokenSource?.Cancel();
                    UpdateUIStatus();
                    Log(ResourceManagerHelper.Instance.AutoClickerStopped);
                }
                catch (Exception ex)
                {
                    HandleError(ResourceManagerHelper.Instance.ErrorStoppingAutoClicker, ex);
                }
            }
        }

        private void ClickLoop(CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                long elapsedMs = stopwatch.ElapsedMilliseconds;
                if (elapsedMs >= _clickIntervalMs)
                {
                    lock (_syncLock)
                    {
                        if (!_isClicking) break;
                        SimulateClick();
                    }
                    stopwatch.Restart();
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        private void SimulateClick()
        {
            INPUT[] inputs = new INPUT[2];

            inputs[0].type = INPUT_MOUSE;
            inputs[0].mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN };

            inputs[1].type = INPUT_MOUSE;
            inputs[1].mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP };

            SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            try
            {
                if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
                {
                    lock (_syncLock)
                    {
                        if (_isClicking)
                            StopClicking(null, null);
                        else
                            StartClicking(null, null);
                    }
                    handled = true;
                    Log(string.Format(ResourceManagerHelper.Instance.HotkeyActivated, _currentActivationKey));
                }
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorProcessingHotkey, ex);
            }
            return IntPtr.Zero;
        }

        private void RegisterHotkey()
        {
            try
            {
                if (_hWnd == IntPtr.Zero) return;

                UnregisterHotKey(_hWnd, HOTKEY_ID);

                Key activationKey = GetActivationKey();
                if (activationKey != Key.None)
                {
                    uint vk = (uint)KeyInterop.VirtualKeyFromKey(activationKey);
                    if (!RegisterHotKey(_hWnd, HOTKEY_ID, 0, vk))
                        throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Falha ao registrar hotkey.");
                    _currentActivationKey = activationKey;
                    Log(string.Format(ResourceManagerHelper.Instance.HotkeyRegistered, _currentActivationKey));
                }
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorRegisteringHotkey, ex);
            }
        }

        private void CmbActivationKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                RegisterHotkey();
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorChangingActivationKey, ex);
            }
        }

        private void TxtInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                lock (_syncLock)
                {
                    int newInterval = ValidateAndUpdateInterval();
                    if (newInterval <= 0)
                    {
                        ShowWarning("IntervalMustBeGreaterThanZero");
                        return;
                    }

                    if (_isClicking && newInterval != _clickIntervalMs)
                    {
                        _clickIntervalMs = newInterval;
                        UpdateUIStatus();
                        Log(string.Format(ResourceManagerHelper.Instance.IntervalUpdated, _clickIntervalMs));
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorProcessingIntervalChange, ex);
            }
        }

        private int ValidateAndUpdateInterval()
        {
            try
            {
                if (int.TryParse(txtInterval.Text, out int value) && value > 0)
                {
                    _clickIntervalMs = value;
                    return value;
                }

                ShowWarning("IntervalMustBeGreaterThanZero");
                return _clickIntervalMs;
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorValidatingInterval, ex);
                return _clickIntervalMs;
            }
        }

        private Key GetActivationKey()
        {
            try
            {
                if (cmbActivationKey.SelectedItem is ComboBoxItem item && item.Content is string keyName)
                    return (Key)Enum.Parse(typeof(Key), keyName);

                ShowWarning(ResourceManagerHelper.Instance.SelectValidActivationKey);
                return Key.None;
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorGettingActivationKey, ex);
                return Key.None;
            }
        }

        private void UpdateUIStatus()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    lblStatus.Text = _isClicking ? ResourceManagerHelper.Instance.ClickerStatusOn : ResourceManagerHelper.Instance.ClickerStatusOff;
                    btnStart.IsEnabled = !_isClicking;
                    btnStop.IsEnabled = _isClicking;
                    double cps = _clickIntervalMs > 0 ? 1000.0 / _clickIntervalMs : 0;
                    lblClicksPerSecond.Text = string.Format(ResourceManagerHelper.Instance.ClicksPerSecondLabel, cps);
                });
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorUpdatingUIStatus, ex);
            }
        }

        private void ShowWarning(string messageKey)
        {
            string translatedMessage = messageKey;
            try
            {
                translatedMessage = typeof(ResourceManagerHelper).GetProperty(messageKey)?.GetValue(ResourceManagerHelper.Instance) as string ?? messageKey;
            }
            catch
            {
            }

            Dispatcher.Invoke(() =>
                MessageBox.Show(translatedMessage, ResourceManagerHelper.Instance.WarningTitle, MessageBoxButton.OK, MessageBoxImage.Warning)
            );
        }

        private void HandleError(string contextKey, Exception ex)
        {
            string translatedContext = contextKey;
            try
            {
                translatedContext = typeof(ResourceManagerHelper).GetProperty(contextKey)?.GetValue(ResourceManagerHelper.Instance) as string ?? contextKey;
            }
            catch
            {
            }

            string errorMessage = $"{translatedContext}: {ex.Message}\nStackTrace: {ex.StackTrace}";
            Log(errorMessage);
            Dispatcher.Invoke(() =>
                MessageBox.Show(errorMessage, ResourceManagerHelper.Instance.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error)
            );
        }

        private void Log(string message)
        {
            Debug.WriteLine($"[AutoClicker] {DateTime.Now:HH:mm:ss} - {message}");
        }

        public void Dispose()
        {
            try
            {
                lock (_syncLock)
                {
                    if (_isClicking)
                    {
                        _clickCancellationTokenSource?.Cancel();
                        _isClicking = false;
                    }
                    _clickCancellationTokenSource?.Dispose();
                    _clickCancellationTokenSource = null;
                    Log(ResourceManagerHelper.Instance.TimerDisposed);
                }

                if (_hWnd != IntPtr.Zero)
                {
                    if (!UnregisterHotKey(_hWnd, HOTKEY_ID))
                        Log(string.Format(ResourceManagerHelper.Instance.HotkeyUnregisterFailed, Marshal.GetLastWin32Error()));
                    else
                        Log(ResourceManagerHelper.Instance.HotkeyUnregisteredSuccess);
                    _hwndSource?.RemoveHook(HwndHook);
                    _hWnd = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                HandleError(ResourceManagerHelper.Instance.ErrorDisposingAutoClickerResources, ex);
            }
        }
    }
}