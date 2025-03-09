using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace DarkHub
{
    public class SummaX
    {
        private readonly int SummaryMaxLength;
        private readonly string Language;
        private readonly List<string> StopWords;
        private readonly List<string> Keywords;
        private readonly double KeywordWeight;
        private readonly double TfidfWeight;
        private readonly double TextRankWeight;

        public SummaX(int maxLength = 150, string language = "pt", List<string> keywords = null,
                      double keywordWeight = 1.5, double tfidfWeight = 1.0, double textRankWeight = 1.0)
        {
            SummaryMaxLength = maxLength;
            Language = language;
            StopWords = LoadStopWords(language);
            Keywords = keywords?.Select(k => k.ToLower().Trim()).ToList() ?? new List<string>();
            KeywordWeight = keywordWeight;
            TfidfWeight = tfidfWeight;
            TextRankWeight = textRankWeight;
        }

        private List<string> LoadStopWords(string language)
        {
            switch (language.ToLower())
            {
                case "pt":
                    return new List<string> { "e", "ou", "de", "a", "o", "em", "para", "com", "que", "as", "os" };
                case "en":
                    return new List<string> { "and", "or", "the", "a", "an", "in", "to" };
                default:
                    return new List<string>();
            }
        }

        public string Summarize(string inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
                return "Texto de entrada inválido.";
            return ProcessText(inputText);
        }

        public string SummarizeFromPdf(string pdfFilePath)
        {
            try
            {
                string extractedText = ExtractTextFromPdf(pdfFilePath);
                if (string.IsNullOrWhiteSpace(extractedText))
                    return "Nenhum texto extraído do PDF.";
                return ProcessText(extractedText);
            }
            catch (Exception ex)
            {
                return $"Erro ao processar o PDF: {ex.Message}";
            }
        }

        private string ExtractTextFromPdf(string pdfFilePath)
        {
            var text = new StringBuilder();
            using (var pdfReader = new PdfReader(pdfFilePath))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    text.AppendLine(pageText);
                }
            }
            return text.ToString();
        }

        private string ProcessText(string inputText)
        {
            var (sentences, words) = PreprocessText(inputText);
            var scores = AnalyzeRelevance(sentences, words);
            var selectedSentences = SelectSentences(sentences, scores);
            return GenerateSummary(selectedSentences, inputText);
        }

        private (List<string> sentences, List<List<string>> words) PreprocessText(string text)
        {
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(s => s.Trim())
                               .Where(s => s.Length > 15 && !s.StartsWith("•") && !s.Contains("US$")) // Filtra sentenças curtas e ruídos
                               .ToList();

            var words = sentences.Select(s => s.Split(new[] { ' ', ',', ';', ':', '-', '–' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(w => w.ToLower().Trim(new[] { '(', ')', '[', ']', '"', '\'' }))
                                              .Where(w => !StopWords.Contains(w) && !string.IsNullOrEmpty(w) && w.Length > 2)
                                              .ToList())
                                .ToList();

            return (sentences, words);
        }

        private Dictionary<string, double> AnalyzeRelevance(List<string> sentences, List<List<string>> words)
        {
            var scores = new Dictionary<string, double>();
            int totalDocs = sentences.Count;

            var wordFreq = new Dictionary<string, int>();
            foreach (var wordList in words)
            {
                foreach (var word in wordList)
                {
                    if (wordFreq.TryGetValue(word, out int freq))
                        wordFreq[word] = freq + 1;
                    else
                        wordFreq[word] = 1;
                }
            }

            var tfidfScores = CalculateTfidf(sentences, words, wordFreq, totalDocs);
            var textRankScores = CalculateTextRank(sentences, words);

            for (int i = 0; i < sentences.Count; i++)
            {
                double tfidf = tfidfScores[sentences[i]] * TfidfWeight;
                double textRank = textRankScores[sentences[i]] * TextRankWeight;
                double keywordScore = CalculateKeywordScore(sentences[i]) * KeywordWeight;
                double positionScore = CalculatePositionScore(i, sentences.Count); // Prioriza sentenças iniciais
                double lengthScore = CalculateLengthScore(sentences[i]); // Prioriza sentenças de tamanho médio

                scores[sentences[i]] = tfidf + textRank + keywordScore + positionScore + lengthScore;
            }

            return scores;
        }

        private double CalculatePositionScore(int index, int totalSentences)
        {
            return (totalSentences - index) / (double)totalSentences * 0.5; // Maior peso para sentenças iniciais
        }

        private double CalculateLengthScore(string sentence)
        {
            int wordCount = sentence.Split(' ').Length;
            return Math.Min(wordCount / 20.0, 1.0); // Favorece sentenças de 10-20 palavras
        }

        private Dictionary<string, double> CalculateTfidf(List<string> sentences, List<List<string>> words,
                                                         Dictionary<string, int> wordFreq, int totalDocs)
        {
            var tfidfScores = new Dictionary<string, double>();
            for (int i = 0; i < sentences.Count; i++)
            {
                double score = 0;
                foreach (var word in words[i].Distinct())
                {
                    double tf = (double)words[i].Count(w => w == word) / words[i].Count;
                    double idf = Math.Log((double)totalDocs / (1 + wordFreq[word]));
                    score += tf * idf;
                }
                tfidfScores[sentences[i]] = score;
            }
            return tfidfScores;
        }

        private Dictionary<string, double> CalculateTextRank(List<string> sentences, List<List<string>> words)
        {
            var scores = new Dictionary<string, double>();
            var similarityMatrix = new double[sentences.Count, sentences.Count];

            for (int i = 0; i < sentences.Count; i++)
            {
                for (int j = 0; j < sentences.Count; j++)
                {
                    if (i == j) continue;
                    var commonWords = words[i].Intersect(words[j]).Count();
                    similarityMatrix[i, j] = commonWords / (double)(words[i].Count + words[j].Count);
                }
            }

            for (int i = 0; i < sentences.Count; i++)
            {
                double score = 1.0;
                for (int iter = 0; iter < 10; iter++)
                {
                    double newScore = 0.15;
                    for (int j = 0; j < sentences.Count; j++)
                    {
                        if (i != j)
                        {
                            double currentScore;
                            if (scores.TryGetValue(sentences[j], out currentScore))
                                newScore += 0.85 * similarityMatrix[j, i] * currentScore;
                            else
                                newScore += 0.85 * similarityMatrix[j, i] * 1.0;
                        }
                    }
                    score = newScore;
                }
                scores[sentences[i]] = score;
            }

            return scores;
        }

        private double CalculateKeywordScore(string sentence)
        {
            if (!Keywords.Any()) return 0;
            double score = 0;
            string lowerSentence = sentence.ToLower();
            foreach (var keyword in Keywords)
            {
                if (lowerSentence.Contains(keyword))
                    score += 1.0;
            }
            return score;
        }

        private List<string> SelectSentences(List<string> sentences, Dictionary<string, double> scores)
        {
            var orderedSentences = scores.OrderByDescending(s => s.Value)
                                        .Select(s => s.Key)
                                        .ToList();

            var selected = new List<string>();
            int currentLength = 0;

            foreach (var sentence in orderedSentences)
            {
                int wordCount = sentence.Split(' ').Length;
                if (currentLength + wordCount <= SummaryMaxLength)
                {
                    selected.Add(sentence);
                    currentLength += wordCount;
                }
            }

            return selected.OrderBy(s => sentences.IndexOf(s)).ToList();
        }

        private string GenerateSummary(List<string> selectedSentences, string originalText)
        {
            if (!selectedSentences.Any()) return "Nenhum resumo gerado.";

            var summary = new StringBuilder();
            var filteredSentences = selectedSentences.Where(s => !s.Contains("•") && !s.Contains("US$")).ToList();

            foreach (var sentence in filteredSentences)
            {
                string cleanedSentence = sentence.Trim();
                if (!cleanedSentence.EndsWith("."))
                    cleanedSentence += ".";
                summary.Append(cleanedSentence + " ");
            }

            return summary.ToString().Trim();
        }
    }
}