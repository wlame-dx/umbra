using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media.Imaging;

namespace Umbra
{
    /// <summary>Yakalama dongusu: ekrani yakalar, kodlar ve bagli istemcilere yollar.</summary>
    internal sealed class StreamingService
    {
        public readonly StreamHub Hub = new StreamHub();
        public HostSettings Settings = new HostSettings();

        public event Action<BitmapSource> Preview;

        private volatile bool _running;
        private Thread _thread;
        private ScreenCapture _capture;
        private string _captureDevice;
        private int _captureWidth;
        private int _captureHeight;
        private MonitorInfo _currentMonitor;
        private int _frameWidth;
        private int _frameHeight;
        private long _lastModeCheck;

        public StreamingService()
        {
        }

        public bool IsRunning { get { return _running; } }
        public MonitorInfo CurrentMonitor { get { return _currentMonitor; } }
        public int FrameWidth { get { return _frameWidth; } }
        public int FrameHeight { get { return _frameHeight; } }

        /// <summary>Onaylanan istemciye gonderilecek tanitim mesaji.</summary>
        public string BuildHello()
        {
            return "{\"t\":\"hello\",\"app\":\"Umbra\",\"w\":" + _frameWidth + ",\"h\":" + _frameHeight + "}";
        }

        public void Start()
        {
            if (_running) return;
            _running = true;

            _thread = new Thread(Loop);
            _thread.IsBackground = true;
            _thread.Name = "umbra-capture";
            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            var thread = _thread;
            if (thread != null)
            {
                try { thread.Join(2000); } catch { }
                _thread = null;
            }
            DisposeCapture();
        }

        private void DisposeCapture()
        {
            if (_capture != null)
            {
                try { _capture.Dispose(); } catch { }
                _capture = null;
            }
            _captureDevice = null;
        }

        private void Loop()
        {
            var stopwatch = new Stopwatch();
            long lastPreview = 0;
            int lastW = -1;
            int lastH = -1;

            while (_running)
            {
                stopwatch.Restart();
                var settings = Settings;

                EnsureCapture(settings);
                if (_capture == null)
                {
                    Thread.Sleep(200);
                    continue;
                }

                BitmapSource bitmap;
                try { bitmap = _capture.Capture(); }
                catch { bitmap = null; }

                if (bitmap != null)
                {
                    _frameWidth = _capture.Width;
                    _frameHeight = _capture.Height;

                    if (_frameWidth != lastW || _frameHeight != lastH)
                    {
                        lastW = _frameWidth;
                        lastH = _frameHeight;
                        Hub.BroadcastText(BuildHello());
                    }

                    long now = Environment.TickCount;
                    if (now - lastPreview >= 200)
                    {
                        lastPreview = now;
                        var preview = Preview;
                        if (preview != null)
                        {
                            try { preview(bitmap); } catch { }
                        }
                    }

                    var scaled = FrameEncoder.Scale(bitmap, settings.Scale);

                    byte[] jpeg;
                    try { jpeg = FrameEncoder.ToJpeg(scaled, settings.Quality); }
                    catch { jpeg = null; }

                    if (jpeg != null) Hub.Broadcast(jpeg);
                }

                int target = 1000 / Math.Max(1, settings.Fps);
                int elapsed = (int)stopwatch.ElapsedMilliseconds;
                if (elapsed < target) Thread.Sleep(target - elapsed);
            }
        }

        private void EnsureCapture(HostSettings settings)
        {
            string device = settings.MonitorDevice;
            if (string.IsNullOrEmpty(device))
            {
                var primary = MonitorManager.Primary();
                if (primary == null) { DisposeCapture(); return; }
                device = primary.Device;
            }

            long now = Environment.TickCount;
            bool shouldRecheck = _capture == null || now - _lastModeCheck > 1500;
            if (!shouldRecheck && string.Equals(device, _captureDevice, StringComparison.OrdinalIgnoreCase))
                return;
            _lastModeCheck = now;

            var monitor = MonitorManager.Find(device);
            if (monitor == null) monitor = MonitorManager.Primary();
            if (monitor == null) { DisposeCapture(); return; }
            _currentMonitor = monitor;

            if (_capture != null &&
                string.Equals(_captureDevice, monitor.Device, StringComparison.OrdinalIgnoreCase) &&
                _captureWidth == monitor.Width &&
                _captureHeight == monitor.Height)
                return;

            DisposeCapture();
            try
            {
                _capture = new ScreenCapture(monitor.Device, monitor.Width, monitor.Height, monitor.X, monitor.Y);
                _captureDevice = monitor.Device;
                _captureWidth = monitor.Width;
                _captureHeight = monitor.Height;
                _frameWidth = monitor.Width;
                _frameHeight = monitor.Height;
                Log.Info(I18n.F("log.capture_start", monitor.Name, monitor.Width, monitor.Height));
            }
            catch (Exception ex)
            {
                Log.Error("Yakalama baslatilamadi: " + ex.Message);
                _capture = null;
            }
        }
    }
}
