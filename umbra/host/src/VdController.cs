using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Umbra
{
    /// <summary>
    /// Sanal (ikinci) ekranin yasam dongusu. Umbra imzasiz kernel surucusu
    /// icermez; exe'nin yanindaki "vdriver" klasorundeki yardimci betikleri
    /// calistirir (usbmmidd / Virtual-Display-Driver tabanli). Surucu dosyalari
    /// yoksa sadece uyarir, mevcut ekran yayinlanir.
    ///
    /// Not: Bu islemler yonetici hakki ister ve UAC penceresi acilabilir. Bu
    /// yuzden hepsi arka plan is parcaciginda calisir; arayuz donmaz.
    /// </summary>
    internal static class VdController
    {
        private const int TimeoutMs = 60000;

        private static string ScriptDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vdriver"); }
        }

        /// <summary>vdriver klasorunun yolu.</summary>
        public static string ScriptDirPath
        {
            get { return ScriptDir; }
        }

        /// <summary>vdriver icindeki bir betigin tam yolu; yoksa null.</summary>
        public static string ScriptPath(string fileName)
        {
            try
            {
                string path = Path.Combine(ScriptDir, fileName);
                return File.Exists(path) ? path : null;
            }
            catch { return null; }
        }

        private static string BinDir
        {
            get { return Path.Combine(ScriptDir, "bin"); }
        }

        /// <summary>Yardimci betikler yerinde mi?</summary>
        public static bool IsAvailable()
        {
            try { return File.Exists(Path.Combine(ScriptDir, "create.cmd")); }
            catch { return false; }
        }

        /// <summary>
        /// Surucu dosyalari (vdriver\bin) yerinde mi? Betikler var ama surucu
        /// yoksa bosuna UAC acmayalim.
        /// </summary>
        public static bool IsDriverReady()
        {
            try
            {
                if (!Directory.Exists(BinDir)) return false;
                if (File.Exists(Path.Combine(BinDir, "deviceinstaller64.exe"))) return true;

                // Virtual-Display-Driver tarzi: en az bir .inf ve .sys dosyasi.
                bool hasInf = Directory.GetFiles(BinDir, "*.inf").Length > 0;
                bool hasSys = Directory.GetFiles(BinDir, "*.sys").Length > 0;
                return hasInf && hasSys;
            }
            catch { return false; }
        }

        /// <summary>Sanal ekrani olusturur. Yonetici hakki istenirse UAC acilir;
        /// kullanici iptal ederse false doner.</summary>
        public static bool Create()
        {
            return Run("create.cmd");
        }

        /// <summary>Sanal ekrani kaldirir.</summary>
        public static bool Remove()
        {
            return Run("remove.cmd");
        }

        /// <summary>Sanal ekrani arka planda olusturur; sonuc "done" ile bildirilir.</summary>
        public static void CreateAsync(Action<bool> done)
        {
            RunAsync("create.cmd", done);
        }

        /// <summary>Sanal ekrani arka planda kaldirir; sonuc "done" ile bildirilir.</summary>
        public static void RemoveAsync(Action<bool> done)
        {
            RunAsync("remove.cmd", done);
        }

        private static void RunAsync(string script, Action<bool> done)
        {
            var worker = new Thread(delegate()
            {
                bool ok = false;
                try { ok = Run(script); }
                catch { }
                if (done != null)
                {
                    try { done(ok); }
                    catch { }
                }
            });
            worker.IsBackground = true;
            worker.Name = "umbra-vd-" + script;
            worker.Start();
        }

        private static bool Run(string script)
        {
            string path;
            try { path = Path.Combine(ScriptDir, script); }
            catch { return false; }

            if (!File.Exists(path)) return false;

            try
            {
                var psi = new ProcessStartInfo();
                psi.FileName = path;
                psi.WorkingDirectory = ScriptDir;
                psi.UseShellExecute = true;
                psi.Verb = "runas";       // surucu islemleri yonetici ister
                psi.WindowStyle = ProcessWindowStyle.Hidden;

                using (var process = Process.Start(psi))
                {
                    if (process == null) return false;
                    if (!process.WaitForExit(TimeoutMs))
                    {
                        try { process.Kill(); } catch { }
                        return false;
                    }
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                // UAC iptal edildi ya da betik baslatilamadi
                return false;
            }
        }
    }
}
