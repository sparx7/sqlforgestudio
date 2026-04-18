using SqlForgeWpf.Models;
using SqlForgeWpf.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SqlForgeWpf.Windows
{
    public partial class LmConfigWindow : Window
    {
        private readonly LmStudioService _lmService;

        public LmConfigWindow()
        {
            InitializeComponent();
            _lmService = new LmStudioService();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TxtHost.Text = AppSettings.LmHost;
            TxtPort.Text = AppSettings.LmPort;
            TxtEndpoint.Text = AppSettings.LmEndpoint;
            _ = RefreshActiveModelUi();
        }

        private async Task RefreshActiveModelUi()
        {
            try
            {
                var loaded = await _lmService.GetCurrentlyLoadedModelsAsync();
                if (loaded.Count > 0)
                {
                    AppSettings.ActiveModel = loaded.First();
                    TxtActiveModel.Text = AppSettings.ActiveModel;
                    TxtActiveModel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "SuccessColor");
                }
                else
                {
                    AppSettings.ActiveModel = "[None]";
                    TxtActiveModel.Text = "[None]";
                    TxtActiveModel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextMuted");
                }
            }
            catch { TxtActiveModel.Text = "Cannot reach server"; }
        }

        // --- NEW: Helper method to lock UI and show progress bar ---
        private void SetLoadingState(bool isLoading)
        {
            LmLoadingBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            BtnLoad.IsEnabled = !isLoading;
            BtnUnload.IsEnabled = !isLoading;
            BtnSaveAndFetch.IsEnabled = !isLoading;
            CmbModels.IsEnabled = !isLoading;
        }

        private async void BtnSaveAndFetch_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.LmHost = TxtHost.Text;
            AppSettings.LmPort = TxtPort.Text;
            AppSettings.LmEndpoint = TxtEndpoint.Text;

            SetLoadingState(true);
            try
            {
                var models = await _lmService.GetCurrentlyLoadedModelsAsync();
                CmbModels.ItemsSource = models;
                if (models.Count > 0) CmbModels.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not connect to {TxtHost.Text}:{TxtPort.Text}.\n\n{ex.Message}", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            if (CmbModels.SelectedItem == null) return;

            SetLoadingState(true);
            try
            {
                await _lmService.LoadModelToRamAsync(CmbModels.SelectedItem.ToString()!);
                await RefreshActiveModelUi();
                MessageBox.Show("Model Loaded Successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Load Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async void BtnUnload_Click(object sender, RoutedEventArgs e)
        {
            SetLoadingState(true);
            try
            {
                // Actually command LM Studio to evict the models from RAM
                await _lmService.ClearLoadedModelsAsync();
                AppSettings.ActiveModel = "[None]";
                await RefreshActiveModelUi();
                MessageBox.Show("Memory cleared successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unload Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetLoadingState(false);
            }
        }
    }
}