using SqlForgeWpf.Models;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SqlForgeWpf.Windows
{
    public partial class DbConfigWindow : Window
    {
        private List<ConnectionProfile> _profiles;
        private readonly MainWindow _mainWindow;

        public DbConfigWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _profiles = ProfileManager.LoadProfiles();
            RefreshProfileDropdown();
        }

        private void RefreshProfileDropdown()
        {
            CmbProfiles.ItemsSource = null;
            CmbProfiles.ItemsSource = _profiles;
        }

        private void CmbProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbProfiles.SelectedItem is ConnectionProfile p)
            {
                TxtProfileName.Text = p.ProfileName;
                CmbDbType.Text = p.DbType;
                TxtHost.Text = p.Host;
                TxtPort.Text = p.Port;
                TxtDbName.Text = p.Database;
                TxtUser.Text = p.Username;
                TxtPass.Password = p.Password;
            }
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtProfileName.Text)) return;

            var existing = _profiles.FirstOrDefault(p => p.ProfileName == TxtProfileName.Text);
            if (existing != null) _profiles.Remove(existing);

            _profiles.Add(new ConnectionProfile
            {
                ProfileName = TxtProfileName.Text,
                DbType = CmbDbType.Text,
                Host = TxtHost.Text,
                Port = TxtPort.Text,
                Database = TxtDbName.Text,
                Username = TxtUser.Text,
                Password = TxtPass.Password
            });

            ProfileManager.SaveProfiles(_profiles);
            RefreshProfileDropdown();
            MessageBox.Show("Profile Saved!");
        }

        private void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (CmbProfiles.SelectedItem is ConnectionProfile p)
            {
                _profiles.Remove(p);
                ProfileManager.SaveProfiles(_profiles);
                RefreshProfileDropdown();
            }
        }

        private void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            AppSettings.DbType = CmbDbType.Text;
            AppSettings.DbHost = TxtHost.Text;
            AppSettings.DbPort = TxtPort.Text;
            AppSettings.DbName = TxtDbName.Text;
            AppSettings.DbUser = TxtUser.Text;
            AppSettings.DbPassword = TxtPass.Password;

            _mainWindow.SyncDatabaseTree();
            this.Close();
        }
    }
}