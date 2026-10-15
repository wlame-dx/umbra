using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Web.Script.Serialization;

namespace Umbra
{
    internal sealed class MainWindow : Window
    {
        private readonly StreamingService _service = new StreamingService();
        private readonly InputInjector _injector = new InputInjector();
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();

        private WebServer _server;
        private int _serverPort = -1;
        private bool _running;
        private bool _suppressSelection;
        private bool _vdCreated;
        private bool _vdWanted;
        private bool _autoConfirm;
        private DiscoveryResponder _discovery;
        private readonly object _approvalGate = new object();
        private readonly HashSet<string> _approvalRequested = new HashSet<string>();

        private FrameworkElement _root;
        private Border _rootBorder;
        private Effect _shadowEffect;
        private Ellipse _statusDot;
        private TextBlock _statusText;
        private TextBox _urlBox;
        private Image _preview;
        private TextBlock _previewHint;
        private ComboBox _monitorCombo;
        private ComboBox _modeCombo;
        private ComboBox _resCombo;
        private ComboBox _langCombo;
        private Slider _fps;
        private Slider _quality;
        private Slider _scale;
        private TextBlock _fpsValue;
        private TextBlock _qualityValue;
        private TextBlock _scaleValue;
        private ToggleButton _inputSwitch;
        private ToggleButton _vdSwitch;
        private ToggleButton _approvalSwitch;
        private Button _btnGetDriver;
        private TextBox _portBox;
        private Button _startButton;
        private Button _btnMax;
        private ListBox _log;

        public MainWindow()
        {
            Title = "Umbra";
            Width = 960;
            Height = 668;
            MinWidth = 880;
            MinHeight = 600;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = false;                 // boyutlandirma icin opak pencere sart
            ResizeMode = ResizeMode.CanResize;
            Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0B, 0x0E));
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var chrome = new WindowChrome();
            chrome.CaptionHeight = 46;
            chrome.ResizeBorderThickness = new Thickness(6);
            chrome.GlassFrameThickness = new Thickness(0);
            chrome.CornerRadius = new CornerRadius(0);
            chrome.UseAeroCaptionButtons = false;
            WindowChrome.SetWindowChrome(this, chrome);

            LoadIcon();

            string xaml = ResourceStore.LoadText("ui.MainWindow.xaml", System.IO.Path.Combine("ui", "MainWindow.xaml"));
            if (string.IsNullOrEmpty(xaml)) throw new InvalidOperationException("Arayuz tanimi bulunamadi.");

            _root = (FrameworkElement)XamlReader.Parse(xaml);
            _rootBorder = _root as Border;
            if (_rootBorder != null) _shadowEffect = _rootBorder.Effect;
            Content = _root;

            Build();
        }

        private void LoadIcon()
        {
            try
            {
                byte[] bytes = ResourceStore.LoadBytes("assets.umbra.png", System.IO.Path.Combine("assets", "umbra.png"));
                if (bytes == null) return;
                using (var stream = new MemoryStream(bytes))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    Icon = image;
                }
            }
            catch { }
        }

        private T Find<T>(string name) where T : class
        {
            return _root.FindName(name) as T;
        }

        private void Build()
        {
            _statusDot = Find<Ellipse>("StatusDot");
            _statusText = Find<TextBlock>("StatusText");
            _urlBox = Find<TextBox>("UrlBox");
            _preview = Find<Image>("PreviewImage");
            _previewHint = Find<TextBlock>("PreviewHint");
            _monitorCombo = Find<ComboBox>("MonitorCombo");
            _modeCombo = Find<ComboBox>("ModeCombo");
            _resCombo = Find<ComboBox>("ResCombo");
            _langCombo = Find<ComboBox>("LangCombo");
            _fps = Find<Slider>("FpsSlider");
            _quality = Find<Slider>("QualitySlider");
            _scale = Find<Slider>("ScaleSlider");
            _fpsValue = Find<TextBlock>("FpsValue");
            _qualityValue = Find<TextBlock>("QualityValue");
            _scaleValue = Find<TextBlock>("ScaleValue");
            _inputSwitch = Find<ToggleButton>("InputSwitch");
            _vdSwitch = Find<ToggleButton>("VdSwitch");
            _approvalSwitch = Find<ToggleButton>("ApprovalSwitch");
            _btnGetDriver = Find<Button>("BtnGetDriver");
            _portBox = Find<TextBox>("PortBox");
            _startButton = Find<Button>("BtnStart");
            _btnMax = Find<Button>("BtnMax");
            _log = Find<ListBox>("LogList");

            _fpsValue.Text = ((int)Math.Round(_fps.Value)).ToString(CultureInfo.InvariantCulture);
            _qualityValue.Text = ((int)Math.Round(_quality.Value)).ToString(CultureInfo.InvariantCulture);
            _scaleValue.Text = _scale.Value.ToString("0.00", CultureInfo.InvariantCulture);
            _portBox.Text = _service.Settings.Port.ToString(CultureInfo.InvariantCulture);

            _startButton.Click += delegate { ToggleStreaming(); };
            Find<Button>("BtnClose").Click += delegate { Close(); };
            Find<Button>("BtnMin").Click += delegate { WindowState = WindowState.Minimized; };
            if (_btnMax != null) _btnMax.Click += delegate { ToggleMaximize(); };
            Find<Button>("BtnCopy").Click += delegate { CopyUrl(); };
            Find<Button>("BtnOpen").Click += delegate { OpenUrl(); };
            Find<Button>("BtnRefresh").Click += delegate { RefreshMonitors(true); };
            Find<Button>("BtnApplyRes").Click += delegate { ApplyResolution(); };

            _fps.ValueChanged += delegate { _fpsValue.Text = ((int)Math.Round(_fps.Value)).ToString(CultureInfo.InvariantCulture); PushSettings(); };
            _quality.ValueChanged += delegate { _qualityValue.Text = ((int)Math.Round(_quality.Value)).ToString(CultureInfo.InvariantCulture); PushSettings(); };
            _scale.ValueChanged += delegate { _scaleValue.Text = _scale.Value.ToString("0.00", CultureInfo.InvariantCulture); PushSettings(); };
            _inputSwitch.Click += delegate { PushSettings(); };
            if (_vdSwitch != null) _vdSwitch.Click += delegate { PushSettings(); };
            if (_approvalSwitch != null) _approvalSwitch.Click += delegate { PushSettings(); };
            if (_btnGetDriver != null) _btnGetDriver.Click += delegate { DownloadDriver(); };
            _modeCombo.SelectionChanged += delegate { PushSettings(); };
            _monitorCombo.SelectionChanged += OnMonitorChanged;
            _portBox.TextChanged += delegate { UpdateUrl(); };

            // Dil secimi
            _suppressSelection = true;
            _langCombo.Items.Add("Türkçe");
            _langCombo.Items.Add("English");
            _langCombo.SelectedIndex = 0;
            _suppressSelection = false;
            _langCombo.SelectionChanged += OnLanguageChanged;

            _service.Preview += OnPreview;
            _service.Hub.Added += OnSessionAdded;
            Log.Line += OnLogLine;

            ApplyLanguage();
            RefreshMonitors(false);
            UpdateStatus();
            Log.Info(I18n.T("log.ready"));

            // Otomatik baslatma: test ve otomatik kullanim icin gizli bir bayrak.
            // Bu kipte onay/uyari pencereleri ve sanal ekran olusturma atlanir.
            string[] args = Environment.GetCommandLineArgs();
            bool autoStart = Array.IndexOf(args, "--autostart") >= 0;
            bool selfTest = Array.IndexOf(args, "--selftest") >= 0;
            _autoConfirm = autoStart;

            if (selfTest) UmbDialog.TestAutoCloseMs = 700;

            if (autoStart || selfTest)
            {
                Loaded += delegate
                {
                    Dispatcher.BeginInvoke(new Action(delegate() { StartStreaming(); }));
                };
            }

            if (selfTest)
            {
                // Baslat -> durdur -> kapat akisini kendiliginden calistir.
                var stopTimer = new DispatcherTimer();
                stopTimer.Interval = TimeSpan.FromSeconds(3);
                stopTimer.Tick += delegate
                {
                    stopTimer.Stop();
                    Log.Info("selftest: durduruluyor");
                    StopStreaming();
                };

                var closeTimer = new DispatcherTimer();
                closeTimer.Interval = TimeSpan.FromSeconds(7);
                closeTimer.Tick += delegate
                {
                    closeTimer.Stop();
                    Log.Info("selftest: kapatiliyor");
                    try { Close(); } catch (Exception ex) { Log.Error("selftest close hatasi: " + ex.Message); }
                };

                Loaded += delegate { stopTimer.Start(); closeTimer.Start(); };
            }
        }

        // ------------------------------------------------------------------
        //  Pencere durumu (buyut / geri al)
        // ------------------------------------------------------------------

        private void ToggleMaximize()
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            bool maximized = WindowState == WindowState.Maximized;

            if (_rootBorder != null)
            {
                _rootBorder.Margin = maximized ? new Thickness(0) : new Thickness(2);
                _rootBorder.CornerRadius = new CornerRadius(maximized ? 0 : 14);
                _rootBorder.Effect = maximized ? null : _shadowEffect;
            }
            if (_btnMax != null) _btnMax.Content = maximized ? "\u2750" : "\u25A1";
        }

        // ------------------------------------------------------------------
        //  Buyutme: pencereyi gorev cubugunu kapatmadan ekrana sigdir.
        //  WindowStyle=None oldugu icin WPF kendi basina tum ekrani kaplar;
        //  WM_GETMINMAXINFO ile calisma alanina sinirliyoruz.
        // ------------------------------------------------------------------

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var handle = new WindowInteropHelper(this).Handle;
                var source = HwndSource.FromHwnd(handle);
                if (source != null) source.AddHook(WindowProc);
            }
            catch { }
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0024) // WM_GETMINMAXINFO
            {
                try { ClampToWorkArea(hwnd, lParam); } catch { }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void ClampToWorkArea(IntPtr hwnd, IntPtr lParam)
        {
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero) return;

            var info = new MONITORINFO();
            info.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
            if (!GetMonitorInfo(monitor, ref info)) return;

            var mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            mmi.ptMaxPosition.X = info.rcWork.Left - info.rcMonitor.Left;
            mmi.ptMaxPosition.Y = info.rcWork.Top - info.rcMonitor.Top;
            mmi.ptMaxSize.X = info.rcWork.Right - info.rcWork.Left;
            mmi.ptMaxSize.Y = info.rcWork.Bottom - info.rcWork.Top;

            // Arayuzun sikismamasi icin asgari boyutu (DIP -> fiziksel piksel).
            double scale = 1.0;
            try
            {
                uint dpi = GetDpiForWindow(hwnd);
                if (dpi > 0) scale = dpi / 96.0;
            }
            catch { }

            int minW = (int)Math.Ceiling(MinWidth * scale);
            int minH = (int)Math.Ceiling(MinHeight * scale);
            if (minW > 0) mmi.ptMinTrackSize.X = minW;
            if (minH > 0) mmi.ptMinTrackSize.Y = minH;

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        private const int MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct WinPoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WinRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public WinPoint ptReserved;
            public WinPoint ptMaxSize;
            public WinPoint ptMaxPosition;
            public WinPoint ptMinTrackSize;
            public WinPoint ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public WinRect rcMonitor;
            public WinRect rcWork;
            public int dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        // ------------------------------------------------------------------
        //  Dil
        // ------------------------------------------------------------------

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection) return;
            I18n.Language = _langCombo.SelectedIndex == 1 ? I18n.En : I18n.Tr;
            ApplyLanguage();
            RefreshMonitors(false);
            Log.Info(I18n.T("log.lang"));
        }

        private void ApplyLanguage()
        {
            SetText("LblTagline", "app.tagline");
            SetText("LblUrlLabel", "url.label");
            SetContent("BtnCopy", "url.copy");
            SetContent("BtnOpen", "url.open");

            SetText("LblScreenTitle", "screen.title");
            SetContent("BtnRefresh", "screen.refresh");
            SetText("LblMonitorLabel", "screen.source");
            SetText("LblResLabel", "screen.res");
            SetContent("BtnApplyRes", "screen.apply");

            SetText("LblQualityTitle", "quality.title");
            SetText("LblFpsLabel", "quality.fps");
            SetText("LblQLabel", "quality.jpeg");
            SetText("LblScaleLabel", "quality.scale");

            SetText("LblControlTitle", "control.title");
            SetText("LblInputLabel", "control.input");
            SetText("LblVdLabel", "control.vd");
            SetText("LblApprovalLabel", "control.approval");
            SetContent("BtnGetDriver", "control.getdriver");
            SetText("LblDriverHint", "control.driver_hint");
            SetText("LblModeLabel", "control.mode");
            SetText("LblPortLabel", "control.port");
            SetText("LblTip", "control.tip");

            if (_modeCombo.Items.Count >= 2)
            {
                var touch = _modeCombo.Items[0] as ComboBoxItem;
                var mouse = _modeCombo.Items[1] as ComboBoxItem;
                if (touch != null) touch.Content = I18n.T("control.mode.touch");
                if (mouse != null) mouse.Content = I18n.T("control.mode.mouse");
            }

            SetText("LblPreviewTitle", "preview.title");
            if (_previewHint != null) _previewHint.Text = I18n.T("preview.hint");
            SetText("LblLogTitle", "log.title");

            UpdateStatus();
        }

        private void SetText(string name, string key)
        {
            var block = Find<TextBlock>(name);
            if (block != null) block.Text = I18n.T(key);
        }

        private void SetContent(string name, string key)
        {
            var button = Find<Button>(name);
            if (button != null) button.Content = I18n.T(key);
        }

        // ------------------------------------------------------------------
        //  Ekran listesi
        // ------------------------------------------------------------------

        private void RefreshMonitors(bool announce)
        {
            List<MonitorInfo> monitors = MonitorManager.Enumerate();
            var previous = _monitorCombo.SelectedItem as MonitorInfo;

            _suppressSelection = true;
            _monitorCombo.Items.Clear();
            foreach (var monitor in monitors) _monitorCombo.Items.Add(monitor);

            int index = 0;
            if (previous != null)
            {
                for (int i = 0; i < monitors.Count; i++)
                {
                    if (monitors[i].Device == previous.Device) { index = i; break; }
                }
            }
            if (monitors.Count > 0) _monitorCombo.SelectedIndex = index;
            _suppressSelection = false;

            RefreshModes();
            PushSettings();

            if (announce) Log.Info(I18n.F("log.monitors", monitors.Count));
        }

        private void OnMonitorChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection) return;
            RefreshModes();
            PushSettings();
        }

        private void RefreshModes()
        {
            var monitor = _monitorCombo.SelectedItem as MonitorInfo;
            _resCombo.Items.Clear();
            if (monitor == null) return;

            foreach (var mode in MonitorManager.GetModes(monitor.Device)) _resCombo.Items.Add(mode);

            string current = monitor.Width + " x " + monitor.Height;
            for (int i = 0; i < _resCombo.Items.Count; i++)
            {
                if (_resCombo.Items[i].ToString().StartsWith(current, StringComparison.Ordinal))
                {
                    _resCombo.SelectedIndex = i;
                    break;
                }
            }
            if (_resCombo.SelectedIndex < 0 && _resCombo.Items.Count > 0) _resCombo.SelectedIndex = 0;
        }

        private void ApplyResolution()
        {
            var monitor = _monitorCombo.SelectedItem as MonitorInfo;
            if (monitor == null || _resCombo.SelectedItem == null) return;

            string text = _resCombo.SelectedItem.ToString();
            string[] pieces = text.Split('x');
            if (pieces.Length < 2) return;

            int width;
            if (!int.TryParse(pieces[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out width)) return;

            string rest = pieces[1].Trim();
            int space = rest.IndexOf(' ');
            string heightText = space > 0 ? rest.Substring(0, space) : rest;

            int height;
            if (!int.TryParse(heightText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out height)) return;

            if (monitor.Width == width && monitor.Height == height)
            {
                Log.Info(I18n.F("log.res_already", width, height));
                return;
            }

            bool ok = MonitorManager.SetMode(monitor.Device, width, height);
            Log.Info(ok ? I18n.F("log.res_set", width, height) : I18n.T("log.res_fail"));
            if (ok) RefreshMonitors(false);
        }

        // ------------------------------------------------------------------
        //  Yayin
        // ------------------------------------------------------------------

        private void PushSettings()
        {
            var settings = _service.Settings;
            var monitor = _monitorCombo.SelectedItem as MonitorInfo;
            if (monitor != null) settings.MonitorDevice = monitor.Device;

            settings.Fps = (int)Math.Round(_fps.Value);
            settings.Quality = (int)Math.Round(_quality.Value);
            settings.Scale = _scale.Value;
            settings.AllowInput = _inputSwitch.IsChecked == true;
            settings.InputMode = _modeCombo.SelectedIndex == 1 ? "mouse" : "touch";
            settings.AutoVd = _vdSwitch == null || _vdSwitch.IsChecked == true;
            settings.RequireApproval = _approvalSwitch == null || _approvalSwitch.IsChecked == true;
        }

        private void ToggleStreaming()
        {
            if (_running) StopStreaming();
            else StartStreaming();
        }

        private void StartStreaming()
        {
            try
            {
                PushSettings();
                var settings = _service.Settings;

                if (_monitorCombo.SelectedItem == null)
                {
                    Log.Error(I18n.T("log.no_source"));
                    return;
                }

                int port;
                if (!int.TryParse(_portBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out port) ||
                    port < 1 || port > 65535)
                {
                    port = 8777;
                    _portBox.Text = "8777";
                    Log.Warn(I18n.T("log.bad_port"));
                }
                settings.Port = port;

                // Yayindan once olasi sorunlari uyari olarak goster.
                if (!ConfirmStart(port)) return;

                try
                {
                    if (_server == null || _serverPort != port)
                    {
                        if (_server != null) { _server.Stop(); _server = null; }
                        _server = new WebServer(_service.Hub, OnClientMessage);
                        _server.Start(port);
                        _serverPort = port;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(I18n.F("log.server_fail", port, ex.Message));
                    return;
                }

                _service.Start();
                _running = true;
                UpdateStatus();

                // Yerel agda gorunurluk: yayin acikken istemciler bizi bulabilir.
                StartDiscovery();

                Log.Info(I18n.F("log.start", NetUtil.GetLanIPv4(), port));

                // Sanal ekrani arka planda olustur; arayuz donmasin.
                _vdCreated = false;
                if (!_autoConfirm && settings.AutoVd) StartVirtualDisplay();
            }
            catch (Exception ex)
            {
                Log.Error(I18n.F("log.start_fail", ex.Message));
                _running = false;
                UpdateStatus();
            }
        }

        // ------------------------------------------------------------------
        //  Sanal ekran (arka planda)
        // ------------------------------------------------------------------

        /// <summary>
        /// vdriver\get-driver.ps1 betigini gorunur bir PowerShell penceresinde
        /// calistirir. Betik acik kaynak surucuyu indirip vdriver\bin'e koyar.
        /// </summary>
        private void DownloadDriver()
        {
            if (VdController.IsDriverReady())
            {
                UmbDialog.Warn(this, I18n.T("dlg.driver_title"), I18n.T("dlg.driver_ready"));
                return;
            }

            string script = VdController.ScriptPath("get-driver.ps1");
            if (script == null)
            {
                UmbDialog.Warn(this, I18n.T("dlg.driver_title"), I18n.T("dlg.driver_noscript"));
                return;
            }

            if (!UmbDialog.Confirm(this, I18n.T("dlg.driver_title"), I18n.T("dlg.driver_msg")))
                return;

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = "powershell.exe";
                psi.Arguments = "-NoProfile -ExecutionPolicy Bypass -NoExit -File \"" + script + "\"";
                psi.UseShellExecute = true;
                psi.WorkingDirectory = VdController.ScriptDirPath;
                System.Diagnostics.Process.Start(psi);
                Log.Info(I18n.T("log.driver_started"));
            }
            catch (Exception ex)
            {
                Log.Error(I18n.F("log.driver_fail", ex.Message));
            }
        }

        private void StartVirtualDisplay()
        {
            if (!VdController.IsAvailable() || !VdController.IsDriverReady())
            {
                // Surucu kurulu degilse bosuna UAC acmayalim.
                Log.Warn(I18n.T("log.vd_nodriver"));
                return;
            }

            _vdWanted = true;
            Log.Info(I18n.T("log.vd_creating"));

            VdController.CreateAsync(delegate(bool ok)
            {
                if (ok) _vdCreated = true;
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    if (!_vdWanted)
                    {
                        // Yayin bu sirada durduruldu; hemen geri kaldir.
                        if (_vdCreated) { _vdCreated = false; VdController.RemoveAsync(null); }
                        return;
                    }
                    if (ok)
                    {
                        Log.Info(I18n.T("log.vd_created"));
                        RefreshMonitors(false);
                    }
                    else
                    {
                        Log.Error(I18n.T("log.vd_failed"));
                    }
                }));
            });
        }

        private void StopVirtualDisplay()
        {
            _vdWanted = false;
            if (!_vdCreated) return;

            _vdCreated = false;
            Log.Info(I18n.T("log.vd_removing"));

            VdController.RemoveAsync(delegate(bool ok)
            {
                Dispatcher.BeginInvoke(new Action(delegate()
                {
                    if (ok)
                    {
                        Log.Info(I18n.T("log.vd_removed"));
                        RefreshMonitors(false);
                    }
                    else
                    {
                        Log.Warn(I18n.T("log.vd_remove_failed"));
                    }
                }));
            });
        }

        private bool ConfirmStart(int port)
        {
            if (_autoConfirm) return true;

            var warnings = new List<string>();
            var settings = _service.Settings;

            if (!settings.AllowInput)
                warnings.Add(I18n.T("warn.touch_off"));
            else if (settings.InputMode == "touch" && !_injector.CanTouch)
                warnings.Add(I18n.T("warn.touch_fail"));

            if (settings.AutoVd && !VdController.IsDriverReady())
                warnings.Add(I18n.T("warn.vd_missing"));

            if (IsPortBusy(port))
                warnings.Add(I18n.F("warn.port_busy", port));

            if (settings.Scale < 0.999)
                warnings.Add(I18n.T("warn.scale"));

            if (warnings.Count == 0) return true;
            return UmbDialog.ConfirmMany(this, I18n.T("dlg.start_title"), I18n.T("dlg.start_cont"), warnings);
        }

        private static bool IsPortBusy(int port)
        {
            try
            {
                var listener = new TcpListener(System.Net.IPAddress.Any, port);
                listener.Start();
                listener.Stop();
                return false;
            }
            catch { return true; }
        }

        private void StopStreaming()
        {
            try
            {
                if (!_autoConfirm && !UmbDialog.Confirm(this, I18n.T("dlg.stop_title"), I18n.T("dlg.stop_msg")))
                    return;

                _running = false;
                UpdateStatus();

                try { _service.Stop(); }
                catch (Exception ex) { Log.Warn("Yayin dongusu durdurulamadi: " + ex.Message); }

                try { if (_server != null) { _server.Stop(); _server = null; } }
                catch (Exception ex) { Log.Warn("Sunucu durdurulamadi: " + ex.Message); }
                _serverPort = -1;

                StopDiscovery();

                try { _injector.ReleaseAll(); } catch { }

                // Sanal ekrani arka planda kaldir; arayuz donmasin.
                StopVirtualDisplay();

                Log.Info(I18n.T("log.stop"));
            }
            catch (Exception ex)
            {
                Log.Error(I18n.F("log.stop_fail", ex.Message));
            }
        }

        private void UpdateStatus()
        {
            if (_running)
            {
                _statusDot.Fill = new SolidColorBrush(Color.FromRgb(0x6E, 0xE7, 0xA8));
                _statusText.Text = I18n.T("status.live");
                _startButton.Content = I18n.T("btn.stop");
                _previewHint.Visibility = Visibility.Collapsed;
            }
            else
            {
                _statusDot.Fill = new SolidColorBrush(Color.FromRgb(0x3A, 0x3F, 0x49));
                _statusText.Text = I18n.T("status.idle");
                _startButton.Content = I18n.T("btn.start");
                _previewHint.Visibility = Visibility.Visible;
            }
            UpdateUrl();
        }

        private void UpdateUrl()
        {
            int port = _serverPort > 0 ? _serverPort : ParsePort();
            _urlBox.Text = "http://" + NetUtil.GetLanIPv4() + ":" + port + "/";
        }

        private int ParsePort()
        {
            int port;
            if (int.TryParse(_portBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out port) &&
                port > 0 && port <= 65535)
                return port;
            return 8777;
        }

        // ------------------------------------------------------------------
        //  Yerel ag kesfi
        // ------------------------------------------------------------------

        private void StartDiscovery()
        {
            if (_discovery != null) return;
            _discovery = new DiscoveryResponder(DiscoveryPayload);
            _discovery.Start();
        }

        private void StopDiscovery()
        {
            if (_discovery == null) return;
            try { _discovery.Stop(); } catch { }
            _discovery = null;
        }

        /// <summary>Kesif paketine verilecek yanit; yayin kapaliysa null.</summary>
        private string DiscoveryPayload()
        {
            if (!_running) return null;
            int port = _serverPort > 0 ? _serverPort : ParsePort();
            var map = new Dictionary<string, object>();
            map["umbra"] = "host";
            map["v"] = 1;
            map["name"] = Environment.MachineName;
            map["port"] = port;
            map["os"] = "Windows";
            map["streaming"] = true;
            return _json.Serialize(map);
        }

        // ------------------------------------------------------------------
        //  Baglanti onayi
        // ------------------------------------------------------------------

        private void OnSessionAdded(ClientSession session)
        {
            if (!_service.Settings.RequireApproval)
            {
                ApproveSession(session, false);
                return;
            }

            // Istemci tanitimini hemen gonderir; gonderemezse kisa sure sonra
            // eldeki bilgiyle onay istenir.
            var waiter = new Thread(delegate()
            {
                try { Thread.Sleep(2500); } catch { }
                RequestApproval(session);
            });
            waiter.IsBackground = true;
            waiter.Name = "umbra-approve-wait";
            waiter.Start();
        }

        private void RequestApproval(ClientSession session)
        {
            if (!session.Alive || session.Approved) return;

            lock (_approvalGate)
            {
                if (!_approvalRequested.Add(session.Id)) return;
            }

            var app = Application.Current;
            if (app == null) { DenySession(session); return; }

            try
            {
                app.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    if (!session.Alive || session.Approved) return;
                    if (ShowApprovalDialog(session)) ApproveSession(session, true);
                    else DenySession(session);
                }));
            }
            catch
            {
                DenySession(session);
            }
        }

        private bool ShowApprovalDialog(ClientSession session)
        {
            if (_autoConfirm) return true;

            var lines = new List<string>();
            if (session.DeviceName.Length > 0) lines.Add(I18n.F("dlg.conn_name", session.DeviceName));
            if (session.DeviceModel.Length > 0 && !string.Equals(session.DeviceModel, session.DeviceName, StringComparison.Ordinal))
                lines.Add(I18n.F("dlg.conn_model", session.DeviceModel));
            if (session.DevicePlatform.Length > 0) lines.Add(I18n.F("dlg.conn_platform", session.DevicePlatform));
            if (session.DeviceOs.Length > 0 && !string.Equals(session.DeviceOs, session.DevicePlatform, StringComparison.Ordinal))
                lines.Add(I18n.F("dlg.conn_os", session.DeviceOs));
            if (lines.Count == 0) lines.Add(I18n.T("dlg.conn_unknown"));
            lines.Add(I18n.F("dlg.conn_ip", session.RemoteIP.Length > 0 ? session.RemoteIP : "?"));

            return UmbDialog.ConfirmManyTimed(this, I18n.T("dlg.conn_title"),
                I18n.T("dlg.conn_allow"), lines, 30000);
        }

        private void ApproveSession(ClientSession session, bool showLog)
        {
            if (!session.Alive) return;
            session.Approved = true;
            try { session.SendText(_service.BuildHello()); } catch { }
            if (showLog) Log.Info(I18n.F("log.conn_allowed", session.Describe(), session.RemoteIP));
        }

        private void DenySession(ClientSession session)
        {
            try { session.SendText("{\"t\":\"denied\"}"); } catch { }
            Log.Info(I18n.F("log.conn_denied", session.Describe(), session.RemoteIP));
            try { _service.Hub.Remove(session); } catch { }
            try { session.Close(); } catch { }
        }

        // ------------------------------------------------------------------
        //  Tarayici olaylari
        // ------------------------------------------------------------------

        private void OnClientMessage(ClientSession session, string text)
        {
            try
            {
                var settings = _service.Settings;
                var message = _json.Deserialize<Dictionary<string, object>>(text);
                if (message == null) return;

                string kind = GetString(message, "t");

                if (kind == "p")
                {
                    if (!settings.AllowInput) return;
                    var monitor = _service.CurrentMonitor;
                    if (monitor == null) return;

                    string action = GetString(message, "a");
                    double x = GetDouble(message, "x");
                    double y = GetDouble(message, "y");
                    int button = (int)GetDouble(message, "b");
                    int pointerId = (int)GetDouble(message, "id");

                    if (settings.InputMode == "touch" && _injector.CanTouch)
                    {
                        // Dokunma basarisiz olursa fareye dus.
                        bool ok = _injector.TouchPointer(monitor, pointerId, x, y, action);
                        if (!ok) ApplyMousePointer(monitor, action, button, x, y);
                    }
                    else
                    {
                        ApplyMousePointer(monitor, action, button, x, y);
                    }
                }
                else if (kind == "wheel")
                {
                    if (!settings.AllowInput) return;
                    var monitor = _service.CurrentMonitor;
                    if (monitor == null) return;
                    _injector.Wheel((int)GetDouble(message, "d"),
                        GetDouble(message, "x"), GetDouble(message, "y"), monitor);
                }
                else if (kind == "key")
                {
                    if (!settings.AllowInput) return;
                    bool down = GetString(message, "a") == "down";
                    _injector.Key(down, GetString(message, "code"), GetString(message, "key"));
                }
                else if (kind == "mode")
                {
                    string mode = GetString(message, "m");
                    if (mode == "mouse" || mode == "touch")
                    {
                        settings.InputMode = mode;
                        SyncUiFromSettings();
                    }
                }
                else if (kind == "identify")
                {
                    session.DeviceName = GetString(message, "name");
                    session.DeviceModel = GetString(message, "model");
                    session.DevicePlatform = GetString(message, "platform");
                    session.DeviceOs = GetString(message, "os");
                    if (_service.Settings.RequireApproval) RequestApproval(session);
                }
                else if (kind == "ping")
                {
                    long stamp = (long)GetDouble(message, "ts");
                    session.SendText("{\"t\":\"pong\",\"ts\":" + stamp + "}");
                }
                else if (kind == "set")
                {
                    if (message.ContainsKey("quality"))
                        settings.Quality = ClampInt((int)GetDouble(message, "quality"), 20, 95);
                    if (message.ContainsKey("fps"))
                        settings.Fps = ClampInt((int)GetDouble(message, "fps"), 5, 60);
                    if (message.ContainsKey("scale"))
                        settings.Scale = ClampDouble(GetDouble(message, "scale"), 0.3, 1.0);
                    SyncUiFromSettings();
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Girdi cozulemedi: " + ex.Message);
            }
        }

        private void ApplyMousePointer(MonitorInfo monitor, string action, int button, double x, double y)
        {
            if (action == "down") _injector.Button(true, button, x, y, monitor);
            else if (action == "up") _injector.Button(false, button, x, y, monitor);
            else _injector.Move(x, y, monitor);
        }

        private void OnPreview(BitmapSource bitmap)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                if (_preview != null) _preview.Source = bitmap;
            }));
        }

        private void OnLogLine(string line)
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                if (_log == null) return;
                _log.Items.Add(line);
                while (_log.Items.Count > 400) _log.Items.RemoveAt(0);
                if (_log.Items.Count > 0) _log.ScrollIntoView(_log.Items[_log.Items.Count - 1]);
            }));
        }

        private void CopyUrl()
        {
            try
            {
                Clipboard.SetText(_urlBox.Text);
                Log.Info(I18n.T("log.copied"));
            }
            catch
            {
                Log.Warn(I18n.T("log.copy_fail"));
            }
        }

        private void OpenUrl()
        {
            try
            {
                System.Diagnostics.Process.Start(_urlBox.Text);
            }
            catch (Exception ex)
            {
                Log.Warn("Tarayici acilamadi: " + ex.Message);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _running = false;
            _vdWanted = false;
            try { _injector.ReleaseAll(); } catch { }
            try { _service.Stop(); } catch { }
            try { if (_server != null) _server.Stop(); } catch { }
            StopDiscovery();

            // Kapanirken sanal ekrani da temizle (beklemeden).
            if (_vdCreated)
            {
                _vdCreated = false;
                try { VdController.RemoveAsync(null); } catch { }
            }

            base.OnClosed(e);
        }

        // ------------------------------------------------------------------
        //  JSON yardimcilari
        // ------------------------------------------------------------------

        private static string GetString(Dictionary<string, object> map, string key)
        {
            object value;
            if (map.TryGetValue(key, out value) && value != null) return value.ToString();
            return "";
        }

        private static double GetDouble(Dictionary<string, object> map, string key)
        {
            object value;
            if (map.TryGetValue(key, out value) && value != null)
            {
                try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
                catch { return 0; }
            }
            return 0;
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static double ClampDouble(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private void SyncUiFromSettings()
        {
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                var settings = _service.Settings;

                _suppressSelection = true;
                _quality.Value = settings.Quality;
                _fps.Value = settings.Fps;
                _scale.Value = settings.Scale;
                int modeIndex = settings.InputMode == "mouse" ? 1 : 0;
                if (_modeCombo.SelectedIndex != modeIndex) _modeCombo.SelectedIndex = modeIndex;
                _suppressSelection = false;
            }));
        }
    }
}