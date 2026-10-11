using System;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Umbra
{
    /// <summary>
    /// GDI ile tek bir ekranin goruntusunu yakalar. Sanal ekranlar dahil
    /// herhangi bir monitorde calisir, ek surucu gerektirmez.
    /// Fare imleci ayrica goruntuye islenir (istemci onu boylece gorur).
    /// </summary>
    internal sealed class ScreenCapture : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            public uint bmiColors;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateDC(string driver, string device, string port, IntPtr devmode);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO bmi, uint usage, out IntPtr bits, IntPtr section, uint offset);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, uint rop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr obj);

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, ref ICONINFO piconinfo);

        [DllImport("user32.dll")]
        private static extern bool DrawIconEx(IntPtr hdc, int x, int y, IntPtr hIcon, int width, int height, int stepIfAniCur, IntPtr flickerFreeDraw, uint diFlags);

        private const uint SRCCOPY = 0x00CC0020;
        private const uint CAPTUREBLT = 0x40000000;
        private const uint DI_NORMAL = 0x0003;
        private const int CURSOR_SHOWING = 0x00000001;

        private readonly string _device;
        private readonly int _width;
        private readonly int _height;
        private readonly int _originX;
        private readonly int _originY;

        private IntPtr _screenDc;
        private IntPtr _memoryDc;
        private IntPtr _dib;
        private IntPtr _previousObject;
        private IntPtr _bits;
        private bool _disposed;

        public int Width { get { return _width; } }
        public int Height { get { return _height; } }

        public ScreenCapture(string deviceName, int width, int height, int originX, int originY)
        {
            if (width <= 0 || height <= 0) throw new ArgumentException("Gecersiz ekran boyutu.");

            _device = deviceName;
            _width = width;
            _height = height;
            _originX = originX;
            _originY = originY;

            _screenDc = CreateDC("DISPLAY", deviceName, null, IntPtr.Zero);
            if (_screenDc == IntPtr.Zero)
                throw new InvalidOperationException("Ekran baglami acilamadi: " + deviceName);

            _memoryDc = CreateCompatibleDC(_screenDc);
            if (_memoryDc == IntPtr.Zero)
                throw new InvalidOperationException("Bellek baglami olusturulamadi.");

            var info = new BITMAPINFO();
            info.bmiHeader.biSize = (uint)Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            info.bmiHeader.biWidth = width;
            info.bmiHeader.biHeight = -height; // negatif = yukaridan asagiya
            info.bmiHeader.biPlanes = 1;
            info.bmiHeader.biBitCount = 32;
            info.bmiHeader.biCompression = 0;  // BI_RGB

            IntPtr bits;
            _dib = CreateDIBSection(_screenDc, ref info, 0, out bits, IntPtr.Zero, 0);
            if (_dib == IntPtr.Zero)
                throw new InvalidOperationException("Goruntu tamponu olusturulamadi.");

            _bits = bits;
            _previousObject = SelectObject(_memoryDc, _dib);
        }

        public BitmapSource Capture()
        {
            if (_disposed || _screenDc == IntPtr.Zero || _memoryDc == IntPtr.Zero) return null;

            if (!BitBlt(_memoryDc, 0, 0, _width, _height, _screenDc, 0, 0, SRCCOPY | CAPTUREBLT))
                return null;

            DrawCursor();

            var bitmap = BitmapSource.Create(
                _width, _height, 96, 96, PixelFormats.Bgra32, null,
                _bits, _width * _height * 4, _width * 4);
            bitmap.Freeze();
            return bitmap;
        }

        /// <summary>Gecerli fare imlecini yakalanan kareye isler (istemci imleci gorur).</summary>
        private void DrawCursor()
        {
            var cursor = new CURSORINFO();
            cursor.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
            if (!GetCursorInfo(ref cursor)) return;
            if ((cursor.flags & CURSOR_SHOWING) == 0) return;
            if (cursor.hCursor == IntPtr.Zero) return;

            int x = cursor.ptScreenPos.x - _originX;
            int y = cursor.ptScreenPos.y - _originY;

            // Imlecin sicak noktasini (hotspot) uygula.
            var info = new ICONINFO();
            if (GetIconInfo(cursor.hCursor, ref info))
            {
                x -= info.xHotspot;
                y -= info.yHotspot;
                if (info.hbmColor != IntPtr.Zero) DeleteObject(info.hbmColor);
                if (info.hbmMask != IntPtr.Zero) DeleteObject(info.hbmMask);
            }

            DrawIconEx(_memoryDc, x, y, cursor.hCursor, 0, 0, 0, IntPtr.Zero, DI_NORMAL);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_memoryDc != IntPtr.Zero && _previousObject != IntPtr.Zero)
                SelectObject(_memoryDc, _previousObject);

            if (_dib != IntPtr.Zero) { DeleteObject(_dib); _dib = IntPtr.Zero; }
            if (_memoryDc != IntPtr.Zero) { DeleteDC(_memoryDc); _memoryDc = IntPtr.Zero; }
            if (_screenDc != IntPtr.Zero) { DeleteDC(_screenDc); _screenDc = IntPtr.Zero; }
        }
    }
}