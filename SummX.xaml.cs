using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Diagnostics;
using static System.Windows.Clipboard;

namespace DarkHub
{
    public partial class SummX : Page
    {
        private SummaX summarizer;
        private bool isProcessing = false;

        public SummX()
        {
            InitializeComponent();
            summarizer = new SummaX();
        }

        private void Page_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                Window.GetWindow(this)?.DragMove();
            }
        }

        private async void SummarizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessing) return;
            isProcessing = true;

            try
            {
                string inputText = InputTextBox.Text;
                if (string.IsNullOrEmpty(inputText))
                {
                    MessageBox.Show(ResourceManagerHelper.Instance.EmptyTextError,
                                  ResourceManagerHelper.Instance.ErrorTitle,
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    return;
                }

                SummarizeButton.IsEnabled = false;
                SummarizePdfButton.IsEnabled = false;
                SummarizeButton.Content = ResourceManagerHelper.Instance.Processing;

                ConfigureSummarizer();
                string summary = await Task.Run(() => summarizer.Summarize(inputText));
                OutputTextBox.Text = summary;
                ShowKeywordFeedback(summary);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.SummarizationError, ex.Message),
                              ResourceManagerHelper.Instance.ErrorTitle,
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao resumir texto: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                SummarizeButton.IsEnabled = true;
                SummarizePdfButton.IsEnabled = true;
                SummarizeButton.Content = ResourceManagerHelper.Instance.SummarizeText;
                isProcessing = false;
            }
        }

        private async void SummarizePdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (isProcessing) return;
            isProcessing = true;

            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    Title = ResourceManagerHelper.Instance.SelectPDFFile
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    SummarizeButton.IsEnabled = false;
                    SummarizePdfButton.IsEnabled = false;
                    SummarizePdfButton.Content = ResourceManagerHelper.Instance.Processing;

                    ConfigureSummarizer();
                    string summary = await Task.Run(() => summarizer.SummarizeFromPdf(openFileDialog.FileName));
                    OutputTextBox.Text = summary;
                    ShowKeywordFeedback(summary);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ResourceManagerHelper.Instance.PDFSummarizationError, ex.Message),
                              ResourceManagerHelper.Instance.ErrorTitle,
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
                Debug.WriteLine($"Erro ao resumir PDF: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                SummarizeButton.IsEnabled = true;
                SummarizePdfButton.IsEnabled = true;
                SummarizePdfButton.Content = ResourceManagerHelper.Instance.SummarizePDF;
                isProcessing = false;
            }
        }

        private void ConfigureSummarizer()
        {
            List<string> keywords = string.IsNullOrWhiteSpace(KeywordsTextBox.Text)
                ? new List<string>()
                : KeywordsTextBox.Text.Split(',')
                                     .Select(k => k.Trim())
                                     .Where(k => !string.IsNullOrEmpty(k))
                                     .ToList();

            string language = (LanguageComboBox.SelectedItem as ComboBoxItem)?.Content.ToString().ToLower() ?? "pt";
            if (language == "português") language = "pt";
            else if (language == "english") language = "en";
            else if (language == "español") language = "es";

            int maxLength = int.TryParse(MaxLengthTextBox.Text, out int ml) ? ml : 150;
            double keywordWeight = double.TryParse(KeywordWeightTextBox.Text, out double kw) ? kw : 1.5;
            double tfidfWeight = double.TryParse(TfidfWeightTextBox.Text, out double tw) ? tw : 1.0;
            double textRankWeight = double.TryParse(TextRankWeightTextBox.Text, out double trw) ? trw : 1.0;

            summarizer = new SummaX(maxLength, language, keywords, keywordWeight, tfidfWeight, textRankWeight);
        }

        private void ShowKeywordFeedback(string summary)
        {
            List<string> keywords = string.IsNullOrWhiteSpace(KeywordsTextBox.Text)
                ? new List<string>()
                : KeywordsTextBox.Text.Split(',')
                                     .Select(k => k.Trim())
                                     .Where(k => !string.IsNullOrEmpty(k))
                                     .ToList();

            if (keywords.Any() && !summary.ContainsAny(keywords))
            {
                MessageBox.Show(ResourceManagerHelper.Instance.NoKeywordsFound,
                              ResourceManagerHelper.Instance.WarningTitle,
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
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
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(OutputTextBox.Text))
            {
                System.Windows.Clipboard.SetText(OutputTextBox.Text);
                MessageBox.Show(
                    ResourceManagerHelper.Instance.CopyToClipboardSuccess,
                    ResourceManagerHelper.Instance.SuccessTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    public static class StringExtensions
    {
        public static bool ContainsAny(this string text, List<string> keywords)
        {
            return keywords.Any(k => text.ToLower().Contains(k.ToLower()));
        }
    }
} 