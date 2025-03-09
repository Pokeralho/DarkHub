using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ICSharpCode.AvalonEdit;
using System.Windows.Media;

namespace DarkHub
{
    public partial class TextEditor : Page
    {
        private readonly string docsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DarkHubDocs");
        private bool isLoaded = false;

        public TextEditor()
        {
            try
            {
                InitializeComponent();
                Debug.WriteLine("InitializeComponent concluído.");
                Loaded += TextEditor_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorBuildingPage, ex.Message),
                                ResourceManagerHelper.Instance.CriticalErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao inicializar TextEditor: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void TextEditor_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Debug.WriteLine("TextEditor_Loaded iniciado.");
                EnsureDocsFolderExists();
                await LoadSavedFilesAsync();
                await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.PageLoadedSuccess);
                isLoaded = true;
                SyncComboBoxWithSelectedTab();
                Debug.WriteLine("TextEditor inicializado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.ErrorLoadingTextEditor, ex.Message),
                                ResourceManagerHelper.Instance.CriticalErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine($"Erro em TextEditor_Loaded: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void EnsureDocsFolderExists()
        {
            try
            {
                if (!Directory.Exists(docsFolder))
                {
                    Directory.CreateDirectory(docsFolder);
                    Debug.WriteLine($"Pasta {docsFolder} criada com sucesso.");
                }
                else
                {
                    Debug.WriteLine($"Pasta {docsFolder} já existe.");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorCreatingDocsFolder, ex.Message));
                Debug.WriteLine($"Erro em EnsureDocsFolderExists: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async Task LoadSavedFilesAsync()
        {
            try
            {
                Debug.WriteLine("LoadSavedFilesAsync iniciado.");
                string[] files = await Task.Run(() =>
                {
                    Debug.WriteLine($"Buscando arquivos em {docsFolder}");
                    return Directory.GetFiles(docsFolder, "*.*");
                });
                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string content = await File.ReadAllTextAsync(file);
                    string extension = Path.GetExtension(file).ToLower();
                    await Dispatcher.InvokeAsync(() =>
                    {
                        AddTab(fileName, content, extension == ".py" ? ResourceManagerHelper.Instance.PythonOption : ResourceManagerHelper.Instance.PlainTextOption);
                    });
                }

                if (tabControl.Items.Count == 0)
                {
                    await Dispatcher.InvokeAsync(() => AddTab(ResourceManagerHelper.Instance.NewDocumentHeader, "", ResourceManagerHelper.Instance.PlainTextOption));
                }
                Debug.WriteLine($"Arquivos carregados: {files.Length}");
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorLoadingFiles, ex.Message));
                Debug.WriteLine($"Erro em LoadSavedFilesAsync: {ex.Message}\nStackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private void AddTab(string header, string content, string docType)
        {
            try
            {
                Debug.WriteLine($"Adicionando aba: {header} ({docType})");
                if (tabControl == null)
                {
                    Debug.WriteLine("tabControl é nulo em AddTab!");
                    throw new InvalidOperationException("TabControl não foi inicializado.");
                }

                TabItem newTab = new() { Header = header };
                object? editorContent = null;

                if (docType == ResourceManagerHelper.Instance.PythonOption)
                {
                    editorContent = new ICSharpCode.AvalonEdit.TextEditor
                    {
                        ShowLineNumbers = true,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 14,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                        Text = content ?? "",
                        SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("Python")
                    };
                }
                else
                {
                    editorContent = new RichTextBox
                    {
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 14,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                    };
                    ((RichTextBox)editorContent).Document.Blocks.Clear();
                    ((RichTextBox)editorContent).Document.Blocks.Add(new Paragraph(new Run(content ?? "")));
                }

                newTab.Tag = docType;
                newTab.Content = editorContent ?? throw new InvalidOperationException("Editor content não foi inicializado.");
                tabControl.Items.Add(newTab);

                if (editorContent is ICSharpCode.AvalonEdit.TextEditor codeEditor)
                {
                    SyncFontSizeComboBox(codeEditor);
                }
                else if (editorContent is RichTextBox richEditor)
                {
                    SyncFontSizeComboBox(richEditor);
                }
                Debug.WriteLine($"Aba adicionada com sucesso: {header} ({docType})");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorAddingTab, ex.Message));
                Debug.WriteLine($"Erro em AddTab: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void NewTab_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("NewTab_Click iniciado.");
                if (docTypeComboBox == null)
                {
                    Debug.WriteLine("docTypeComboBox é nulo em NewTab_Click!");
                    throw new InvalidOperationException("docTypeComboBox não foi inicializado.");
                }

                string docType = (docTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? ResourceManagerHelper.Instance.PlainTextOption;
                await Dispatcher.InvokeAsync(() =>
                {
                    AddTab($"{ResourceManagerHelper.Instance.NewDocumentHeader} {tabControl.Items.Count + 1}", "", docType);
                    tabControl.SelectedIndex = tabControl.Items.Count - 1;
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorCreatingNewTab, ex.Message));
                Debug.WriteLine($"Erro em NewTab_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void Save_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("Save_Click iniciado.");
                if (tabControl == null)
                {
                    Debug.WriteLine("tabControl é nulo em Save_Click!");
                    throw new InvalidOperationException("TabControl não foi inicializado.");
                }

                if (tabControl.SelectedItem is not TabItem selectedTab)
                {
                    await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.NoEditorSelectedToSave);
                    Debug.WriteLine("Nenhuma aba selecionada em Save_Click.");
                    return;
                }

                if (selectedTab.Tag == null)
                {
                    selectedTab.Tag = ResourceManagerHelper.Instance.PlainTextOption;
                    Debug.WriteLine("selectedTab.Tag era nulo e foi definido como 'Texto Simples' em Save_Click.");
                }

                string fileName = selectedTab.Header?.ToString() ?? ResourceManagerHelper.Instance.NewDocumentHeader;
                string extension = selectedTab.Tag.ToString() == ResourceManagerHelper.Instance.PythonOption ? ".py" : ".txt";
                if (!fileName.EndsWith(extension)) fileName += extension;
                string filePath = Path.Combine(docsFolder, fileName);

                await Task.Run(async () =>
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        if (selectedTab.Content is ICSharpCode.AvalonEdit.TextEditor codeEditor)
                        {
                            Debug.WriteLine($"Salvando código Python em: {filePath}");
                            File.WriteAllText(filePath, codeEditor.Text);
                        }
                        else if (selectedTab.Content is RichTextBox richEditor)
                        {
                            Debug.WriteLine($"Salvando texto simples em: {filePath}");
                            TextRange textRange = new(richEditor.Document.ContentStart, richEditor.Document.ContentEnd);
                            File.WriteAllText(filePath, textRange.Text.TrimEnd());
                        }
                        else
                        {
                            Debug.WriteLine("selectedTab.Content é nulo ou de tipo inesperado em Save_Click!");
                            throw new InvalidOperationException("Conteúdo da aba não é um editor válido.");
                        }
                        outputBox.Text = string.Format(ResourceManagerHelper.Instance.FileSavedAt, filePath);
                    });
                    Debug.WriteLine($"Arquivo salvo: {filePath}");
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorSavingFile, ex.Message));
                Debug.WriteLine($"Erro em Save_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void Rename_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("Rename_Click iniciado.");
                if (tabControl == null)
                {
                    Debug.WriteLine("tabControl é nulo em Rename_Click!");
                    throw new InvalidOperationException("TabControl não foi inicializado.");
                }

                if (tabControl.SelectedItem is not TabItem selectedTab)
                {
                    await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.NoEditorSelectedToRename);
                    Debug.WriteLine("Nenhuma aba selecionada em Rename_Click.");
                    return;
                }

                if (selectedTab.Tag == null)
                {
                    selectedTab.Tag = ResourceManagerHelper.Instance.PlainTextOption;
                    Debug.WriteLine("selectedTab.Tag era nulo e foi definido como 'Texto Simples' em Rename_Click.");
                }

                string oldFileName = selectedTab.Header?.ToString() ?? ResourceManagerHelper.Instance.NewDocumentHeader;
                string extension = selectedTab.Tag.ToString() == ResourceManagerHelper.Instance.PythonOption ? ".py" : ".txt";
                string oldFilePath = Path.Combine(docsFolder, oldFileName + extension);

                string? newName = await Dispatcher.InvokeAsync(() => Microsoft.VisualBasic.Interaction.InputBox(
                    ResourceManagerHelper.Instance.EnterNewDocumentNamePrompt,
                    ResourceManagerHelper.Instance.RenameDocumentTitle,
                    oldFileName));
                if (string.IsNullOrWhiteSpace(newName))
                {
                    Debug.WriteLine("Novo nome não fornecido em Rename_Click.");
                    return;
                }

                string newFileName = newName + extension;
                string newFilePath = Path.Combine(docsFolder, newFileName);

                await Task.Run(async () =>
                {
                    if (File.Exists(oldFilePath))
                    {
                        File.Move(oldFilePath, newFilePath);
                        Debug.WriteLine($"Arquivo movido: {oldFilePath} -> {newFilePath}");
                    }
                    else
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            if (selectedTab.Content is ICSharpCode.AvalonEdit.TextEditor codeEditor)
                            {
                                File.WriteAllText(newFilePath, codeEditor.Text);
                                Debug.WriteLine($"Novo arquivo Python criado: {newFilePath}");
                            }
                            else if (selectedTab.Content is RichTextBox richEditor)
                            {
                                TextRange textRange = new(richEditor.Document.ContentStart, richEditor.Document.ContentEnd);
                                File.WriteAllText(newFilePath, textRange.Text.TrimEnd());
                                Debug.WriteLine($"Novo arquivo de texto criado: {newFilePath}");
                            }
                            else
                            {
                                Debug.WriteLine("selectedTab.Content é nulo ou de tipo inesperado em Rename_Click!");
                                throw new InvalidOperationException("Conteúdo da aba não é um editor válido.");
                            }
                        });
                    }
                    await Dispatcher.InvokeAsync(() =>
                    {
                        selectedTab.Header = newName;
                        outputBox.Text = string.Format(ResourceManagerHelper.Instance.FileRenamedTo, newFilePath);
                    });
                    Debug.WriteLine($"Arquivo renomeado: {oldFilePath} -> {newFilePath}");
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorRenamingFile, ex.Message));
                Debug.WriteLine($"Erro em Rename_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void Delete_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("Delete_Click iniciado.");
                if (tabControl == null)
                {
                    Debug.WriteLine("tabControl é nulo em Delete_Click!");
                    throw new InvalidOperationException("TabControl não foi inicializado.");
                }

                if (tabControl.SelectedItem is not TabItem selectedTab)
                {
                    await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.NoTabSelectedToDelete);
                    Debug.WriteLine("Nenhuma aba selecionada em Delete_Click.");
                    return;
                }

                string fileName = selectedTab.Header?.ToString() ?? ResourceManagerHelper.Instance.NewDocumentHeader;
                string extension = selectedTab.Tag?.ToString() == ResourceManagerHelper.Instance.PythonOption ? ".py" : ".txt";
                if (!fileName.EndsWith(extension)) fileName += extension;
                string filePath = Path.Combine(docsFolder, fileName);

                bool confirm = await Dispatcher.InvokeAsync(() => MessageBox.Show(
                    string.Format(ResourceManagerHelper.Instance.ConfirmDeleteMessage, fileName),
                    ResourceManagerHelper.Instance.ConfirmDeleteTitle,
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes);
                if (!confirm)
                {
                    await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.DeleteCancelled);
                    Debug.WriteLine("Exclusão cancelada pelo usuário em Delete_Click.");
                    return;
                }

                await Task.Run(async () =>
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        await Dispatcher.InvokeAsync(() =>
                        {
                            tabControl.Items.Remove(selectedTab);
                            outputBox.Text = string.Format(ResourceManagerHelper.Instance.FileDeletedSuccess, fileName);
                        });
                        Debug.WriteLine($"Arquivo excluído: {filePath}");
                    }
                    else
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            tabControl.Items.Remove(selectedTab);
                            outputBox.Text = string.Format(ResourceManagerHelper.Instance.TabClosedNoFile, fileName);
                        });
                        Debug.WriteLine($"Aba fechada (arquivo não existia): {fileName}");
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorDeletingFile, ex.Message));
                Debug.WriteLine($"Erro em Delete_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private async void RunPython_Click(object? sender, RoutedEventArgs? e)
        {
            try
            {
                Debug.WriteLine("RunPython_Click iniciado.");
                if (tabControl == null)
                {
                    Debug.WriteLine("tabControl é nulo em RunPython_Click!");
                    throw new InvalidOperationException("TabControl não foi inicializado.");
                }

                if (tabControl.SelectedItem is not TabItem selectedTab || selectedTab.Content is not ICSharpCode.AvalonEdit.TextEditor editor)
                {
                    await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.NoEditorSelectedToRun);
                    Debug.WriteLine("Nenhuma aba válida selecionada em RunPython_Click.");
                    return;
                }

                if (selectedTab.Tag?.ToString() != ResourceManagerHelper.Instance.PythonOption)
                {
                    await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.NotPythonDocument);
                    Debug.WriteLine("Documento não é Python em RunPython_Click.");
                    return;
                }

                string pythonCode = editor.Text;
                await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.RunningPythonCode + Environment.NewLine + Environment.NewLine);

                string tempFilePath = Path.Combine(Path.GetTempPath(), "temp_script.py");
                await File.WriteAllTextAsync(tempFilePath, pythonCode);

                ProcessStartInfo psi = new()
                {
                    FileName = "python",
                    Arguments = $"\"{tempFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = psi };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(error))
                {
                    await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.RunningPythonCode + Environment.NewLine + Environment.NewLine + error);
                }
                else
                {
                    await Dispatcher.InvokeAsync(() => outputBox.Text = ResourceManagerHelper.Instance.RunningPythonCode + Environment.NewLine + Environment.NewLine + output);
                }

                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro ao deletar arquivo temporário: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    outputBox.Text += string.Format(ResourceManagerHelper.Instance.ErrorRunningPython, ex.Message);
                    outputBox.Text += ResourceManagerHelper.Instance.PythonInstallInstructions;
                });
                Debug.WriteLine($"Erro em RunPython_Click: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void DocTypeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
        {
            if (!isLoaded) return;

            try
            {
                Debug.WriteLine("DocTypeComboBox_SelectionChanged iniciado.");
                if (tabControl == null || docTypeComboBox == null)
                {
                    Debug.WriteLine("tabControl ou docTypeComboBox é nulo em DocTypeComboBox_SelectionChanged!");
                    throw new InvalidOperationException("Controles essenciais não foram inicializados.");
                }

                if (tabControl.SelectedItem is not TabItem selectedTab)
                    return;

                string? selectedType = (docTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (string.IsNullOrEmpty(selectedType))
                    return;

                if (selectedType == ResourceManagerHelper.Instance.PythonOption)
                {
                    if (selectedTab.Content is RichTextBox currentRichEditor)
                    {
                        TextRange textRange = new(currentRichEditor.Document.ContentStart, currentRichEditor.Document.ContentEnd);
                        string content = textRange.Text.TrimEnd();
                        var codeEditor = new ICSharpCode.AvalonEdit.TextEditor
                        {
                            ShowLineNumbers = true,
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 14,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                            Text = content,
                            SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("Python")
                        };
                        selectedTab.Content = codeEditor;
                        SyncFontSizeComboBox(codeEditor);
                    }
                    selectedTab.Tag = ResourceManagerHelper.Instance.PythonOption;
                }
                else
                {
                    if (selectedTab.Content is ICSharpCode.AvalonEdit.TextEditor currentCodeEditor)
                    {
                        string content = currentCodeEditor.Text;
                        var richEditor = new RichTextBox
                        {
                            FontFamily = new FontFamily("Consolas"),
                            FontSize = 14,
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                        };
                        richEditor.Document.Blocks.Clear();
                        richEditor.Document.Blocks.Add(new Paragraph(new Run(content)));
                        selectedTab.Content = richEditor;
                        SyncFontSizeComboBox(richEditor);
                    }
                    selectedTab.Tag = ResourceManagerHelper.Instance.PlainTextOption;
                }
                fontSizeComboBox.Visibility = Visibility.Visible;
                Debug.WriteLine($"Tipo de documento alterado para: {selectedType}");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorChangingDocType, ex.Message));
                Debug.WriteLine($"Erro em DocTypeComboBox_SelectionChanged: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void TabControl_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
        {
            if (!isLoaded) return;

            try
            {
                Debug.WriteLine("TabControl_SelectionChanged iniciado.");
                if (tabControl == null || docTypeComboBox == null || fontSizeComboBox == null)
                {
                    Debug.WriteLine("Controles essenciais são nulos em TabControl_SelectionChanged!");
                    throw new InvalidOperationException("Controles essenciais não foram inicializados.");
                }

                if (tabControl.SelectedItem is TabItem selectedTab)
                {
                    string docType = selectedTab.Tag?.ToString() ?? ResourceManagerHelper.Instance.PlainTextOption;
                    docTypeComboBox.SelectedItem = docTypeComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == docType);

                    fontSizeComboBox.Visibility = Visibility.Visible;
                    if (selectedTab.Content is ICSharpCode.AvalonEdit.TextEditor codeEditor)
                    {
                        SyncFontSizeComboBox(codeEditor);
                    }
                    else if (selectedTab.Content is RichTextBox richEditor)
                    {
                        SyncFontSizeComboBox(richEditor);
                    }
                    Debug.WriteLine($"Aba selecionada: {selectedTab.Header}");
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorSwitchingTab, ex.Message));
                Debug.WriteLine($"Erro em TabControl_SelectionChanged: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SyncComboBoxWithSelectedTab()
        {
            try
            {
                Debug.WriteLine("SyncComboBoxWithSelectedTab iniciado.");
                if (tabControl == null || docTypeComboBox == null || fontSizeComboBox == null)
                {
                    Debug.WriteLine("Controles essenciais são nulos em SyncComboBoxWithSelectedTab!");
                    throw new InvalidOperationException("Controles essenciais não foram inicializados.");
                }

                if (tabControl.SelectedItem is TabItem selectedTab)
                {
                    ICSharpCode.AvalonEdit.TextEditor? codeEditor = selectedTab.Content as ICSharpCode.AvalonEdit.TextEditor;
                    RichTextBox? richEditor = selectedTab.Content as RichTextBox;

                    string docType = selectedTab.Tag?.ToString() ?? ResourceManagerHelper.Instance.PlainTextOption;
                    docTypeComboBox.SelectedItem = docTypeComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == docType);

                    fontSizeComboBox.Visibility = Visibility.Visible;
                    if (codeEditor != null)
                    {
                        SyncFontSizeComboBox(codeEditor);
                    }
                    else if (richEditor != null)
                    {
                        SyncFontSizeComboBox(richEditor);
                    }
                    else
                    {
                        Debug.WriteLine("selectedTab.Content é nulo ou de tipo inesperado em SyncComboBoxWithSelectedTab!");
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorSyncingComboBox, ex.Message));
                Debug.WriteLine($"Erro em SyncComboBoxWithSelectedTab: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void FontSizeComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
        {
            if (!isLoaded) return;

            try
            {
                Debug.WriteLine("FontSizeComboBox_SelectionChanged iniciado.");
                if (tabControl == null || fontSizeComboBox == null)
                {
                    Debug.WriteLine("Controles essenciais são nulos em FontSizeComboBox_SelectionChanged!");
                    throw new InvalidOperationException("Controles essenciais não foram inicializados.");
                }

                if (tabControl.SelectedItem is TabItem selectedTab)
                {
                    string? selectedSize = (fontSizeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                    if (double.TryParse(selectedSize, out double size))
                    {
                        if (selectedTab.Content is ICSharpCode.AvalonEdit.TextEditor codeEditor)
                        {
                            codeEditor.FontSize = size;
                        }
                        else if (selectedTab.Content is RichTextBox richEditor)
                        {
                            richEditor.FontSize = size;
                        }
                        else
                        {
                            Debug.WriteLine("selectedTab.Content é nulo ou de tipo inesperado em FontSizeComboBox_SelectionChanged!");
                        }
                        Debug.WriteLine($"Tamanho da fonte alterado para: {size}");
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => outputBox.Text = string.Format(ResourceManagerHelper.Instance.ErrorChangingFontSize, ex.Message));
                Debug.WriteLine($"Erro em FontSizeComboBox_SelectionChanged: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SyncFontSizeComboBox(ICSharpCode.AvalonEdit.TextEditor editor)
        {
            try
            {
                Debug.WriteLine("SyncFontSizeComboBox (TextEditor) iniciado.");
                if (fontSizeComboBox == null)
                {
                    Debug.WriteLine("fontSizeComboBox é nulo em SyncFontSizeComboBox (TextEditor)!");
                    throw new InvalidOperationException("fontSizeComboBox não foi inicializado.");
                }

                string currentSize = editor.FontSize.ToString();
                fontSizeComboBox.SelectedItem = fontSizeComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == currentSize) ?? fontSizeComboBox.Items[2];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em SyncFontSizeComboBox (TextEditor): {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void SyncFontSizeComboBox(RichTextBox richEditor)
        {
            try
            {
                Debug.WriteLine("SyncFontSizeComboBox (RichTextBox) iniciado.");
                if (fontSizeComboBox == null)
                {
                    Debug.WriteLine("fontSizeComboBox é nulo em SyncFontSizeComboBox (RichTextBox)!");
                    throw new InvalidOperationException("fontSizeComboBox não foi inicializado.");
                }

                string currentSize = richEditor.FontSize.ToString();
                fontSizeComboBox.SelectedItem = fontSizeComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == currentSize) ?? fontSizeComboBox.Items[2];
                Debug.WriteLine($"FontSizeComboBox sincronizado para RichTextBox: {currentSize}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em SyncFontSizeComboBox (RichTextBox): {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        }
    }
}