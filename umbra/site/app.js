(function () {
  "use strict";

  // ---------------------------------------------------------------- dil (i18n)
  var I18N = {
    tr: {
      "meta.title": "Umbra - ekranını telefondan kullan",
      "meta.desc": "Umbra, Windows ekranını yerel ağ üzerinden telefon veya tablete aktarır. Kurulum yok, hesap yok, filigran yok.",
      "nav.how": "Nasıl çalışır",
      "nav.features": "Özellikler",
      "nav.faq": "SSS",
      "nav.download": "İndir",
      "hero.kicker": "Windows 10 / 11 · ücretsiz",
      "hero.h1": "Bilgisayarının ekranını<br>telefonunda aç.",
      "hero.alt": "Umbra, Windows'ta çalışan küçük bir program. Ekranı yerel ağ üzerinden yayınlar; aynı Wi-Fi'deki telefon ya da tablet tarayıcıdan bağlanır.<br>Kurulum ve hesap istemez.",
      "hero.cta1": "Windows için indir",
      "hero.cta2": "Nasıl çalışır",
      "steps.title": "Kurulum yok, üç adım var",
      "step1.h": "Çalıştır",
      "step1.p": "Umbra.exe'yi aç. Sana tek bir adres gösterir. Kurulum, hesap, arka plan servisi yok.",
      "step2.h": "Adresi aç",
      "step2.p": "Telefon veya tabletten aynı ağdaki adresi tarayıcıya yaz. Görüntü hemen gelir.",
      "step3.h": "İkinci ekran (isteğe bağlı)",
      "step3.p": "Açık kaynak bir IDD sürücüsü kurarsan masaüstünü genişletirsin. İstemezsen mevcut ekranı yayınlarsın.",
      "term.body": "<span class=\"t1\">C:\\&gt; umbra.exe</span>\n<span class=\"t2\">yayın hazır: http://192.168.1.9:8777/</span>\n<span class=\"t3\">telefonda bu adresi aç. gerisi tarayıcıda halledilir.</span>",
      "features.title": "Ne var, ne yok",
      "feat1.h": "Dokunmatik kontrol",
      "feat1.p": "Dokunma, iki parmakla kaydırma desteklenir. İşletim sistemi touch enjeksiyonunu kabul etmezse fareye düşer.",
      "feat2.h": "Klavye",
      "feat2.p": "Tarayıcı klavyesini bilgisayara gönderir. Metin alanlarına yazabilirsin.",
      "feat3.h": "Kalite ayarı",
      "feat3.p": "Kare hızı, JPEG kalitesi ve ölçek çalışırken değiştirilebilir. Yavaş ağda ölçeği küçült.",
      "feat4.h": "Tam ekran",
      "feat4.p": "Sığdır, doldur veya 1:1. İstediğin zaman tam ekrana geç.",
      "feat5.h": "Tek dosya",
      "feat5.p": "Yaklaşık 180 KB, .NET Framework 4.8 ile çalışır. Kurulum yapmaz, kayıt defterini kurcalamaz.",
      "feat6.h": "Filigran yok",
      "feat6.p": "Ücretsiz sürüm ayrımı yok. Tek sürüm var, o da sınırsız.",
      "dl.h2": "Windows 10 / 11, tek dosya",
      "dl.p": "~180 KB. .NET Framework 4.8 kuruluysa (Windows'ta hazır gelir) başka bir şey gerekmez.<br>İndir, aç, çalıştır.",
      "dl.btn": "Son sürümü indir",
      "dl.hint": "Kaynak kod: <a href=\"https://github.com/wlame-dx/umbra\">github.com/wlame-dx/umbra</a>",
      "faq.title": "Sık sorulanlar",
      "q1.s": "Sanal ekran neden hemen gelmiyor?",
      "q1.p": "Windows, imzasız kernel sürücüsüne izin vermez. Bu yüzden Umbra sürücüyü kendi kurmaz. Açık kaynak bir IDD sürücüsü (mesela Virtual-Display-Driver) kurman gerekir. vdriver klasöründeki get-driver.ps1 bunu senin yerine indirir. Sürücü olmasa bile mevcut ekranı yayınlayabilirsin.",
      "q2.s": "İnternet gerekli mi?",
      "q2.p": "Hayır. Görüntü sadece yerel ağda dolaşır. Dışarıya hiçbir veri gitmez.",
      "q3.s": "Neden spacedesk değil?",
      "q3.p": "spacedesk'in ücretsiz sürümünde filigran var. Umbra ücretsiz, kaynak kodu açık ve tek dosya. Sadece daha az şey yapıyor; artısı bu.",
      "q4.s": "Android uygulaması var mı?",
      "q4.p": "Ayrı bir APK yok. Tarayıcı istemcisi Android'de tam ekran çalışıyor, \"Ana ekrana ekle\" derseniz uygulama gibi açılıyor.",
      "q5.s": "Ne kadar güvenli?",
      "q5.p": "Yayın şifresiz HTTP/WebSocket üzerinden gider; aynı ağdaki biri bağlanabilir. Ev ağı için pratik, halka açık Wi-Fi için değil. Uzaktan kontrolü istediğin an kapatabilirsin.",
      "footer.tag": "Windows için küçük bir araç.",
      "footer.legal": "Açık kaynak · MIT lisansı · <a href=\"https://github.com/wlame-dx/umbra\">GitHub</a>"
    },
    en: {
      "meta.title": "Umbra - use your screen from your phone",
      "meta.desc": "Umbra streams your Windows screen over the local network to a phone or tablet. No install, no account, no watermark.",
      "nav.how": "How it works",
      "nav.features": "Features",
      "nav.faq": "FAQ",
      "nav.download": "Download",
      "hero.kicker": "Windows 10 / 11 · free",
      "hero.h1": "Open your computer's screen<br>on your phone.",
      "hero.alt": "Umbra is a small program that runs on Windows. It streams the screen over your local network; a phone or tablet on the same Wi-Fi opens it in a browser.<br>No install, no account.",
      "hero.cta1": "Download for Windows",
      "hero.cta2": "How it works",
      "steps.title": "No setup, three steps",
      "step1.h": "Run it",
      "step1.p": "Open Umbra.exe. It shows you one address. No install, no account, no background service.",
      "step2.h": "Open the address",
      "step2.p": "On your phone or tablet, type that address into the browser (same network). The picture appears.",
      "step3.h": "Second display (optional)",
      "step3.p": "Install an open-source IDD driver and you can extend your desktop. If you don't, you just stream the current screen.",
      "term.body": "<span class=\"t1\">C:\\&gt; umbra.exe</span>\n<span class=\"t2\">streaming at: http://192.168.1.9:8777/</span>\n<span class=\"t3\">open that address on your phone. the browser does the rest.</span>",
      "features.title": "What you get",
      "feat1.h": "Touch control",
      "feat1.p": "Taps and two-finger scrolling work. If the OS refuses touch injection, it falls back to the mouse.",
      "feat2.h": "Keyboard",
      "feat2.p": "Sends your browser keyboard to the PC. Works in text fields.",
      "feat3.h": "Quality control",
      "feat3.p": "Frame rate, JPEG quality and scale can be changed while running. On a slow network, lower the scale.",
      "feat4.h": "Fullscreen",
      "feat4.p": "Fit, fill or 1:1. Go fullscreen whenever you want.",
      "feat5.h": "Single file",
      "feat5.p": "About 180 KB, runs on .NET Framework 4.8. No installer, doesn't touch the registry.",
      "feat6.h": "No watermark",
      "feat6.p": "No separate free tier. One version, no limits.",
      "dl.h2": "Windows 10 / 11, single file",
      "dl.p": "~180 KB. If .NET Framework 4.8 is present (it ships with Windows) you need nothing else.<br>Download, open, run.",
      "dl.btn": "Download the latest build",
      "dl.hint": "Source code: <a href=\"https://github.com/wlame-dx/umbra\">github.com/wlame-dx/umbra</a>",
      "faq.title": "Common questions",
      "q1.s": "Why doesn't the virtual display show up right away?",
      "q1.p": "Windows won't load an unsigned kernel driver, so Umbra doesn't install one. You need an open-source IDD driver (Virtual-Display-Driver, for example). The get-driver.ps1 script in the vdriver folder downloads it for you. Even without a driver you can stream the current screen.",
      "q2.s": "Does it need the internet?",
      "q2.p": "No. The picture only travels over your local network. Nothing leaves the LAN.",
      "q3.s": "Why not spacedesk?",
      "q3.p": "spacedesk's free tier has a watermark. Umbra is free, open source and a single file. It simply does less; that's the point.",
      "q4.s": "Is there an Android app?",
      "q4.p": "No separate APK. The browser client runs fullscreen on Android, and \"Add to Home screen\" makes it open like an app.",
      "q5.s": "How secure is it?",
      "q5.p": "The stream runs over plain HTTP/WebSocket, so anyone on the same network could connect. Fine for home Wi-Fi, not for public Wi-Fi. You can turn off remote control at any time.",
      "footer.tag": "A small tool for Windows.",
      "footer.legal": "Open source · MIT license · <a href=\"https://github.com/wlame-dx/umbra\">GitHub</a>"
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
