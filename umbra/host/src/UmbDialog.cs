using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Umbra
{
    /// <summary>Umbra temasiyla eslesen uyari / onay penceresi.</summary>
    internal sealed class UmbDialog : Window
    {
        private bool _result;
        private Grid _body;
        private Grid _panel;

        private UmbDialog()
        {
            Title = "Umbra";
            Width = 470;
            SizeToContent = SizeToContent.Height;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = false;
            Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0B, 0x0E));
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;

            // Duzgun gozukmesi icin WindowChrome kullanilmaz; baslik surukleme elle yapilir.
            Content = BuildShell();
        }

        private FrameworkElement BuildShell()
        {
            var shell = new Border();
            shell.Margin = new Thickness(10);
            shell.CornerRadius = new CornerRadius(14);
            shell.BorderBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x2B, 0x34));
            shell.BorderThickness = new Thickness(1);
            shell.Background = CreateGradient(0x12, 0x15, 0x1C, 0x0B, 0x0D, 0x11);

            var panel = new Grid();
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _panel = panel;

            panel.Children.Add(BuildHeader());
            _body = new Grid();
            _body.Margin = new Thickness(20, 2, 20, 4);
            Grid.SetRow(_body, 1);
            panel.Children.Add(_body);

            shell.Child = panel;
            return shell;
        }

        private FrameworkElement BuildHeader()
        {
            var header = new Grid();
            header.Margin = new Thickness(18, 14, 18, 10);
            header.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                if (e.ButtonState == MouseButtonState.Pressed) { try { DragMove(); } catch { } }
            };
            header.Cursor = Cursors.SizeAll;

            var stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;
            stack.VerticalAlignment = VerticalAlignment.Center;

            var mark = new Border();
            mark.Width = 12;
            mark.Height = 12;
            mark.CornerRadius = new CornerRadius(4);
            mark.Background = AccentBrush();
            mark.VerticalAlignment = VerticalAlignment.Center;

            var title = new TextBlock();
            title.Name = "DlgTitle";
            title.Margin = new Thickness(10, 0, 0, 0);
            title.Foreground = new SolidColorBrush(Color.FromRgb(0xEC, 0xEE, 0xF1));
            title.FontSize = 14;
            title.FontWeight = FontWeights.SemiBold;
            title.VerticalAlignment = VerticalAlignment.Center;

            stack.Children.Add(mark);
            stack.Children.Add(title);
            header.Children.Add(stack);

            var close = new Button();
            close.Content = "\u2715";
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.VerticalAlignment = VerticalAlignment.Center;
            close.Style = CloseButtonStyle();
            close.Click += delegate { DialogResult = false; Close(); };
            header.Children.Add(close);

            return header;
        }

        private void SetActionButtons(string okText, bool twoButtons, Action onOk, Action onCancel)
        {
            var row = new Grid();
            row.Margin = new Thickness(20, 2, 20, 20);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var ok = new Button();
            ok.Content = okText;
            ok.MinWidth = 96;
            ok.Height = 36;
            ok.Margin = new Thickness(0, 0, 10, 0);
            ok.Style = PrimaryButtonStyle();
            ok.Click += delegate { _result = true; if (onOk != null) onOk(); Close(); };

            var cancel = new Button();
            cancel.Content = twoButtons ? I18n.T("dlg.cancel") : I18n.T("dlg.ok");
            cancel.MinWidth = 96;
            cancel.Height = 36;
            cancel.Style = GhostButtonStyle();
            cancel.Click += delegate { _result = false; if (onCancel != null) onCancel(); Close(); };

            if (twoButtons)
            {
                Grid.SetColumn(ok, 1);
                Grid.SetColumn(cancel, 2);
                row.Children.Add(ok);
                row.Children.Add(cancel);
            }
            else
            {
                // tek buton: sagda "Tamam"
                Grid.SetColumn(cancel, 2);
                row.Children.Add(cancel);
            }

            Grid.SetRow(row, 2);
            _panel.Children.Add(row);
        }

        private void SetBody(string message)
        {
            _body.Children.Clear();
            var text = new TextBlock();
            text.Text = message;
            text.Foreground = new SolidColorBrush(Color.FromRgb(0x9A, 0xA1, 0xAC));
            text.FontSize = 12.5;
            text.TextWrapping = TextWrapping.Wrap;
            text.LineHeight = 20;
            text.Margin = new Thickness(0, 0, 0, 4);
            _body.Children.Add(text);
        }

        private void SetBodyLines(IEnumerable<string> lines)
        {
            _body.Children.Clear();
            foreach (string line in lines)
            {
                var wrap = new TextBlock();
                wrap.TextWrapping = TextWrapping.Wrap;
                wrap.Margin = new Thickness(0, 0, 0, 8);
                wrap.LineHeight = 20;

                var inlineStack = new StackPanel();
                inlineStack.Orientation = Orientation.Horizontal;

                var bullet = new TextBlock();
                bullet.Text = "\u2022  ";
                bullet.Foreground = new SolidColorBrush(Color.FromRgb(0xD9, 0xA4, 0x41));
                bullet.FontSize = 12.5;

                var item = new TextBlock();
                item.Text = line;
                item.Foreground = new SolidColorBrush(Color.FromRgb(0xCD, 0xD3, 0xDB));
                item.FontSize = 12.5;
                item.TextWrapping = TextWrapping.Wrap;

                inlineStack.Children.Add(bullet);
                inlineStack.Children.Add(item);
                _body.Children.Add(inlineStack);
            }
        }

        // ------------------------------------------------------------------
        //  Statik yardimcilar
        // ------------------------------------------------------------------

        public static void Warn(Window owner, string title, string message)
        {
            var dialog = new UmbDialog();
            dialog.Owner = owner;
            ((TextBlock)dialog.FindName("DlgTitle")).Text = title;
            dialog.SetBody(message);
            dialog.SetActionButtons(I18n.T("dlg.ok"), false, null, null);
            dialog.ShowDialog();
        }

        /// <summary>Birden fazla uyari maddesi gosterir. Cift buton: "Başlat" / "Vazgeç".</summary>
        public static bool ConfirmMany(Window owner, string title, string confirmText, IEnumerable<string> lines)
        {
            var dialog = new UmbDialog();
            dialog.Owner = owner;
            ((TextBlock)dialog.FindName("DlgTitle")).Text = title;
            dialog.SetBodyLines(lines);
            dialog.SetActionButtons(confirmText, true, null, null);
            dialog.ShowDialog();
            return dialog._result;
        }

        public static bool Confirm(Window owner, string title, string message)
        {
            var dialog = new UmbDialog();
            dialog.Owner = owner;
            ((TextBlock)dialog.FindName("DlgTitle")).Text = title;
            dialog.SetBody(message);
            dialog.SetActionButtons(I18n.T("dlg.yes"), true, null, null);
            dialog.ShowDialog();
            return dialog._result;
        }

        // ------------------------------------------------------------------
        //  Fircalar / stiller
        // ------------------------------------------------------------------

        private static Brush AccentBrush()
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 1);
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xF5, 0xF6, 0xF8), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(0xB4, 0xBA, 0xC4), 1));
            return brush;
        }

        private static Brush CreateGradient(byte r1, byte g1, byte b1, byte r2, byte g2, byte b2)
        {
            var brush = new LinearGradientBrush();
            brush.StartPoint = new Point(0, 0);
            brush.EndPoint = new Point(1, 1);
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(r1, g1, b1), 0));
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(r2, g2, b2), 1));
            return brush;
        }

        private Style PrimaryButtonStyle()
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

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Border.OpacityProperty, 0.92));
            template.Triggers.Add(trigger);

            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private Style GhostButtonStyle()
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

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x1C, 0x20, 0x28))));
            template.Triggers.Add(trigger);

            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }

        private Style CloseButtonStyle()
        {
            var style = new Style(typeof(Button));
            style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x9A, 0xA1, 0xAC))));
            style.Setters.Add(new Setter(Control.WidthProperty, 26.0));
            style.Setters.Add(new Setter(Control.HeightProperty, 22.0));
            style.Setters.Add(new Setter(Control.FontSizeProperty, 11.0));
            style.Setters.Add(new Setter(FrameworkElement.CursorProperty, Cursors.Hand));

            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xC0, 0x44, 0x4B))));
            trigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
            template.Triggers.Add(trigger);

            style.Setters.Add(new Setter(Control.TemplateProperty, template));
            return style;
        }
    }
}