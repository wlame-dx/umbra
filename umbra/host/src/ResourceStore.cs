using System;
using System.IO;
using System.Reflection;

namespace Umbra
{
    /// <summary>
    /// Arayuz ve web dosyalarini yukler. Exe'nin yanindaki klasor varsa onu,
    /// yoksa exe icine gomulmus kopyayi kullanir.
    /// </summary>
    internal static class ResourceStore
    {
        public static string BaseDirectory
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static string LoadText(string resourceName, string relativePath)
        {
            try
            {
                string full = Path.Combine(BaseDirectory, relativePath);
                if (File.Exists(full)) return File.ReadAllText(full);
            }
            catch { }

            using (var stream = typeof(ResourceStore).Assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream))
                    return reader.ReadToEnd();
            }
        }

        public static byte[] LoadBytes(string resourceName, string relativePath)
        {
            try
            {
                string full = Path.Combine(BaseDirectory, relativePath);
                if (File.Exists(full)) return File.ReadAllBytes(full);
            }
            catch { }

            using (var stream = typeof(ResourceStore).Assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;
                using (var memory = new MemoryStream())
                {
                    var buffer = new byte[81920];
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        memory.Write(buffer, 0, read);
                    return memory.ToArray();
                }
            }
        }
    }
}
