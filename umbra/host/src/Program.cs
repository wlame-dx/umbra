using System;
using System.Windows;

namespace Umbra
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Log.Init();

            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                Log.Error("Beklenmeyen hata: " + (e.ExceptionObject == null ? "?" : e.ExceptionObject.ToString()));
            };

            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;

            MainWindow window;
            try
            {
                window = new MainWindow();
            }
            catch (Exception ex)
            {
                Log.Error("Arayuz yuklenemedi: " + ex);
                return;
            }

            app.MainWindow = window;
            app.Run(window);
        }
    }
}
