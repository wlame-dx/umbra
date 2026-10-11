using System;
using System.IO;

namespace Umbra
{
    /// <summary>Basit gunluk. Hem dosyaya yazar hem arayuze satir yollar.</summary>
    internal static class Log
    {
        private static readonly object Gate = new object();
        private static string _path;

        public static event Action<string> Line;

        public static string FilePath { get { return _path; } }

        public static void Init()
        {
            try
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Umbra");
                Directory.CreateDirectory(dir);
                _path = Path.Combine(dir, "umbra.log");
            }
            catch
            {
                _path = null;
            }
        }

        public static void Info(string message) { Write("INFO", message); }
        public static void Warn(string message) { Write("WARN", message); }
        public static void Error(string message) { Write("ERROR", message); }

        private static void Write(string level, string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "   " + message;

            var handler = Line;
            if (handler != null)
            {
                try { handler(line); }
                catch { }
            }

            if (_path == null) return;
            lock (Gate)
            {
                try
                {
                    File.AppendAllText(_path,
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [" + level + "] " + message + Environment.NewLine);
                }
                catch { }
            }
        }
    }
}
