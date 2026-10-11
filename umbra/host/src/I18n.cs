using System;
using System.Collections.Generic;

namespace Umbra
{
    /// <summary>
    /// Basit iki dilli arayuz destegi (Turkce / Ingilizce).
    /// Dil degistiginde arayuz elemanlari yeniden doldurulur.
    /// </summary>
    internal static class I18n
    {
        public const string Tr = "tr";
        public const string En = "en";

        public static string Language = Tr;

        // Anahtar -> [Turkce, Ingilizce]
        private static readonly Dictionary<string, string[]> Map = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            { "app.tagline",          new[] { "ikinci ekran, kablosuz", "second screen, wireless" } },

            { "status.idle",          new[] { "Beklemede", "Idle" } },
            { "status.live",          new[] { "Yayında", "Streaming" } },
            { "btn.start",            new[] { "Yayını Başlat", "Start Stream" } },
            { "btn.stop",             new[] { "Yayını Durdur", "Stop Stream" } },

            { "url.label",            new[] { "Tarayıcı adresi", "Browser address" } },
            { "url.copy",             new[] { "Kopyala", "Copy" } },
            { "url.open",             new[] { "Tarayıcıda aç", "Open in browser" } },

            { "screen.title",         new[] { "EKRAN", "DISPLAY" } },
            { "screen.refresh",       new[] { "Yenile", "Refresh" } },
            { "screen.source",        new[] { "Kaynak ekran", "Source display" } },
            { "screen.res",           new[] { "Çözünürlük", "Resolution" } },
            { "screen.apply",         new[] { "Çözünürlüğü uygula", "Apply resolution" } },
            { "screen.monitor",       new[] { "Ekran {0}", "Screen {0}" } },
            { "screen.primary",       new[] { "(ana)", "(primary)" } },

            { "quality.title",        new[] { "AKIŞ VE KALİTE", "STREAM & QUALITY" } },
            { "quality.fps",          new[] { "Kare hızı", "Frame rate" } },
            { "quality.jpeg",         new[] { "JPEG kalitesi", "JPEG quality" } },
            { "quality.scale",        new[] { "Ölçek (bant genişliği)", "Scale (bandwidth)" } },

            { "control.title",        new[] { "UZAKTAN KONTROL", "REMOTE CONTROL" } },
            { "control.input",        new[] { "Dokunmatik / fare kontrolü", "Touch / mouse control" } },
            { "control.mode",         new[] { "Girdi kipi", "Input mode" } },
            { "control.mode.touch",   new[] { "Dokunmatik (çoklu parmak)", "Touch (multi-finger)" } },
            { "control.mode.mouse",   new[] { "Fare imleci", "Mouse cursor" } },
            { "control.port",         new[] { "Bağlantı noktası (port)", "Port" } },
            { "control.vd",           new[] { "Sanal ekranı otomatik oluştur/kapat", "Auto create virtual display" } },
            { "control.tip",          new[]
                {
                    "İpucu: Sanal ekran sürücüsü kuruluysa ikinci ekranı seçip yayınlayın. Telefon veya tablet aynı ağdan tarayıcıyla bağlanır.",
                    "Tip: If a virtual display driver is installed, select the second screen and stream. A phone or tablet connects with a browser on the same network."
                } },

            { "preview.title",        new[] { "ÖNİZLEME", "PREVIEW" } },
            { "preview.hint",         new[] { "Yayın başlatıldığında burada görünür", "Will appear here when streaming starts" } },
            { "log.title",            new[] { "GÜNLÜK", "LOG" } },

            // --- diyaloglar ---
            { "dlg.warn",             new[] { "Uyarı", "Warning" } },
            { "dlg.ok",               new[] { "Tamam", "OK" } },
            { "dlg.cancel",           new[] { "Vazgeç", "Cancel" } },
            { "dlg.start_title",      new[] { "Yayına başlamadan önce", "Before starting the stream" } },
            { "dlg.start_cont",       new[] { "Başlat", "Start" } },
            { "dlg.stop_title",       new[] { "Yayını durdur", "Stop the stream" } },
            { "dlg.stop_msg",         new[]
                {
                    "Yayın durdurulacak ve bağlı istemciler ayrılacak. Devam edilsin mi?",
                    "The stream will stop and connected clients will disconnect. Continue?"
                } },
            { "dlg.yes",              new[] { "Evet", "Yes" } },
            { "dlg.no",               new[] { "Hayır", "No" } },

            // --- yayin oncesi uyarilar ---
            { "warn.touch_off",       new[]
                {
                    "Uzaktan kontrol kapalı: istemci yalnızca görüntüleyebilir.",
                    "Remote control is off: clients can only view the screen."
                } },
            { "warn.touch_fail",      new[]
                {
                    "Bu bilgisayarda dokunma enjeksiyonu kullanılamıyor. Dokunma olayları fare olarak uygulanacak.",
                    "Touch injection is not available on this PC. Touch events will be applied as mouse."
                } },
            { "warn.vd_missing",      new[]
                {
                    "Sanal ekran sürücüsü bulunamadı (vdriver klasörü yok). Mevcut ekran yayınlanacak.",
                    "Virtual display driver not found (no vdriver folder). The existing display will be streamed."
                } },
            { "warn.vd_failed",       new[]
                {
                    "Sanal ekran oluşturulamadı. Mevcut ekran yayınlanacak.",
                    "Could not create the virtual display. The existing display will be streamed."
                } },
            { "warn.port_busy",       new[]
                {
                    "Port {0} kullanımda görünüyor; yayın başlatılamayabilir. Başka bir port seçmeyi düşünün.",
                    "Port {0} looks busy; the stream may fail. Consider choosing another port."
                } },
            { "warn.scale",           new[]
                {
                    "Ölçek 1'den küçük: görüntü hafif bulanık olabilir.",
                    "Scale is below 1: the image may look slightly blurry."
                } },

            // --- gunluk ---
            { "log.copied",           new[] { "Adres kopyalandı.", "Address copied." } },
            { "log.copy_fail",        new[] { "Adres kopyalanamadı.", "Could not copy the address." } },
            { "log.ready",            new[]
                {
                    "Umbra hazır. Yayını başlatmak için düğmeye bas.",
                    "Umbra is ready. Press the button to start streaming."
                } },
            { "log.monitors",         new[] { "Ekranlar yenilendi: {0} adet.", "Displays refreshed: {0}." } },
            { "log.start",            new[]
                {
                    "Yayın başladı. Adres: http://{0}:{1}/",
                    "Streaming started. Address: http://{0}:{1}/"
                } },
            { "log.stop",             new[] { "Yayın durduruldu.", "Stream stopped." } },
            { "log.no_source",        new[] { "Kaynak ekran seçilmedi.", "No source display selected." } },
            { "log.bad_port",         new[] { "Geçersiz bağlantı noktası, 8777 kullanılacak.", "Invalid port, using 8777." } },
            { "log.server_fail",      new[]
                {
                    "Sunucu başlatılamadı (port {0}): {1}",
                    "Could not start server (port {0}): {1}"
                } },
            { "log.capture_start",    new[] { "Yakalama başladı: {0} {1}x{2}", "Capture started: {0} {1}x{2}" } },
            { "log.res_set",          new[] { "Çözünürlük ayarlandı: {0}x{1}", "Resolution set: {0}x{1}" } },
            { "log.res_already",      new[] { "Çözünürlük zaten {0}x{1}.", "Resolution is already {0}x{1}." } },
            { "log.res_fail",         new[]
                {
                    "Çözünürlük değiştirilemedi (sürücü bu kipi sunmuyor olabilir).",
                    "Could not change resolution (the driver may not offer this mode)."
                } },
            { "log.vd_creating",      new[] { "Sanal ekran oluşturuluyor…", "Creating virtual display…" } },
            { "log.vd_removing",      new[] { "Sanal ekran kaldırılıyor…", "Removing virtual display…" } },
            { "log.vd_created",       new[] { "Sanal ekran hazır.", "Virtual display is ready." } },
            { "log.vd_removed",       new[] { "Sanal ekran kaldırıldı.", "Virtual display removed." } },
            { "log.vd_failed",        new[]
                {
                    "Sanal ekran oluşturulamadı (yönetici yetkisi gerekebilir).",
                    "Could not create virtual display (administration may be required)."
                } },
            { "log.lang",             new[] { "Dil: Türkçe", "Language: English" } },
        };

        public static string T(string key)
        {
            string[] pair;
            if (!Map.TryGetValue(key, out pair)) return key;
            return Language == En ? pair[1] : pair[0];
        }

        public static string F(string key, params object[] args)
        {
            try { return string.Format(T(key), args); }
            catch { return T(key); }
        }
    }
}