using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Threading;

namespace SMART_NOC
{
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Menyiapkan wrapper COM untuk Windows App SDK
            WinRT.ComWrappersSupport.InitializeComWrappers();

            // Memulai Aplikasi XAML
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                // Memanggil App.xaml
                new App();
            });
        }
    }
}