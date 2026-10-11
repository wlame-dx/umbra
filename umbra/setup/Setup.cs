using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace UmbraSetup
{
#if !UMBRA_VERSION
    /// <summary>package.ps1, /define:UMBRA_VERSION ile derlerken surum sinifini uretir.</summary>
    internal static class BuildInfo
    {
        public const string Version = "0.0.0";
    }
#endif

    internal static class I18n
    {
        public static string Lang = "en";

        private static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "title",        new[] { "Umbra Kurulum", "Umbra Setup" } },
            { "subtitle",     new[] { "ikinci ekran, kablosuz", "second screen, wireless" } },
            { "ready.title",  new[] { "Kuruluma hazır", "Ready to install" } },
            { "ready.text",   new[]
                {
                    "Umbra bu bilgisayara kurulacak ve Başlat menüsüne kısayol eklenecek. Yönetici hakki gerekmez, her şey yalnızca bu kullanıcı için.",
                    "Umbra will be installed for this user and a Start menu shortcut will be added. No administrator rights are needed."
                } },
            { "chk.desktop",  new[] { "Masaüstü kısayolu da oluştur", "Also create a desktop shortcut" } },
            { "btn.install",  new[] { "Kur", "Install" } },
            { "btn.cancel",   new[] { "Vazgeç", "Cancel" } },
            { "btn.close",    new[] { "Kapat", "Close" } },
            { "btn.launch",   new[] { "Umbra'yı başlat", "Launch Umbra" } },
            { "btn.uninstall",new[] { "Kaldır", "Uninstall" } },
            { "working",      new[] { "Kuruluyor…", "Installing…" } },
            { "working.off",  new[] { "Kaldırılıyor…", "Uninstalling…" } },
            { "done.title",   new[] { "Kurulum tamamlandı", "Installation complete" } },
            { "done.text",    new[] { "Umbra kuruldu. Artık Başlat menüsünden açabilirsin.", "Umbra is installed. You can open it from the Start menu." } },
            { "un.title",     new[] { "Umbra kaldırılacak", "Umbra will be removed" } },
            { "un.text",      new[] { "Program, kısayollar ve kayıt defteri girdileri silinecek. Devam edilsin mi?", "The program, shortcuts and registry entries will be removed. Continue?" } },
            { "un.done",      new[] { "Umbra kaldırıldı.", "Umbra has been removed." } },
            { "un.done.title", new[] { "Kaldırma tamamlandı", "Removal complete" } },
            { "err.exists",   new[] { "Umbra zaten kurulu görünüyor. Üzerine yazılacak.", "Umbra appears to be already installed. It will be overwritten." } },
            { "err.io",       new[] { "İşlem tamamlanamadı: ", "The operation could not be completed: " } },
            { "err.running",  new[] { "Açık olan Umbra kapatılıyor…", "Closing the running Umbra…" } },
            { "installing.todo", new[] { "Dosyalar kopyalanıyor", "Copying files" } },
            { "installing.sc",   new[] { "Kısayollar oluşturuluyor", "Creating shortcuts" } },
            { "installing.reg",  new[] { "Kayıt defteri yazılıyor", "Writing registry" } }
        };

        public static string T(string key)
        {
            string[] pair;
            if (!Map.TryGetValue(key, out pair)) return key;
            return Lang == "tr" ? pair[0] : pair[1];
        }
    }

    internal static class Installer
    {
        public static string InstallDir
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Path.Combine("Programs", "Umbra"));
            }
        }

        public static string ExePath { get { return Path.Combine(InstallDir, "Umbra.exe"); } }

        public static string StartMenuLink
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Umbra.lnk"); }
        }

        public static string DesktopLink
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Umbra.lnk"); }
        }

        private const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Umbra";

        public static bool IsInstalled { get { return File.Exists(ExePath); } }

        public static void KillRunning()
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("Umbra"))
                {
                    try { process.Kill(); process.WaitForExit(4000); } catch { }
                }
            }
            catch { }
        }

        public static void Install(Action<string> progress, bool desktopShortcut)
        {
            Directory.CreateDirectory(InstallDir);

            progress(I18n.T("installing.todo"));
            ExtractPayload(InstallDir);

            // Setup.exe'yi de kurulum klasorune kopyala: kaldirma bunu kullanir.
            progress(I18n.T("installing.sc"));
            CopySelf();

            CreateShortcut(StartMenuLink, ExePath, InstallDir);
            if (desktopShortcut) CreateShortcut(DesktopLink, ExePath, InstallDir);

            progress(I18n.T("installing.reg"));
            WriteRegistry(desktopShortcut);
        }

        public static void Uninstall()
        {
            KillRunning();

            try { if (File.Exists(StartMenuLink)) File.Delete(StartMenuLink); } catch { }
            try { if (File.Exists(DesktopLink)) File.Delete(DesktopLink); } catch { }
            try { Registry.CurrentUser.DeleteSubKeyTree(UninstallKeyPath, false); } catch { }

            // Kendi exe'mizi silemeyecegimiz icin klasoru gecikmeli sil.
            string dir = InstallDir;
            try
            {
                var info = new ProcessStartInfo();
                info.FileName = "cmd.exe";
                info.Arguments = "/c ping -n 2 127.0.0.1 >nul & rd /s /q \"" + dir + "\"";
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                Process.Start(info);
            }
            catch
            {
                try { Directory.Delete(dir, true); } catch { }
            }
        }

        public static void Launch()
        {
            try { Process.Start(ExePath); } catch { }
        }

        private static void ExtractPayload(string targetDir)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("UmbraSetup.Payload.zip"))
            {
                if (stream == null) throw new InvalidOperationException("Payload bulunamadi (UmbraSetup.Payload.zip).");
                using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue; // klasor girdisi
                        string dest = Path.Combine(targetDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                        string parent = Path.GetDirectoryName(dest);
                        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                        using (var input = entry.Open())
                        using (var output = File.Create(dest))
                            input.CopyTo(output);
                    }
                }
            }
        }

        private static void CopySelf()
        {
            try
            {
                string self = Assembly.GetExecutingAssembly().Location;
                string dest = Path.Combine(InstallDir, "Setup.exe");
                if (!string.Equals(self, dest, StringComparison.OrdinalIgnoreCase))
                    File.Copy(self, dest, true);
            }
            catch { }
        }

        private static void WriteRegistry(bool desktopShortcut)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(UninstallKeyPath))
            {
                if (key == null) return;
                key.SetValue("DisplayName", "Umbra");
                key.SetValue("DisplayVersion", BuildInfo.Version);
                key.SetValue("Publisher", "Umbra");
                key.SetValue("InstallLocation", InstallDir);
                key.SetValue("DisplayIcon", ExePath + ",0");
                key.SetValue("UninstallString", "\"" + Path.Combine(InstallDir, "Setup.exe") + "\" --uninstall");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }

        private static void CreateShortcut(string linkPath, string target, string workDir)
        {
            object shell = null;
            object shortcut = null;
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                shell = Activator.CreateInstance(shellType);
                shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { linkPath });

                Type scType = shortcut.GetType();
                scType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { target });
                scType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workDir });
                scType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { target + ",0" });
                scType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { "Umbra - ikinci ekran" });
                scType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
            }
            catch { }
            finally
            {
                if (shortcut != null) { try { Marshal.ReleaseComObject(shortcut); } catch { } }
                if (shell != null) { try { Marshal.ReleaseComObject(shell); } catch { } }
            }
        }
    }

    internal sealed class SetupWindow : Window
    {
        private readonly TextBlock _heading;
        private readonly TextBlock _message;
        private readonly ProgressBar _progress;
        private readonly StackPanel _buttonRow;
        private readonly CheckBox _desktopCheck;
        private readonly Button _langButton;
        private bool _busy;
        private bool _uninstallMode;

        public SetupWindow(bool uninstallMode)
        {
            _uninstallMode = uninstallMode;

            Title = "Umbra";
            Width = 470;
            SizeToContent = SizeToContent.Height;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = true;

            I18n.Lang = DetectLanguage();

            var shell = new Border();
            shell.Margin = new Thickness(14);
            shell.CornerRadius = new CornerRadius(16);
            shell.BorderThickness = new Thickness(1);
            shell.BorderBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x2B, 0x34));
            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(1, 1);
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x12, 0x15, 0x1D), 0));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(0x0A, 0x0B, 0x0E), 1));
            shell.Background = gradient;
            shell.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 28, ShadowDepth = 0, Opacity = 0.6, Color = Colors.Black
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // --- baslik ---
            var header = new Grid();
            header.Margin = new Thickness(20, 16, 16, 10);
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.MouseLeftButtonDown += delegate(object s, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed) { try { DragMove(); } catch { } }
            };

            var mark = new Border();
            mark.Width = 30;
            mark.Height = 30;
            mark.CornerRadius = new CornerRadius(9);
            var logo = new LinearGradientBrush();
            logo.StartPoint = new Point(0, 0);
            logo.EndPoint = new Point(1, 1);
            logo.GradientStops.Add(new GradientStop(Colors.White, 0));
            logo.GradientStops.Add(new GradientStop(Color.FromRgb(0x56, 0x5D, 0x69), 1));
            mark.Background = logo;
            var markText = new TextBlock();
            markText.Text = "U";
            markText.FontSize = 17;
            markText.FontWeight = FontWeights.Bold;
            markText.Foreground = new SolidColorBrush(Color.FromRgb(0x0B, 0x0C, 0x10));
            markText.HorizontalAlignment = HorizontalAlignment.Center;
            markText.VerticalAlignment = VerticalAlignment.Center;
            mark.Child = markText;
            header.Children.Add(mark);

            var titles = new StackPanel();
            titles.Margin = new Thickness(12, 0, 0, 0);
            titles.VerticalAlignment = VerticalAlignment.Center;
            var appName = new TextBlock();
            appName.Text = "Umbra";
            appName.FontSize = 15;
            appName.FontWeight = FontWeights.SemiBold;
            appName.Foreground = new SolidColorBrush(Color.FromRgb(0xEC, 0xEE, 0xF1));
            var sub = new TextBlock();
            sub.Text = I18n.T("subtitle");
            sub.FontSize = 11.5;
            sub.Foreground = new SolidColorBrush(Color.FromRgb(0x5F, 0x66, 0x72));
            titles.Children.Add(appName);
            titles.Children.Add(sub);
            Grid.SetColumn(titles, 1);
            header.Children.Add(titles);

            _langButton = new Button();
            _langButton.Content = I18n.Lang == "tr" ? "EN" : "TR";
            _langButton.Width = 42;
            _langButton.Height = 26;
            _langButton.Style = GhostButtonStyle();
            _langButton.Click += delegate { ToggleLanguage(); };
            Grid.SetColumn(_langButton, 2);
            header.Children.Add(_langButton);

            grid.Children.Add(header);

            // --- govde ---
            var body = new StackPanel();
            body.Margin = new Thickness(20, 6, 20, 4);

            _heading = new TextBlock();
            _heading.FontSize = 15;
            _heading.FontWeight = FontWeights.SemiBold;
            _heading.Foreground = new SolidColorBrush(Color.FromRgb(0xEC, 0xEE, 0xF1));
            _heading.Margin = new Thickness(0, 4, 0, 6);

            _message = new TextBlock();
            _message.FontSize = 12.5;
            _message.LineHeight = 19;
            _message.TextWrapping = TextWrapping.Wrap;
            _message.Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA1, 0xAC));

            _progress = new ProgressBar();
            _progress.Height = 6;
            _progress.Margin = new Thickness(0, 14, 0, 4);
            _progress.IsIndeterminate = true;
            _progress.Foreground = new SolidColorBrush(Color.FromRgb(0xB4, 0xBA, 0xC4));
            _progress.Visibility = Visibility.Collapsed;

            _desktopCheck = new CheckBox();
            _desktopCheck.Content = I18n.T("chk.desktop");
            _desktopCheck.IsChecked = true;
            _desktopCheck.Foreground = new SolidColorBrush(Color.FromRgb(0xCD, 0xD3, 0xDB));
            _desktopCheck.Margin = new Thickness(0, 12, 0, 0);

            body.Children.Add(_heading);
            body.Children.Add(_message);
            body.Children.Add(_desktopCheck);
            body.Children.Add(_progress);
            Grid.SetRow(body, 1);
            grid.Children.Add(body);

            // --- butonlar ---
            _buttonRow = new StackPanel();
            _buttonRow.Orientation = Orientation.Horizontal;
            _buttonRow.HorizontalAlignment = HorizontalAlignment.Right;
            _buttonRow.Margin = new Thickness(20, 14, 20, 18);
            Grid.SetRow(_buttonRow, 2);
            grid.Children.Add(_buttonRow);

            shell.Child = grid;
            Content = shell;

            if (_uninstallMode) ShowUninstallReady();
            else if (Installer.IsInstalled) ShowInstalledReady();
            else ShowReady();
        }

        private static string DetectLanguage()
        {
            try
            {
                string name = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                return name == "tr" ? "tr" : "en";
            }
            catch { return "en"; }
        }

        private void ToggleLanguage()
        {
            I18n.Lang = I18n.Lang == "tr" ? "en" : "tr";
            _langButton.Content = I18n.Lang == "tr" ? "EN" : "TR";
            _desktopCheck.Content = I18n.T("chk.desktop");
            if (_uninstallMode) ShowUninstallReady();
            else if (Installer.IsInstalled) ShowInstalledReady();
            else ShowReady();
        }

        private Button MakeButton(string text, bool primary)
        {
            var button = new Button();
            button.Content = text;
            button.MinWidth = 104;
            button.Height = 38;
            button.Margin = new Thickness(10, 0, 0, 0);
            button.Style = primary ? PrimaryButtonStyle() : GhostButtonStyle();
            return button;
        }

        private void ClearButtons()
        {
            _buttonRow.Children.Clear();
        }

        private void ShowReady()
        {
            _desktopCheck.Visibility = Visibility.Visible;
            _desktopCheck.IsChecked = true;
            _progress.Visibility = Visibility.Collapsed;
            _heading.Text = I18n.T("ready.title");
            _message.Text = I18n.T("ready.text");

            ClearButtons();
            var install = MakeButton(I18n.T("btn.install"), true);
            install.Click += delegate { RunInstall(); };
            var cancel = MakeButton(I18n.T("btn.cancel"), false);
            cancel.Click += delegate { Close(); };
            _buttonRow.Children.Add(install);
            _buttonRow.Children.Add(cancel);
        }

        private void ShowInstalledReady()
        {
            _desktopCheck.Visibility = Visibility.Visible;
            _desktopCheck.IsChecked = true;
            _progress.Visibility = Visibility.Collapsed;
            _heading.Text = I18n.T("ready.title");
            _message.Text = I18n.T("err.exists");

            ClearButtons();
            var install = MakeButton(I18n.T("btn.install"), true);
            install.Click += delegate { RunInstall(); };
            var cancel = MakeButton(I18n.T("btn.cancel"), false);
            cancel.Click += delegate { Close(); };
            _buttonRow.Children.Add(install);
            _buttonRow.Children.Add(cancel);
        }

        private void ShowDone()
        {
            _desktopCheck.Visibility = Visibility.Collapsed;
            _progress.Visibility = Visibility.Collapsed;
            _heading.Text = I18n.T("done.title");
            _message.Text = I18n.T("done.text");

            ClearButtons();
            var launch = MakeButton(I18n.T("btn.launch"), true);
            launch.Click += delegate { Installer.Launch(); Close(); };
            var close = MakeButton(I18n.T("btn.close"), false);
            close.Click += delegate { Close(); };
            _buttonRow.Children.Add(launch);
            _buttonRow.Children.Add(close);
        }

        private void ShowUninstallReady()
        {
            _desktopCheck.Visibility = Visibility.Collapsed;
            _progress.Visibility = Visibility.Collapsed;
            _heading.Text = I18n.T("un.title");
            _message.Text = I18n.T("un.text");

            ClearButtons();
            var remove = MakeButton(I18n.T("btn.uninstall"), true);
            remove.Click += delegate { RunUninstall(); };
            var cancel = MakeButton(I18n.T("btn.cancel"), false);
            cancel.Click += delegate { Close(); };
            _buttonRow.Children.Add(remove);
            _buttonRow.Children.Add(cancel);
        }

        private void ShowUninstalled()
        {
            _desktopCheck.Visibility = Visibility.Collapsed;
            _progress.Visibility = Visibility.Collapsed;
            _heading.Text = I18n.T("un.done.title");
            _message.Text = I18n.T("un.done");

            ClearButtons();
            var close = MakeButton(I18n.T("btn.close"), true);
            close.Click += delegate { Close(); };
            _buttonRow.Children.Add(close);
        }

        private void RunInstall()
        {
            if (_busy) return;
            _busy = true;
            _desktopCheck.Visibility = Visibility.Collapsed;
            _progress.Visibility = Visibility.Visible;
            _heading.Text = I18n.T("working");
            _message.Text = I18n.T("installing.todo");
            ClearButtons();

            bool desktop = _desktopCheck.IsChecked == true;

            var worker = new System.ComponentModel.BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += delegate(object s, System.ComponentModel.DoWorkEventArgs e)
            {
                var report = (System.Action<string>)e.Argument;
                Installer.KillRunning();
                Installer.Install(report, desktop);
            };
            worker.ProgressChanged += delegate(object s, System.ComponentModel.ProgressChangedEventArgs e)
            {
                _message.Text = (string)e.UserState;
            };
            worker.RunWorkerCompleted += delegate(object s, System.ComponentModel.RunWorkerCompletedEventArgs e)
            {
                _busy = false;
                if (e.Error != null)
                {
                    _progress.Visibility = Visibility.Collapsed;
                    _heading.Text = I18n.T("btn.cancel");
                    _message.Text = I18n.T("err.io") + e.Error.Message;
                    ClearButtons();
                    var close = MakeButton(I18n.T("btn.close"), true);
                    close.Click += delegate { Close(); };
                    _buttonRow.Children.Add(close);
                    return;
                }
                ShowDone();
            };
            worker.RunWorkerAsync(new System.Action<string>(delegate(string text)
            {
                Dispatcher.BeginInvoke(new Action(delegate() { _message.Text = text; }));
            }));
        }

        private void RunUninstall()
        {
            if (_busy) return;
            _busy = true;
            _progress.Visibility = Visibility.Visible;
            _heading.Text = I18n.T("working.off");
            _message.Text = I18n.T("installing.reg");
            ClearButtons();

            var worker = new System.ComponentModel.BackgroundWorker();
            worker.DoWork += delegate { Installer.Uninstall(); };
            worker.RunWorkerCompleted += delegate
            {
                _busy = false;
                ShowUninstalled();
            };
            worker.RunWorkerAsync();
        }

        // ----------------------------------------------------------------
        //  Stillar
        // ----------------------------------------------------------------
        private static Brush AccentBrush()
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 1);
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xF5, 0xF6, 0xF8), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xB4, 0xBA, 0xC4), 1));
            return brush;
        }

        private static Style PrimaryButtonStyle()
        {
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x0A, 0x0B, 0x0D))));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 12.5));
            style.Setters.Add(new Setter(Control.FontWeightProperty, FontWeights.SemiBold));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
            border.SetValue(Border.BackgroundProperty, AccentBrush());
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.OpacityProperty, 0.92));
            template.Triggers.Add(hover);
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private static Style GhostButtonStyle()
        {
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0xEC, 0xEE, 0xF1))));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 12.5));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(9));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x15, 0x18, 0x1D)));
            border.SetValue(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x25, 0x2A, 0x32)));
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;
            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x1C, 0x20, 0x28))));
            template.Triggers.Add(hover);
            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            bool uninstall = false;
            bool silent = false;
            foreach (string arg in args)
            {
                if (string.Equals(arg, "--uninstall", StringComparison.OrdinalIgnoreCase)) uninstall = true;
                if (string.Equals(arg, "--silent", StringComparison.OrdinalIgnoreCase)) silent = true;
            }

            if (silent)
            {
                if (uninstall) Installer.Uninstall();
                else Installer.Install(delegate(string text) { }, true);
                return;
            }

            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;

            var window = new SetupWindow(uninstall);
            app.MainWindow = window;
            app.Run(window);
        }
    }
}
