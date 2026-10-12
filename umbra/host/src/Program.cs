using System;
using System.Windows;
using System.Windows.Threading;

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

            // Arayuz thread'inde olusan beklenmeyen bir hata uygulamayi kapatmasin.
            // Hata gunluge yazilir, uygulama calismaya devam eder.
            app.DispatcherUnhandledException += delegate(object sender, DispatcherUnhandledExceptionEventArgs e)
            {
                Log.Error("Arayuz hatasi (yoksayildi): " + (e.Exception == null ? "?" : e.Exception.ToString()));
                e.Handled = true;
            };

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
            Log.Info("Uygulama kapandi.");

            // Arka planda yonetici istemi (UAC) bekleyen bir is parcacigi olsa bile
            // surec hemen sonlansin.
            Environment.Exit(0);
        }
    }
}
