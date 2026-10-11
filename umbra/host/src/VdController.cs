using System;
using System.Diagnostics;
using System.IO;

namespace Umbra
{
    /// <summary>
    /// Sanal (ikinci) ekranin yasam dongusu. Umbra imzasiz kernel surucusu
    /// icermez; exe'nin yanindaki "vdriver" klasorundeki yardimci betikleri
    /// calistirir (usbmmidd / IddSampleDriver tabanli). Betik yoksa sadece uyarir.
    /// </summary>
    internal static class VdController
    {
        private static string ScriptDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vdriver"); }
        }

        /// <summary>Yardimci betikler yerinde mi?</summary>
        public static bool IsAvailable()
        {
            return File.Exists(Path.Combine(ScriptDir, "create.cmd"));
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

        private static bool Run(string script)
        {
            string path = Path.Combine(ScriptDir, script);
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
                    if (!process.WaitForExit(25000))
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