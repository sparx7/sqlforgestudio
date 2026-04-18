using SqlForgeWpf.Models;
using SqlForgeWpf.Services;
using SqlForgeWpf.Windows;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SqlForgeWpf
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseService _dbService;
        private readonly LmStudioService _lmService;

        private string _cachedSchema = "No database connected.";
        private int _queryCounter = 0;
        private bool _isDarkMode = true;

        public MainWindow()
        {
            InitializeComponent();
            _dbService = new DatabaseService();
            _lmService = new LmStudioService();
            WriteLog("Application Booting...", "INFO");
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var loadedModels = await _lmService.GetCurrentlyLoadedModelsAsync();
                if (loadedModels.Count > 0) AppSettings.ActiveModel = loadedModels.First();
            }
            catch { WriteLog("LM Studio not found locally.", "WARN"); }

            UpdateAiLedStatus();
        }

        // ==========================================
        // THE MAIN AI GENERATION WORKFLOW (Logs Fixed!)
        // ==========================================
        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string text = TxtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            if (string.IsNullOrEmpty(AppSettings.DbType))
            {
                WriteLog("Please connect to a database first.", "WARN");
                return;
            }

            AppendChatBubble("User", text, "TextPrimary", "BgInput");
            TxtChatInput.Text = "";
            BtnSend.IsEnabled = false;
            AiLoadingBar.Visibility = Visibility.Visible;

            try
            {
                WriteLog($"Starting AI query generation for {AppSettings.DbType}...", "INFO");
                string systemPrompt = PromptManager.GetSystemPrompt(AppSettings.DbType, _cachedSchema);

                var sw = Stopwatch.StartNew();
                string rawAiResponse = await _lmService.SendChatMessageAsync(AppSettings.ActiveModel, systemPrompt, text, 0.0);
                sw.Stop();
                long aiTime = sw.ElapsedMilliseconds;

                WriteLog($"AI generation completed in {aiTime}ms.", "SUCCESS");
                string cleanSql = rawAiResponse.Replace("```sql", "").Replace("```", "").Trim();

                
                if (AppSettings.DbType.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
                {
                    cleanSql = ForcePostgresQuotes(cleanSql);
                }
                else if (AppSettings.DbType.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
                {
                   
                    cleanSql = RemoveOracleQuotes(cleanSql);
                }

                AppendChatBubble("AI", cleanSql, "TextPrimary", "BgPanel");
                FileSystemManager.SaveChatInteraction(AppSettings.DbType, text, cleanSql);

                WriteLog($"Executing generated query on {AppSettings.DbType} server...", "INFO");

                sw.Restart();
                var dataTable = await _dbService.ExecuteSqlAsync(
                    AppSettings.DbType, AppSettings.DbHost, AppSettings.DbPort, AppSettings.DbName, AppSettings.DbUser, AppSettings.DbPassword, cleanSql);
                sw.Stop();
                long dbTime = sw.ElapsedMilliseconds;

                _queryCounter++;
                TxtQueryCount.Text = $"QUERIES: {_queryCounter}";

                WriteLog($"Query executed successfully. Returned {dataTable.Rows.Count} rows in {dbTime}ms.", "SUCCESS");
                AppendChatBubble("System", $"⏱️ Generated in {aiTime}ms | Executed in {dbTime}ms | Returned {dataTable.Rows.Count} rows.", "SuccessColor", "BgWindow");

                var resultsWindow = new ResultsWindow(dataTable, _lmService, AppSettings.ActiveModel, text) { Owner = this };
                resultsWindow.Show();
            }
            catch (Exception ex)
            {
                WriteLog($"Workflow failed: {ex.Message}", "ERROR");
                AppendChatBubble("System", $"Execution Failed: {ex.Message}", "DangerColor", "BgWindow");
            }
            finally
            {
                BtnSend.IsEnabled = true;
                AiLoadingBar.Visibility = Visibility.Collapsed;
                TxtChatInput.Focus();
            }
        }

        // ==========================================
        // UI & EVENT CONTROLS
        // ==========================================
        private void BtnAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutWin = new AboutWindow { Owner = this };
            aboutWin.ShowDialog();
        }

        private void TxtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnSend_Click(sender, e);
        }

        private void BtnClearChat_Click(object sender, RoutedEventArgs e)
        {
            ChatHistoryPanel.Children.Clear();
            AppendChatBubble("System", "✨ Chat history cleared.", "TextMuted", "Transparent");
            WriteLog("Chat history cleared.", "INFO");
        }

        private void BtnOpenDbConfig_Click(object sender, RoutedEventArgs e)
        {
            var win = new DbConfigWindow(this) { Owner = this };
            win.ShowDialog();
        }

        private void BtnOpenLmConfig_Click(object sender, RoutedEventArgs e)
        {
            var win = new LmConfigWindow { Owner = this };
            win.ShowDialog();
            UpdateAiLedStatus();
        }

        // ==========================================
        // INTERNAL HELPERS
        // ==========================================
        public async void SyncDatabaseTree()
        {
            DbTreeView.Items.Clear();
            WriteLog($"Connecting to {AppSettings.DbName} ({AppSettings.DbType})...", "INFO");

            try
            {
                var treeData = await _dbService.GetDatabaseTreeAsync(
                    AppSettings.DbType, AppSettings.DbHost, AppSettings.DbPort, AppSettings.DbName, AppSettings.DbUser, AppSettings.DbPassword);

                var dbNode = new TreeViewItem { Header = $"🗄️ {AppSettings.DbName}", FontWeight = FontWeights.Bold, IsExpanded = true };
                dbNode.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimary");
                DbTreeView.Items.Add(dbNode);

                var schemaBuilder = new StringBuilder();
                schemaBuilder.AppendLine($"Database: {AppSettings.DbName}");

                foreach (var schema in treeData)
                {
                    var schemaNode = new TreeViewItem { Header = $"📂 {schema.Key}" };
                    schemaNode.SetResourceReference(TreeViewItem.ForegroundProperty, "AccentPrimary");
                    dbNode.Items.Add(schemaNode);

                    foreach (var table in schema.Value)
                    {
                        var tableNode = new TreeViewItem { Header = $"📄 {table.Key}" };
                        tableNode.SetResourceReference(TreeViewItem.ForegroundProperty, "TextPrimary");
                        schemaNode.Items.Add(tableNode);

                        schemaBuilder.AppendLine($"Table: {schema.Key}.{table.Key}");
                        schemaBuilder.AppendLine($"Columns: " + string.Join(", ", table.Value.Select(c => $"{c.Name} ({c.Type})")));

                        foreach (var col in table.Value)
                        {
                            var colNode = new TreeViewItem { Header = $"{col.Name} ({col.Type})" };
                            colNode.SetResourceReference(TreeViewItem.ForegroundProperty, "TextMuted");
                            tableNode.Items.Add(colNode);
                        }
                    }
                }

                string cachedSchema = SchemaManager.GetCachedSchema(AppSettings.DbHost, AppSettings.DbName);

                if (cachedSchema != null)
                {
                    _cachedSchema = cachedSchema;
                    WriteLog("Loaded optimized schema from local cache.", "INFO");
                }
                else
                {
                    // Build the ultra-dense format and save it
                    _cachedSchema = SchemaManager.BuildConciseSchema(treeData);
                    SchemaManager.CacheSchemaLocally(AppSettings.DbHost, AppSettings.DbName, _cachedSchema);
                    WriteLog("Generated and cached optimized schema.", "INFO");
                }

                SetLedStatus(true);
                WriteLog("Database connected successfully.", "SUCCESS");
            }
            catch (Exception ex)
            {
                SetLedStatus(false);
                WriteLog($"Database Connection Failed: {ex.Message}", "ERROR");
            }
        }

        private string ForcePostgresQuotes(string sql)
        {
            var stringLiterals = new System.Collections.Generic.Dictionary<string, string>();
            int counter = 0;
            string safeSql = Regex.Replace(sql, @"'[^']*'", match =>
            {
                string placeholder = $"__STR{counter++}__";
                stringLiterals[placeholder] = match.Value;
                return placeholder;
            });
            safeSql = Regex.Replace(safeSql, @"\""?([a-zA-Z_]\w*)\""?\.\""?([a-zA-Z_]\w*)\""?\.\""?([a-zA-Z_]\w*)\""?", "\"$1\".\"$2\".\"$3\"");
            safeSql = Regex.Replace(safeSql, @"\""?([a-zA-Z_]\w*)\""?\.\""?([a-zA-Z_]\w*)\""?", "\"$1\".\"$2\"");
            foreach (var kvp in stringLiterals) safeSql = safeSql.Replace(kvp.Key, kvp.Value);
            return safeSql;
        }

        private void BtnThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            _isDarkMode = !_isDarkMode;
            var appResources = Application.Current.Resources;

            if (_isDarkMode)
            {
                appResources["BgWindow"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#121212"));
                appResources["BgPanel"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"));
                appResources["BgInput"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"));
                appResources["BorderInput"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#424242"));
                appResources["TextPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                appResources["TextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0BEC5"));
                appResources["AccentPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2979FF"));
            }
            else
            {
                appResources["BgWindow"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F5F7FA"));
                appResources["BgPanel"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                appResources["BgInput"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
                appResources["BorderInput"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CFD8DC"));
                appResources["TextPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#000000"));
                appResources["TextMuted"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#546E7A"));
                appResources["AccentPrimary"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2962FF"));
            }
        }

        private void UpdateAiLedStatus()
        {
            if (AppSettings.ActiveModel != "[None]")
            {
                LedAiStatus.Fill = (SolidColorBrush)Application.Current.Resources["SuccessColor"];
                TxtAiStatus.Text = $"AI READY: {AppSettings.ActiveModel}";
                TxtAiStatus.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            }
            else
            {
                LedAiStatus.Fill = (SolidColorBrush)Application.Current.Resources["DangerColor"];
                TxtAiStatus.Text = "AI DISCONNECTED";
                TxtAiStatus.SetResourceReference(TextBlock.ForegroundProperty, "TextMuted");
            }
        }

        private void SetLedStatus(bool isConnected)
        {
            LedDbStatus.Fill = (SolidColorBrush)Application.Current.Resources[isConnected ? "SuccessColor" : "DangerColor"];
        }

        private void AppendChatBubble(string role, string text, string fgResourceKey, string bgResourceKey)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 8, 0, 8),
                HorizontalAlignment = role == "User" ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };
            border.SetResourceReference(Border.BackgroundProperty, bgResourceKey);

            var tb = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 15, MaxWidth = 700 };
            tb.SetResourceReference(TextBlock.ForegroundProperty, fgResourceKey);

            border.Child = tb;
            ChatHistoryPanel.Children.Add(border);
            ChatScroll.ScrollToEnd();
        }

        private string RemoveOracleQuotes(string sql)
        {
            // 1. Temporarily hide single-quoted string literals so we don't accidentally modify them
            var stringLiterals = new System.Collections.Generic.Dictionary<string, string>();
            int counter = 0;
            string safeSql = Regex.Replace(sql, @"'[^']*'", match =>
            {
                string placeholder = $"__STR{counter++}__";
                stringLiterals[placeholder] = match.Value;
                return placeholder;
            });

            // 2. Strip all double quotes from the SQL structure
            safeSql = safeSql.Replace("\"", "");
            safeSql = safeSql.Replace(@"\r\n?|\n", "");
            safeSql = safeSql.Replace(";", "");


            // 3. Restore the protected string literals
            foreach (var kvp in stringLiterals)
            {
                safeSql = safeSql.Replace(kvp.Key, kvp.Value);
            }

            return safeSql;
        }
        private void BtnRefreshSchema_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(AppSettings.DbName)) return;

            WriteLog("Forcing schema refresh from database...", "WARN");

            // Delete the cache file so SyncDatabaseTree is forced to rebuild it
            SqlForgeWpf.Models.SchemaManager.ClearCache(AppSettings.DbHost, AppSettings.DbName);

            // Re-run the sync
            SyncDatabaseTree();
        }

        private void WriteLog(string message, string level = "INFO")
        {
            // Logs to the internal text file
            FileSystemManager.SaveLog(level, message);

            // Updates the visible UI console
            string colorKey = level switch { "ERROR" => "DangerColor", "SUCCESS" => "SuccessColor", "WARN" => "AccentPrimary", _ => "TextMuted" };
            var tb = new TextBlock { Text = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}", FontFamily = new FontFamily("Consolas"), FontSize = 13, Margin = new Thickness(0, 0, 0, 4) };
            tb.SetResourceReference(TextBlock.ForegroundProperty, colorKey);

            LogPanel.Children.Add(tb);
            LogScroll.ScrollToEnd();
        }
    }
}