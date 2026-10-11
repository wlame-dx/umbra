using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Umbra
{
    internal static class FrameEncoder
    {
        /// <summary>Kucultme katsayisi uygular (bant genisligi tasarrufu icin).</summary>
        public static BitmapSource Scale(BitmapSource source, double factor)
        {
            if (factor >= 0.995 || factor <= 0) return source;
            try
            {
                var scaled = new TransformedBitmap(source, new ScaleTransform(factor, factor));
                scaled.Freeze();
                return scaled;
            }
            catch
            {
                return source;
            }
        }

        /// <summary>Bitmap'i JPEG'e cevirir.</summary>
        public static byte[] ToJpeg(BitmapSource source, int quality)
        {
            int q = quality;
            if (q < 10) q = 10;
            if (q > 100) q = 100;

            var encoder = new JpegBitmapEncoder();
            encoder.QualityLevel = q;
            encoder.Frames.Add(BitmapFrame.Create(source));

            using (var stream = new MemoryStream())
            {
                encoder.Save(stream);
                return stream.ToArray();
            }
        }
    }
}
