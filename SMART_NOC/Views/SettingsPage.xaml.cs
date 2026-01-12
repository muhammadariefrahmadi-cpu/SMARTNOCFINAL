using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SMART_NOC.Services;
using System;
using System.Diagnostics;
using System.IO;

namespace SMART_NOC.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var config = LocalSettings.Load();
            TxtApiKey.Password = config.GeminiApiKey;
            TgAutoSave.IsOn = config.IsAutoSaveEnabled;
        }

        private void BtnSaveApi_Click(object sender, RoutedEventArgs e)
        {
            var config = LocalSettings.Load();
            config.GeminiApiKey = TxtApiKey.Password;
            config.IsAutoSaveEnabled = TgAutoSave.IsOn;
            config.Save();

            BtnSaveApi.Content = "Tersimpan! ✅";
            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            t.Tick += (s, args) => { BtnSaveApi.Content = "Simpan Kunci"; t.Stop(); };
            t.Start();
        }

        private void BtnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMART_NOC");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", path);
            }
            catch { }
        }
    }
}