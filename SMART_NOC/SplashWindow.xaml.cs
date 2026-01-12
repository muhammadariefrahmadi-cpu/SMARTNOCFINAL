using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Windowing;
using System;
using System.IO;
using System.Threading.Tasks;
using SMART_NOC.Services;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace SMART_NOC.Views
{
    public sealed partial class SplashWindow : Window
    {
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        // Ukuran Base
        private const int AppWidth = 400;
        private const int AppHeight = 400;
        private const int CornerRadius = 24;

        // 🔥 SETTING FINAL: CROP 3 PIXEL 🔥
        // Ini akan membuang 3 pixel dari kulit terluar.
        // Kalau masih ada putihnya, ganti jadi 4.
        private const int CropOffset = 3;

        public SplashWindow()
        {
            this.InitializeComponent();

            var presenter = OverlappedPresenter.Create();
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(false, false);
            this.AppWindow.SetPresenter(presenter);

            CenterWindow();
            LoadSplashImage();

            this.Activated += (s, e) => ApplyCleanCut();
            this.Activated += SplashWindow_Activated;
        }

        private void ApplyCleanCut()
        {
            try
            {
                IntPtr hWnd = WindowNative.GetWindowHandle(this);
                uint dpi = GetDpiForWindow(hWnd);
                float scalingFactor = (float)dpi / 96;

                int w = (int)(AppWidth * scalingFactor);
                int h = (int)(AppHeight * scalingFactor);
                int r = (int)(CornerRadius * scalingFactor);

                // Hitung Offset berdasarkan scaling layar
                int offset = (int)(CropOffset * scalingFactor);

                // Potong Jendela: Mulai dari pixel ke-3, Berhenti sebelum 3 pixel terakhir
                IntPtr hRgn = CreateRoundRectRgn(
                    offset,             // Kiri
                    offset,             // Atas
                    w - offset,         // Kanan
                    h - offset,         // Bawah
                    r, r                // Lengkungan
                );

                SetWindowRgn(hWnd, hRgn, true);
            }
            catch { }
        }

        private void LoadSplashImage()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string imagePath = Path.Combine(basePath, "Assets", "SplashScreen.png");
                if (File.Exists(imagePath))
                {
                    if (SplashBorder.Background is ImageBrush brush)
                        brush.ImageSource = new BitmapImage(new Uri(imagePath));
                }
            }
            catch { }
        }

        private void CenterWindow()
        {
            var displayArea = DisplayArea.Primary;
            var screenWidth = displayArea.WorkArea.Width;
            var screenHeight = displayArea.WorkArea.Height;
            var x = (screenWidth - AppWidth) / 2;
            var y = (screenHeight - AppHeight) / 2;

            this.AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(x, y, AppWidth, AppHeight));
        }

        private async void SplashWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= SplashWindow_Activated;
            FadeInStoryboard.Begin();

            await Task.Run(() =>
            {
                try
                {
                    string excelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database_NOC.xlsx");
                    App.GlobalDatabase.LoadData(excelPath);
                }
                catch (Exception ex) { CrashLogger.Log(ex, "DB_INIT_ERROR"); }
            });

            await Task.Delay(1000);
            App.MainWindow = new MainWindow();
            App.MainWindow.Activate();
            this.Close();
        }
    }
}