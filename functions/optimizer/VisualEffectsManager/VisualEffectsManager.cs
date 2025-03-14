using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace DarkHub.UI
{
    public class VisualEffectsManager
    {
        private readonly Window _confirmationWindow;

        public VisualEffectsManager(Window owner)
        {
            _confirmationWindow = WindowFactory.CreateWindow(
                title: "Confirmação",
                width: 350,
                height: 180,
                owner: owner,
                isModal: true,
                resizable: false
            );

            InitializeUI();
        }

        private void InitializeUI()
        {
            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            stackPanel.Children.Add(new TextBlock
            {
                Text = "Deseja realmente desativar todos os efeitos visuais?",
                TextWrapping = TextWrapping.Wrap,
                Foreground = WindowFactory.DefaultTextForeground,
                FontFamily = WindowFactory.DefaultFontFamily,
                FontSize = WindowFactory.DefaultFontSize
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var applyButton = WindowFactory.CreateStyledButton("Aplicar", 80, async (s, ev) => await DisableVisualEffectsAsync());
            var cancelButton = WindowFactory.CreateStyledButton("Cancelar", 80, (s, ev) => _confirmationWindow.Close());
            var revertButton = WindowFactory.CreateStyledButton("Reverter", 80, async (s, ev) => await RevertVisualEffectsAsync());
            revertButton.Margin = new Thickness(10, 0, 0, 0);

            buttonPanel.Children.Add(applyButton);
            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(revertButton);
            stackPanel.Children.Add(buttonPanel);

            _confirmationWindow.Content = stackPanel;
        }

        public void ShowDialog()
        {
            _confirmationWindow.ShowDialog();
        }

        private async Task DisableVisualEffectsAsync()
        {
            try
            {
                const string DesktopKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop";
                const string WindowMetricsKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics";
                const string AdvancedExplorerKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                const string VisualEffectsKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
                const string DwmKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM";
                const string CursorsKeyPath = @"HKEY_CURRENT_USER\Control Panel\Cursors";
                const string AccessibilityKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Accessibility";

                var registrySettings = new Dictionary<(string Key, string Name), (object Value, RegistryValueKind Kind)>
                {
                    {(DesktopKeyPath, "UserPreferencesMask"), (new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary)},
                    {(VisualEffectsKeyPath, "VisualFXSetting"), (2, RegistryValueKind.DWord)},
                    {(WindowMetricsKeyPath, "MinAnimate"), ("0", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "TaskbarAnimations"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "EnableAeroPeek"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "AlwaysHibernateThumbnails"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "IconsOnly"), (1, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ListviewAlphaSelect"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DragFullWindows"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "FontSmoothing"), ("0", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "ListviewShadow"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "EnableTransparency"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "ColorizationOpaqueBlend"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DisableAnimations"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "Composition"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "MenuAnimation"), ("0", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "ExtendedUIHoverTime"), (10000, RegistryValueKind.DWord)},
                    {(CursorsKeyPath, "CursorShadow"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "SmoothScroll"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ListviewFade"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "WallpaperStyle"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "TileWallpaper"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "WallpaperSlideshow"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ShowInfoTip"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "ComboBoxAnimation"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "ListBoxSmoothScrolling"), ("0", RegistryValueKind.String)},
                    {(WindowMetricsKeyPath, "FadeFullScreen"), ("0", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "IconQuality"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "VisualSoundEffects"), (0, RegistryValueKind.DWord)},
                    {(AccessibilityKeyPath, "HighContrast"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DropShadow"), ("0", RegistryValueKind.String)}
                };

                await Task.Run(() =>
                {
                    Parallel.ForEach(registrySettings, setting =>
                    {
                        var (keyPath, name) = setting.Key;
                        var (newValue, kind) = setting.Value;
                        SetRegistryValue(keyPath, name, newValue, kind);
                    });

                    SystemParametersInfo(0x200, 0, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x1012, 0, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x101E, 0, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x1026, 0, IntPtr.Zero, 0x2);
                });

                MessageBox.Show(ResourceManagerHelper.Instance.VisualEffectsDisabledSuccess,
                    ResourceManagerHelper.Instance.SuccessTitle, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Acesso negado ao Registro: {ex.Message}", ResourceManagerHelper.Instance.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao desativar efeitos visuais: {ex.Message}", ResourceManagerHelper.Instance.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _confirmationWindow.Close();
            }
        }

        private async Task RevertVisualEffectsAsync()
        {
            try
            {
                const string DesktopKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop";
                const string WindowMetricsKeyPath = @"HKEY_CURRENT_USER\Control Panel\Desktop\WindowMetrics";
                const string AdvancedExplorerKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
                const string VisualEffectsKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects";
                const string DwmKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM";
                const string CursorsKeyPath = @"HKEY_CURRENT_USER\Control Panel\Cursors";
                const string AccessibilityKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Accessibility";

                var defaultSettings = new Dictionary<(string Key, string Name), (object Value, RegistryValueKind Kind)>
                {
                    {(DesktopKeyPath, "UserPreferencesMask"), (new byte[] { 0x9E, 0x3E, 0x07, 0x80, 0x12, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary)},
                    {(VisualEffectsKeyPath, "VisualFXSetting"), (0, RegistryValueKind.DWord)},
                    {(WindowMetricsKeyPath, "MinAnimate"), ("1", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "TaskbarAnimations"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "EnableAeroPeek"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "AlwaysHibernateThumbnails"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "IconsOnly"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ListviewAlphaSelect"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DragFullWindows"), ("1", RegistryValueKind.String)},
                    {(DesktopKeyPath, "FontSmoothing"), ("2", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "ListviewShadow"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "EnableTransparency"), (1, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "ColorizationOpaqueBlend"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DisableAnimations"), (0, RegistryValueKind.DWord)},
                    {(DwmKeyPath, "Composition"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "MenuAnimation"), ("1", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "ExtendedUIHoverTime"), (400, RegistryValueKind.DWord)},
                    {(CursorsKeyPath, "CursorShadow"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "SmoothScroll"), (1, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ListviewFade"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "WallpaperStyle"), ("10", RegistryValueKind.String)},
                    {(DesktopKeyPath, "TileWallpaper"), ("0", RegistryValueKind.String)},
                    {(DesktopKeyPath, "WallpaperSlideshow"), (0, RegistryValueKind.DWord)},
                    {(AdvancedExplorerKeyPath, "ShowInfoTip"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "ComboBoxAnimation"), ("1", RegistryValueKind.String)},
                    {(DesktopKeyPath, "ListBoxSmoothScrolling"), ("1", RegistryValueKind.String)},
                    {(WindowMetricsKeyPath, "FadeFullScreen"), ("1", RegistryValueKind.String)},
                    {(AdvancedExplorerKeyPath, "IconQuality"), (1, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "VisualSoundEffects"), (1, RegistryValueKind.DWord)},
                    {(AccessibilityKeyPath, "HighContrast"), (0, RegistryValueKind.DWord)},
                    {(DesktopKeyPath, "DropShadow"), ("1", RegistryValueKind.String)}
                };

                await Task.Run(() =>
                {
                    Parallel.ForEach(defaultSettings, setting =>
                    {
                        var (keyPath, name) = setting.Key;
                        var (defaultValue, kind) = setting.Value;
                        SetRegistryValue(keyPath, name, defaultValue, kind);
                    });

                    SystemParametersInfo(0x200, 1, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x1012, 1, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x101E, 1, IntPtr.Zero, 0x2);
                    SystemParametersInfo(0x1026, 1, IntPtr.Zero, 0x2);
                });

                MessageBox.Show("Efeitos visuais restaurados para os padrões do Windows!", ResourceManagerHelper.Instance.SuccessTitle,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Acesso negado ao Registro ao reverter: {ex.Message}", ResourceManagerHelper.Instance.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao reverter efeitos visuais: {ex.Message}", ResourceManagerHelper.Instance.ErrorTitle,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _confirmationWindow.Close();
            }
        }

        private static void SetRegistryValue(string keyPath, string name, object value, RegistryValueKind kind)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true) ?? Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    if (key != null)
                    {
                        key.SetValue(name, value, kind);
                    }
                    else
                    {
                        throw new Exception($"Não foi possível abrir ou criar a chave {keyPath}");
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, IntPtr pvParam, uint fWinIni);
    }
}