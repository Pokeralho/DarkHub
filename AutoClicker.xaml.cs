using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace DarkHub
{
    public partial class AutoClicker : Page, IDisposable
    {
        private bool _isClicking = false;
        private int _clickIntervalMs = 100;
        private readonly object _syncLock = new();
        private DispatcherTimer? _clickTimer;
        private DispatcherTimer? _debounceTimer;
        private IntPtr _hWnd;
        private HwndSource? _hwndSource;
        private const int HOTKEY_ID = 9000;
        private const int WM_HOTKEY = 0x0312; 
        private const int DEBOUNCE_DELAY_MS = 500;
        private Key _currentActivationKey = Key.F6;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        public AutoClicker()
        {
            InitializeComponent();
            SetupUIComponents();
            SetupTimers();
            Loaded += AutoClicker_Loaded;
            Log("AutoClicker inicializado com sucesso.");
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
                HandleError("Erro ao configurar componentes da UI", ex);
            }
        }

        private void SetupTimers()
        {
            try
            {
                _clickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_clickIntervalMs) };
                _clickTimer.Tick += PerformClick;

                _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(DEBOUNCE_DELAY_MS) };
                _debounceTimer.Tick += ProcessIntervalChange;
            }
            catch (Exception ex)
            {
                HandleError("Erro ao configurar timers", ex);
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
                Log("Hotkey registrado com sucesso.");
            }
            catch (Exception ex)
            {
                HandleError("Erro ao configurar hotkey", ex);
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
                        ShowWarning("O intervalo deve ser maior que 0!");
                        return;
                    }

                    _isClicking = true;
                    if (_clickTimer != null)
                    {
                        _clickTimer.Interval = TimeSpan.FromMilliseconds(_clickIntervalMs);
                        _clickTimer.Start();
                    }
                    UpdateUIStatus();
                    Log("AutoClicker iniciado.");
                }
                catch (Exception ex)
                {
                    _isClicking = false;
                    HandleError("Erro ao iniciar o AutoClicker", ex);
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
                    _clickTimer?.Stop();
                    UpdateUIStatus();
                    Log("AutoClicker parado.");
                }
                catch (Exception ex)
                {
                    HandleError("Erro ao parar o AutoClicker", ex);
                }
            }
        }

        private void PerformClick(object? sender, EventArgs e)
        {
            try
            {
                lock (_syncLock)
                {
                    if (!_isClicking) return;
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(5);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                }
            }
            catch (Exception ex)
            {
                HandleError("Erro ao realizar clique", ex);
                StopClicking(null, null);
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) //por algum motivo estava apresentando crashs
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
                    Log($"Hotkey {_currentActivationKey} ativado.");
                }
            }
            catch (Exception ex)
            {
                HandleError("Erro ao processar hotkey", ex);
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
                    Log($"Hotkey registrado: {_currentActivationKey}");
                }
            }
            catch (Exception ex)
            {
                HandleError("Erro ao registrar hotkey", ex);
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
                HandleError("Erro ao trocar tecla de ativação", ex);
            }
        }

        private void TxtInterval_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceTimer?.Stop();
            _debounceTimer?.Start();
        }

        private void ProcessIntervalChange(object? sender, EventArgs e)
        {
            _debounceTimer?.Stop();
            try
            {
                lock (_syncLock)
                {
                    int newInterval = ValidateAndUpdateInterval();
                    if (newInterval <= 0)
                    {
                        ShowWarning("O intervalo deve ser maior que 0!");
                        return;
                    }

                    if (_isClicking && newInterval != _clickIntervalMs)
                    {
                        _clickIntervalMs = newInterval;
                        if (_clickTimer != null)
                        {
                            _clickTimer.Interval = TimeSpan.FromMilliseconds(_clickIntervalMs);
                        }
                        UpdateUIStatus();
                        Log($"Intervalo atualizado para {_clickIntervalMs}ms.");
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Erro ao processar mudança de intervalo", ex);
            }
        }

        private int ValidateAndUpdateInterval()
        {
            try
            {
                if (int.TryParse(txtInterval.Text, out int value) && value > 0)
                    return value;

                ShowWarning("Por favor, insira um valor válido maior que 0!");
                return _clickIntervalMs;
            }
            catch (Exception ex)
            {
                HandleError("Erro ao validar intervalo", ex);
                return _clickIntervalMs;
            }
        }

        private Key GetActivationKey()
        {
            try
            {
                if (cmbActivationKey.SelectedItem is ComboBoxItem item && item.Content is string keyName)
                    return (Key)Enum.Parse(typeof(Key), keyName);

                ShowWarning("Selecione uma tecla de ativação válida!");
                return Key.None;
            }
            catch (Exception ex)
            {
                HandleError("Erro ao obter tecla de ativação", ex);
                return Key.None;
            }
        }

        private void UpdateUIStatus()
        {
            try
            {
                Dispatcher.Invoke(() =>
                {
                    lblStatus.Text = _isClicking ? "Status: Clicando!" : "Status: Parado";
                    btnStart.IsEnabled = !_isClicking;
                    btnStop.IsEnabled = _isClicking;
                    double cps = _clickIntervalMs > 0 ? 1000.0 / _clickIntervalMs : 0;
                    lblClicksPerSecond.Text = $"Cliques por segundo: {cps:F2}";
                });
            }
            catch (Exception ex)
            {
                HandleError("Erro ao atualizar status da UI", ex);
            }
        }

        private void ShowWarning(string message)
        {
            Dispatcher.Invoke(() =>
                MessageBox.Show(message, "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning)
            );
        }

        private void HandleError(string context, Exception ex)
        {
            string errorMessage = $"{context}: {ex.Message}\nStackTrace: {ex.StackTrace}";
            Log(errorMessage);
            Dispatcher.Invoke(() =>
                MessageBox.Show(errorMessage, "Erro", MessageBoxButton.OK, MessageBoxImage.Error)
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
                    if (_clickTimer != null)
                    {
                        _clickTimer.Stop();
                        _clickTimer.Tick -= PerformClick;
                        _clickTimer = null;
                        Log("Timer de cliques descartado.");
                    }

                    if (_debounceTimer != null)
                    {
                        _debounceTimer.Stop();
                        _debounceTimer.Tick -= ProcessIntervalChange;
                        _debounceTimer = null;
                        Log("Timer de debounce descartado.");
                    }
                }

                if (_hWnd != IntPtr.Zero)
                {
                    if (!UnregisterHotKey(_hWnd, HOTKEY_ID))
                        Log($"Falha ao remover hotkey: {Marshal.GetLastWin32Error()}");
                    else
                        Log("Hotkey removido com sucesso.");
                    _hwndSource?.RemoveHook(HwndHook);
                    _hWnd = IntPtr.Zero;
                }
            }
            catch (Exception ex)
            {
                HandleError("Erro ao descartar recursos do AutoClicker", ex);
            }
        }
    }
}