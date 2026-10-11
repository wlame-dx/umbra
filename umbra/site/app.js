(function () {
  "use strict";

  // ---------------------------------------------------------------- dil (i18n)
  var I18N = {
    tr: {
      "meta.title": "Umbra — ekranını kablosuz ikinci ekrana çevir",
      "meta.desc": "Umbra, Windows bilgisayarındaki ekranı yerel ağ üzerinden telefonuna ya da tabletine akıtır. Ücretsiz, filigransız, tek dosya.",
      "nav.how": "Nasıl çalışır",
      "nav.features": "Özellikler",
      "nav.faq": "SSS",
      "nav.download": "İndir",
      "hero.kicker": "Windows için · yerel ağ · ücretsiz",
      "hero.h1": "Ekranın, gölgen gibi<br>yanında olsun.",
      "hero.alt": "Umbra bilgisayarındaki ekranı kablosuz bir ikinci ekrana dönüştürür.<br>Telefonundan ya da tabletinden tarayıcıda aç, tam ekrana geç, kullan.",
      "hero.cta1": "Windows için indir",
      "hero.cta2": "Nasıl çalışır?",
      "steps.title": "Üç adımda hazır",
      "step1.h": "Çalıştır",
      "step1.p": "Umbra.exe'yi aç. Program sana bir adres verir, başka kurulum gerekmez.",
      "step2.h": "Sanal ekran kur (istersen)",
      "step2.p": "Açık kaynak sürücülerle ikinci monitörü ekle. Ekranın yoksa masaüstünü de yayınlayabilirsin.",
      "step3.h": "Bağlan",
      "step3.p": "Başka cihaz aynı Wi-Fi'daysa adresi tarayıcıda aç. Dokun, kaydır, kullan.",
      "term.body": "<span class=\"t1\">$ umbra</span>\n<span class=\"t2\">[ok]   yayın     192.168.1.9:8777</span>\n<span class=\"t2\">[ws]  istemci   android_tablet  (bağlı)</span>\n<span class=\"t2\">[fps]  30 k/sn   \u22482 MB/s   gecikme 18ms</span>\n<span class=\"t3\">ekranın artık yanında.</span>",
      "features.title": "İçinde neler var",
      "feat1.h": "Dokunmatik kontrol",
      "feat1.p": "Dokun, sürükle, iki parmakla kaydır. Uzak masaüstünde gerçek dokunma olarak işlenir.",
      "feat2.h": "Klavye aktarımı",
      "feat2.p": "Tarayıcıdaki klavyeni bilgisayara yönlendir. Kısayollar dahil.",
      "feat3.h": "Kalite ve hız ayarı",
      "feat3.p": "Kare hızı, JPEG kalitesi ve ölçek canlı ayarlanır. Yavaş ağda görüntüyü küçült, akıcılığı koru.",
      "feat4.h": "Tam ekran ve ölçekleme",
      "feat4.p": "Sığdır, doldur ya da 1:1. Tek dokunuşla tam ekrana geç.",
      "feat5.h": "Tek dosya",
      "feat5.p": "Kurulum yok, arka planda servis yok. Tıkla, kullan, kapat.",
      "feat6.h": "Filigran yok",
      "feat6.p": "Ücretsiz sürüm diye ayrı bir şey yok; tek sürüm var, o da tam.",
      "dl.h2": "Windows 10 / 11 için tek dosya",
      "dl.p": "~100 KB \u2014 .NET Framework 4.8'le çalışır (Windows'ta hazır gelir). <br>İndir, aç, çalıştır. Başka bir şey yapma.",
      "dl.btn": "Son sürümü indir",
      "dl.hint": "Kaynak kod ve kurulum rehberi: <a href=\"https://github.com/wlame-dx/umbra\">github.com/wlame-dx/umbra</a>",
      "faq.title": "Merak edilenler",
      "q1.s": "Sanal ekranı kim oluşturuyor?",
      "q1.p": "Umbra kendisi sürücü kurmaz; imzasız kernel sürücüsü dayatmak istemiyoruz. Açık kaynak IDD sürücülerinden birini kullanarak ikinci ekranı eklersin, Umbra onu otomatik bulur. Boşta ekran yoksa mevcut ekranları da yayınlayabilirsin.",
      "q2.s": "İnternet gerekli mi?",
      "q2.p": "Hayır. Görüntü yalnızca yerel ağda dolaşır, buluta çıkmaz.",
      "q3.s": "Neden spacedesk değil de bu?",
      "q3.p": "Spacedesk'in ücretsiz tarafı filigranlı. Umbra'nın tek sürümü var: filigransız ve açık kaynak.",
      "q4.s": "Android uygulaması var mı?",
      "q4.p": "Tarayıcı istemcisi Android'de tam ekran ve \"ana ekrana ekle\" ile uygulama gibi çalışıyor. Ayrı bir APK şu an için planlanmıyor.",
      "footer.tag": "gölge gibi yanında.",
      "footer.legal": "Açık kaynak &middot; MIT lisansı &middot; <a href=\"https://github.com/wlame-dx/umbra\">GitHub</a>"
    },
    en: {
      "meta.title": "Umbra — turn your screen into a wireless second display",
      "meta.desc": "Umbra streams your Windows screen over the local network to your phone or tablet. Free, no watermark, single file.",
      "nav.how": "How it works",
      "nav.features": "Features",
      "nav.faq": "FAQ",
      "nav.download": "Download",
      "hero.kicker": "For Windows · local network · free",
      "hero.h1": "Your screen, right<br>by your side.",
      "hero.alt": "Umbra turns your PC screen into a wireless second display.<br>Open it in your phone or tablet browser, go fullscreen, use it.",
      "hero.cta1": "Download for Windows",
      "hero.cta2": "How does it work?",
      "steps.title": "Ready in three steps",
      "step1.h": "Run it",
      "step1.p": "Open Umbra.exe. The program shows you an address; no further install needed.",
      "step2.h": "Add a virtual display (optional)",
      "step2.p": "Add a second monitor with an open-source driver. If you have none, you can stream your desktop too.",
      "step3.h": "Connect",
      "step3.p": "If another device is on the same Wi-Fi, open the address in a browser. Touch, drag, use it.",
      "term.body": "<span class=\"t1\">$ umbra</span>\n<span class=\"t2\">[ok]  stream    192.168.1.9:8777</span>\n<span class=\"t2\">[ws]  client    android_tablet  (connected)</span>\n<span class=\"t2\">[fps] 30 fps    \u22482 MB/s   latency 18ms</span>\n<span class=\"t3\">your screen is by your side now.</span>",
      "features.title": "What's inside",
      "feat1.h": "Touch control",
      "feat1.p": "Tap, drag, two-finger scroll. Delivered to the remote desktop as real touch input.",
      "feat2.h": "Keyboard passthrough",
      "feat2.p": "Send your browser keyboard to the PC, shortcuts included.",
      "feat3.h": "Quality and speed control",
      "feat3.p": "Frame rate, JPEG quality and scale adjust live. On a slow network, shrink the image and keep it smooth.",
      "feat4.h": "Fullscreen and scaling",
      "feat4.p": "Fit, fill or 1:1. Go fullscreen with a single tap.",
      "feat5.h": "Single file",
      "feat5.p": "No install, no background service. Click, use, close.",
      "feat6.h": "No watermark",
      "feat6.p": "There is no separate free tier; one full version, nothing held back.",
      "dl.h2": "A single file for Windows 10 / 11",
      "dl.p": "~100 KB \u2014 runs on .NET Framework 4.8 (already on Windows). <br>Download, open, run. Nothing else.",
      "dl.btn": "Download the latest build",
      "dl.hint": "Source code and setup guide: <a href=\"https://github.com/wlame-dx/umbra\">github.com/wlame-dx/umbra</a>",
      "faq.title": "Frequently asked",
      "q1.s": "Who creates the virtual display?",
      "q1.p": "Umbra does not install a driver itself; we don't want to push an unsigned kernel driver. You add a second display with an open-source IDD driver and Umbra finds it automatically. Without an extra display, it streams your existing ones.",
      "q2.s": "Does it need the internet?",
      "q2.p": "No. The picture only travels over your local network; it never reaches the cloud.",
      "q3.s": "Why not just use spacedesk?",
      "q3.p": "spacedesk's free tier is watermarked. Umbra has one version: no watermark and open source.",
      "q4.s": "Is there an Android app?",
      "q4.p": "The browser client runs fullscreen on Android and works like an app with \"Add to Home Screen\". A separate APK is not planned for now.",
      "footer.tag": "by your side, like a shadow.",
      "footer.legal": "Open source &middot; MIT license &middot; <a href=\"https://github.com/wlame-dx/umbra\">GitHub</a>"
    }
  };

  var lang = "tr";
  try { lang = localStorage.getItem("umbra.site.lang") || "tr"; } catch (e) { lang = "tr"; }
  if (lang !== "tr" && lang !== "en") lang = "tr";

  function t(key) {
    var table = I18N[lang] || I18N.tr;
    return table[key] != null ? table[key] : key;
  }

  function applyLang() {
    document.documentElement.lang = lang;
    document.title = t("meta.title");

    var desc = document.querySelector('meta[name="description"]');
    if (desc) desc.setAttribute("content", t("meta.desc"));

    var textNodes = document.querySelectorAll("[data-i18n]");
    for (var i = 0; i < textNodes.length; i++) {
      textNodes[i].textContent = t(textNodes[i].getAttribute("data-i18n"));
    }

    var htmlNodes = document.querySelectorAll("[data-i18n-html]");
    for (var j = 0; j < htmlNodes.length; j++) {
      htmlNodes[j].innerHTML = t(htmlNodes[j].getAttribute("data-i18n-html"));
    }

    var btn = document.getElementById("langBtn");
    if (btn) btn.textContent = lang === "tr" ? "EN" : "TR";
  }

  var langBtn = document.getElementById("langBtn");
  if (langBtn) {
    langBtn.addEventListener("click", function () {
      lang = lang === "tr" ? "en" : "tr";
      try { localStorage.setItem("umbra.site.lang", lang); } catch (e) { }
      applyLang();
    });
  }

  applyLang();

  // ---------------------------------------------------------------- görünüm animasyonu
  var items = document.querySelectorAll(".reveal");

  if (!("IntersectionObserver" in window)) {
    for (var k = 0; k < items.length; k++) items[k].classList.add("in");
    return;
  }

  var observer = new IntersectionObserver(function (entries) {
    entries.forEach(function (entry) {
      if (entry.isIntersecting) {
        entry.target.classList.add("in");
        observer.unobserve(entry.target);
      }
    });
  }, { threshold: 0.15 });

  items.forEach(function (item) { observer.observe(item); });
})();
