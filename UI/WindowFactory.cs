using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace DarkHub.UI
{
    public class WindowFactory
    {
        public static readonly SolidColorBrush PrimaryBackground = new SolidColorBrush(Color.FromRgb(30, 30, 30));
        public static readonly SolidColorBrush SecondaryBackground = new SolidColorBrush(Color.FromRgb(45, 45, 45));
        public static readonly SolidColorBrush AccentColor = new SolidColorBrush(Color.FromRgb(0, 196, 180));
        public static readonly SolidColorBrush BorderColor = new SolidColorBrush(Color.FromRgb(64, 64, 64));
        public static readonly SolidColorBrush TextColor = new SolidColorBrush(Color.FromRgb(224, 224, 224));
        public static readonly SolidColorBrush PlaceholderColor = new SolidColorBrush(Color.FromRgb(160, 160, 160));

        public static SolidColorBrush DefaultBackground = PrimaryBackground;
        public static SolidColorBrush DefaultBorderBrush = BorderColor;
        public static SolidColorBrush DefaultTextForeground = TextColor;
        public static SolidColorBrush DefaultControlBackground = SecondaryBackground;

        public static readonly Thickness DefaultBorderThickness = new Thickness(1);
        public static readonly FontFamily DefaultFontFamily = new FontFamily("JetBrains Mono");
        public static readonly double DefaultFontSize = 14;
        public static readonly CornerRadius DefaultCornerRadius = new CornerRadius(8);

        public static (Window Window, TextBox TextBox) CreateProgressWindow(string title, double width = 500, double height = 400)
        {
            var window = CreateWindow(title, width, height);

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = TextColor,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };
            Grid.SetRow(titleText, 0);
            grid.Children.Add(titleText);

            var textBox = new TextBox
            {
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = SecondaryBackground,
                Foreground = TextColor,
                Margin = new Thickness(15),
                Padding = new Thickness(10),
                BorderBrush = BorderColor,
                BorderThickness = DefaultBorderThickness,
                FontFamily = DefaultFontFamily,
                FontSize = DefaultFontSize
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            var border = new Border
            {
                CornerRadius = DefaultCornerRadius,
                Background = PrimaryBackground,
                BorderBrush = BorderColor,
                BorderThickness = DefaultBorderThickness,
                Child = grid,
                Margin = new Thickness(2),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 315,
                    ShadowDepth = 3,
                    Opacity = 0.2,
                    BlurRadius = 5
                }
            };

            window.Content = border;
            return (window, textBox);
        }

        public static TextBlock CreateTitleText(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = TextColor,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap
            };
        }

        public static ListBox CreateStyledListBox(double height = 150)
        {
            var listBox = new ListBox
            {
                Height = height,
                Background = SecondaryBackground,
                Foreground = TextColor,
                BorderBrush = BorderColor,
                BorderThickness = DefaultBorderThickness,
                FontFamily = DefaultFontFamily,
                FontSize = DefaultFontSize
            };

            var style = new Style(typeof(ListBoxItem));
            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, AccentColor));
            style.Triggers.Add(trigger);

            listBox.ItemContainerStyle = style;

            return listBox;
        }

        public static CheckBox CreateStyledCheckBox(string content, object tag = null, bool isChecked = false)
        {
            var checkBox = new CheckBox
            {
                Content = content,
                Tag = tag,
                IsChecked = isChecked,
                Foreground = TextColor,
                Margin = new Thickness(5, 2, 0, 2),
                FontFamily = DefaultFontFamily,
                FontSize = DefaultFontSize
            };

            string templateXaml = @"
        <ControlTemplate TargetType=""CheckBox""
                         xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
                         xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml"">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width=""Auto"" />
                    <ColumnDefinition Width=""*"" />
                </Grid.ColumnDefinitions>
                <Border x:Name=""checkboxBorder"" Width=""16"" Height=""16"" Background=""{DynamicResource SecondaryBackground}""
                        BorderBrush=""{DynamicResource BorderColor}"" BorderThickness=""1"" CornerRadius=""3"" Margin=""0,0,8,0"">
                    <Path x:Name=""checkMarkPath"" Data=""M 2 8 L 6 12 L 14 4"" Stroke=""{DynamicResource AccentColor}""
                          StrokeThickness=""2"" VerticalAlignment=""Center"" HorizontalAlignment=""Center"" Opacity=""0"" />
                </Border>
                <ContentPresenter Grid.Column=""1"" VerticalAlignment=""Center"" />
            </Grid>
            <ControlTemplate.Triggers>
                <Trigger Property=""IsChecked"" Value=""True"">
                    <Setter TargetName=""checkMarkPath"" Property=""Opacity"" Value=""1"" />
                    <Setter TargetName=""checkboxBorder"" Property=""BorderBrush"" Value=""{DynamicResource AccentColor}"" />
                </Trigger>
                <Trigger Property=""IsMouseOver"" Value=""True"">
                    <Setter TargetName=""checkboxBorder"" Property=""BorderBrush"" Value=""{DynamicResource AccentColor}"" />
                </Trigger>
            </ControlTemplate.Triggers>
        </ControlTemplate>";

            using (var stringReader = new System.IO.StringReader(templateXaml))
            {
                using (var xmlReader = System.Xml.XmlReader.Create(stringReader))
                {
                    var template = (ControlTemplate)System.Windows.Markup.XamlReader.Load(xmlReader);
                    checkBox.Template = template;
                }
            }

            checkBox.Resources["SecondaryBackground"] = SecondaryBackground;
            checkBox.Resources["BorderColor"] = BorderColor;
            checkBox.Resources["AccentColor"] = AccentColor;

            return checkBox;
        }

        public static string ExecuteCommandWithOutput(string command, TextBox progressTextBox)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.GetEncoding(850),
                    StandardErrorEncoding = Encoding.GetEncoding(850)
                };

                using (Process process = new Process { StartInfo = processInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    string result = output;
                    if (!string.IsNullOrEmpty(error))
                    {
                        result += $"\nErro: {error}";
                        WindowFactory.AppendProgress(progressTextBox, $"Erro ao executar '{command}': {error}");
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                WindowFactory.AppendProgress(progressTextBox, $"Exceção ao executar comando: {ex.Message}");
                return $"Erro: {ex.Message}";
            }
        }

        public static async Task<string> ExecuteCommandWithOutputAsync(string command, TextBox progressTextBox, bool requiresAdmin = false)
        {
            try
            {
                if (requiresAdmin)
                {
                    string escapedCommand = command.Replace("\"", "\\\"");

                    string tempDir = System.IO.Path.GetTempPath();
                    string scriptPath = System.IO.Path.Combine(tempDir, $"DarkHub_ElevatedCmd_{Guid.NewGuid()}.ps1");

                    string scriptContent = @"
                    # Script para executar comandos elevados sem mostrar janelas
                    $command = """ + escapedCommand + @"""

                    # Criar um objeto StartInfo para iniciar o processo
                    $psi = New-Object System.Diagnostics.ProcessStartInfo
                    $psi.FileName = 'cmd.exe'
                    $psi.Arguments = '/c ' + $command
                    $psi.RedirectStandardOutput = $true
                    $psi.RedirectStandardError = $true
                    $psi.UseShellExecute = $false
                    $psi.CreateNoWindow = $true

                    # Iniciar o processo
                    $process = New-Object System.Diagnostics.Process
                    $process.StartInfo = $psi
                    $process.Start() | Out-Null

                    # Capturar saída
                    $output = $process.StandardOutput.ReadToEnd()
                    $error = $process.StandardError.ReadToEnd()
                    $process.WaitForExit()

                    # Retornar resultado
                    if ($error) {
                        Write-Output ""Erro: $error""
                    }
                    Write-Output $output
                    ";

                    File.WriteAllText(scriptPath, scriptContent);

                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "powershell.exe",
                            Arguments = $"-ExecutionPolicy Bypass -Command \"Start-Process powershell -Verb RunAs -ArgumentList '-ExecutionPolicy Bypass -File \"\"{scriptPath}\"\"' -WindowStyle Hidden -Wait\"",
                            UseShellExecute = true,
                            CreateNoWindow = false,
                            WindowStyle = ProcessWindowStyle.Minimized
                        };

                        AppendProgress(progressTextBox, $"Executando comando com privilégios de administrador: {command}");

                        var process = Process.Start(psi);
                        await Task.Run(() =>
                        {
                            process.WaitForExit();
                            System.Threading.Thread.Sleep(1000);
                        });

                        string outputPath = scriptPath + ".log";
                        string result = "Comando executado com privilégios de administrador";

                        if (File.Exists(outputPath))
                        {
                            result = File.ReadAllText(outputPath);
                            File.Delete(outputPath);
                        }

                        try { File.Delete(scriptPath); } catch { }

                        AppendProgress(progressTextBox, "Comando administrativo concluído.");
                        return result;
                    }
                    catch (Exception ex)
                    {
                        AppendProgress(progressTextBox, $"Erro ao executar comando administrativo: {ex.Message}");
                        return $"Erro: {ex.Message}";
                    }
                    finally
                    {
                        try { if (File.Exists(scriptPath)) File.Delete(scriptPath); } catch { }
                    }
                }
                else
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {command}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.GetEncoding(850),
                        StandardErrorEncoding = Encoding.GetEncoding(850)
                    };

                    AppendProgress(progressTextBox, $"Executando: {command}");

                    var process = new Process { StartInfo = processStartInfo };
                    process.Start();

                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (!string.IsNullOrEmpty(output))
                        AppendProgress(progressTextBox, output);
                    if (!string.IsNullOrEmpty(error))
                        AppendProgress(progressTextBox, $"Erro: {error}");

                    return output + error;
                }
            }
            catch (Exception ex)
            {
                AppendProgress(progressTextBox, $"Erro ao executar comando: {ex.Message}");
                return string.Empty;
            }
        }

        public static Style CreateDataGridHeaderStyle()
        {
            var style = new Style(typeof(DataGridColumnHeader));
            style.Setters.Add(new Setter(Control.BackgroundProperty, SecondaryBackground));
            style.Setters.Add(new Setter(Control.ForegroundProperty, TextColor));
            style.Setters.Add(new Setter(TextElement.FontWeightProperty, FontWeights.Bold));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(60, 60, 60))));
            style.Triggers.Add(trigger);

            return style;
        }

        public static Style CreateDataGridCellStyle()
        {
            var style = new Style(typeof(DataGridCell));
            style.Setters.Add(new Setter(Control.BackgroundProperty, SecondaryBackground));
            style.Setters.Add(new Setter(Control.ForegroundProperty, TextColor));
            style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 4, 8, 4)));
            style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(55, 55, 55))));
            style.Triggers.Add(trigger);

            return style;
        }

        public static Style CreateDataGridRowStyle()
        {
            var style = new Style(typeof(DataGridRow));
            style.Setters.Add(new Setter(Control.BackgroundProperty, SecondaryBackground));
            style.Setters.Add(new Setter(Control.ForegroundProperty, TextColor));

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(60, 60, 60))));
            style.Triggers.Add(trigger);

            var selectedTrigger = new Trigger { Property = Selector.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty, AccentColor));
            selectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            style.Triggers.Add(selectedTrigger);

            return style;
        }

        public static void AppendProgress(TextBox textBox, string message)
        {
            if (textBox != null)
            {
                textBox.Dispatcher.Invoke(() =>
                {
                    textBox.AppendText(message + Environment.NewLine);
                    textBox.ScrollToEnd();
                });
            }
        }

        public static Window CreateWindow(string title, double width, double height, Window owner = null, bool isModal = true, bool resizable = false)
        {
            var window = new Window
            {
                Title = title,
                Width = width,
                Height = height,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                Background = PrimaryBackground,
                BorderBrush = BorderColor,
                BorderThickness = DefaultBorderThickness,
                FontFamily = DefaultFontFamily,
                FontSize = DefaultFontSize,
                ResizeMode = resizable ? ResizeMode.CanResize : ResizeMode.NoResize,
                Owner = owner
            };

            if (isModal)
            {
                window.ShowInTaskbar = false;
                window.WindowStyle = WindowStyle.ToolWindow;
            }

            return window;
        }

        public static (Window, TextBox) CreateProgressWindow(string title)
        {
            var window = CreateWindow(title, 500, 300, null, false, false);
            var textBox = new TextBox
            {
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(15),
                Padding = new Thickness(10),
                Background = SecondaryBackground,
                Foreground = TextColor,
                BorderBrush = BorderColor,
                BorderThickness = DefaultBorderThickness
            };

            var border = new Border
            {
                CornerRadius = DefaultCornerRadius,
                Child = textBox,
                Margin = new Thickness(5)
            };

            window.Content = border;
            return (window, textBox);
        }

        public static Button CreateStyledButton(string content, double width, RoutedEventHandler clickHandler)
        {
            var button = new Button
            {
                Content = content,
                Width = width,
                Height = 40,
                Margin = new Thickness(5),
                Background = AccentColor,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                FontSize = 14
            };

            var style = CreateButtonStyle();
            button.Style = style;

            button.Click += clickHandler;
            return button;
        }

        public static Style CreateButtonStyle()
        {
            var style = new Style(typeof(Button));

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.CornerRadiusProperty, DefaultCornerRadius);

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentPresenterFactory);

            template.VisualTree = borderFactory;

            var trigger = new Trigger
            {
                Property = Button.IsMouseOverProperty,
                Value = true
            };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0, 168, 154))));

            template.Triggers.Add(trigger);

            style.Setters.Add(new Setter(Button.TemplateProperty, template));

            return style;
        }

        public static void CaptureAndRestoreOwnerState(Window owner, Action action)
        {
            WindowState ownerState = WindowState.Normal;
            bool isOwnerVisible = false;

            if (owner != null && owner.IsLoaded)
            {
                ownerState = owner.WindowState;
                isOwnerVisible = owner.IsVisible;
            }

            action?.Invoke();

            if (owner != null && owner.IsLoaded && isOwnerVisible)
            {
                owner.WindowState = ownerState;
                owner.Activate();
                owner.Focus();
            }
        }

        public static void ShowMessage(Window owner, string message, string title, MessageBoxImage icon = MessageBoxImage.Information)
        {
            if (owner != null)
            {
                owner.Dispatcher.Invoke(() =>
                {
                    CaptureAndRestoreOwnerState(owner, () =>
                    {
                        MessageBox.Show(message, title, MessageBoxButton.OK, icon);
                    });
                });
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, icon);
            }
        }
    }
}