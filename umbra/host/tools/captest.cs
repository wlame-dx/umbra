using System;
using System.Threading;
using Umbra;

// Ekran yakalama + JPEG kodlama dogrulamasi.
internal static class CapTest
{
    [STAThread]
    private static void Main()
    {
        Log.Init();

        var monitors = MonitorManager.Enumerate();
        Console.WriteLine("monitor sayisi: " + monitors.Count);
        foreach (var m in monitors)
            Console.WriteLine("  " + m.Device + "  " + m.X + "," + m.Y + "  " + m.Width + "x" + m.Height + (m.Primary ? "  (ana)" : ""));

        var primary = MonitorManager.Primary();
        if (primary == null) { Console.WriteLine("monitor bulunamadi"); Environment.Exit(1); }

        var modes = MonitorManager.GetModes(primary.Device);
        Console.WriteLine("kip sayisi: " + modes.Count);
        for (int i = 0; i < modes.Count && i < 6; i++) Console.WriteLine("  " + modes[i]);

        using (var capture = new ScreenCapture(primary.Device, primary.Width, primary.Height, primary.X, primary.Y))
        {
            for (int i = 0; i < 3; i++)
            {
                var bmp = capture.Capture();
                if (bmp == null) { Console.WriteLine("  yakalama null dondurdu"); continue; }

                byte[] full = FrameEncoder.ToJpeg(bmp, 72);
                var half = FrameEncoder.Scale(bmp, 0.5);
                byte[] small = FrameEncoder.ToJpeg(half, 72);

                Console.WriteLine("  kare " + i + ": " + bmp.PixelWidth + "x" + bmp.PixelHeight +
                    "  tam=" + full.Length + "B" +
                    "  yarim=" + small.Length + "B (" + half.PixelWidth + "x" + half.PixelHeight + ")");
                Thread.Sleep(120);
            }
        }

        Console.WriteLine("tamam");
        Environment.Exit(0);
    }
}
