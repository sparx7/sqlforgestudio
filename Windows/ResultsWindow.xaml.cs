using SqlForgeWpf.Services;
using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace SqlForgeWpf.Windows
{
    public partial class ResultsWindow : Window
    {
        private readonly DataTable _dataTable;
        private readonly LmStudioService _lmService;
        private readonly string _activeModel;
        private readonly string _userPrompt;
        private string _lastGeneratedSummaryHtml = "";

        private readonly string _htmlBg;
        private readonly string _htmlPanel;
        private readonly string _htmlText;
        private readonly string _htmlBorder;
        private readonly string _htmlAccent = "#2979FF";

        public ResultsWindow(DataTable data, LmStudioService lmService, string activeModel, string userPrompt)
        {
            InitializeComponent();
            _dataTable = data;
            _lmService = lmService;
            _activeModel = activeModel;
            _userPrompt = userPrompt;

            var bgBrush = Application.Current.Resources["BgWindow"] as SolidColorBrush;
            bool isDark = bgBrush != null && bgBrush.Color.R < 100;

            _htmlBg = isDark ? "#121212" : "#F5F7FA";
            _htmlPanel = isDark ? "#1E1E1E" : "#FFFFFF";
            _htmlText = isDark ? "#FFFFFF" : "#000000";
            _htmlBorder = isDark ? "#424242" : "#CFD8DC";

            HtmlBrowser.NavigateToString(GenerateHtml(_dataTable));
        }

        private async void BtnGenerateSummary_Click(object sender, RoutedEventArgs e)
        {
            if (_dataTable.Rows.Count == 0) return;

            BtnGenerateSummary.IsEnabled = false;
            SummaryLoadingBar.Visibility = Visibility.Visible;

            try
            {
                string csvData = ConvertToCsv(_dataTable);
                string systemPrompt = $@"You are an expert Data Analyst. The user originally asked: ""{_userPrompt}"". 
Write an insightful summary answering the question using the provided CSV data. Output ONLY raw HTML tags (h2, p, ul). No markdown blocks.";

                // Temp 0.6 for summary
                string rawAiResponse = await _lmService.SendChatMessageAsync(_activeModel, systemPrompt, csvData, 0.6);
                string cleanHtml = rawAiResponse.Replace("```html", "").Replace("```", "").Trim();

                _lastGeneratedSummaryHtml = $@"
                <html style='background-color:{_htmlBg};'>
                <head>
                    <style>
                        html, body {{ background-color: {_htmlPanel}; color: {_htmlText}; font-family: 'Segoe UI', sans-serif; margin: 0; padding: 15px; line-height: 1.6; height: 100%; }}
                        h1, h2, h3, h4, strong, b, th {{ color: {_htmlAccent}; }}
                        table {{ border-collapse: collapse; width: 100%; margin-top: 15px; }}
                        th, td {{ padding: 10px; border-bottom: 1px solid {_htmlBorder}; text-align: left; }}
                        ::-webkit-scrollbar {{ width: 8px; }}
                        ::-webkit-scrollbar-track {{ background: {_htmlPanel}; }}
                        ::-webkit-scrollbar-thumb {{ background: {_htmlBorder}; border-radius: 4px; }}
                    </style>
                </head>
                <body>{cleanHtml}</body>
                </html>";

                SummaryBrowser.NavigateToString(_lastGeneratedSummaryHtml);
            }
            catch (Exception ex) { MessageBox.Show($"Failed: {ex.Message}"); }
            finally { BtnGenerateSummary.IsEnabled = true; SummaryLoadingBar.Visibility = Visibility.Collapsed; }
        }

        private void BtnSaveHtml_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_lastGeneratedSummaryHtml)) { MessageBox.Show("Please generate a summary first."); return; }
            var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "HTML Document|*.html", DefaultExt = ".html" };
            if (dialog.ShowDialog() == true) System.IO.File.WriteAllText(dialog.FileName, _lastGeneratedSummaryHtml);
        }

        private void BtnSavePdf_Click(object sender, RoutedEventArgs e)
        {
            if (SummaryBrowser.Document != null)
            {
                dynamic doc = SummaryBrowser.Document;
                doc.execCommand("Print", true, null);
            }
        }

        private string ConvertToCsv(DataTable table)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", table.Columns.Cast<DataColumn>().Select(c => $"\"{c.ColumnName.Replace("\"", "\"\"")}\"")));
            foreach (DataRow row in table.Rows) sb.AppendLine(string.Join(",", row.ItemArray.Select(f => $"\"{f?.ToString()?.Replace("\"", "\"\"")}\"")));
            return sb.ToString();
        }

        private string GenerateHtml(DataTable table)
        {
            var sb = new StringBuilder();
            sb.AppendLine($@"<html style='background-color:{_htmlBg};'><body style='background-color:{_htmlBg}; color:{_htmlText}; font-family:""Segoe UI"", sans-serif; margin:0; padding:15px;'>");
            sb.AppendLine($"<table style='border-collapse:collapse; width:100%; background-color:{_htmlPanel}; border-radius:8px; overflow:hidden;'>");
            sb.AppendLine($"<tr style='background-color:{_htmlBg}; color:{_htmlAccent}; text-align:left;'>");
            foreach (DataColumn col in table.Columns) sb.AppendLine($"<th style='padding:12px; border-bottom:2px solid {_htmlAccent};'>{col.ColumnName}</th>");
            sb.AppendLine("</tr>");

            bool isAlt = false;
            foreach (DataRow row in table.Rows)
            {
                sb.AppendLine($"<tr style='background-color:{(isAlt ? _htmlBg : _htmlPanel)};'>");
                foreach (var item in row.ItemArray) sb.AppendLine($"<td style='padding:10px; border-bottom:1px solid {_htmlBorder};'>{item}</td>");
                sb.AppendLine("</tr>");
                isAlt = !isAlt;
            }
            sb.AppendLine("</table></body></html>");
            return sb.ToString();
        }
    }
}