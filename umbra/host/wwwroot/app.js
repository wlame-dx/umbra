(function () {
  "use strict";

  var screenEl = document.getElementById("screen");
  var overlay = document.getElementById("overlay");
  var overlayText = document.getElementById("overlayText");
  var retry = document.getElementById("retry");
  var toolbar = document.getElementById("toolbar");
  var statsEl = document.getElementById("stats");
  var panel = document.getElementById("panel");
  var dot = document.getElementById("dot");
  var hint = document.getElementById("hint");

  var sState = document.getElementById("sState");
  var sRes = document.getElementById("sRes");
  var sFps = document.getElementById("sFps");
  var sNet = document.getElementById("sNet");
  var sPing = document.getElementById("sPing");

  var btnFit = document.getElementById("btnFit");
  var btnFull = document.getElementById("btnFull");
  var btnStats = document.getElementById("btnStats");
  var btnKeys = document.getElementById("btnKeys");
  var btnPanel = document.getElementById("btnPanel");
  var btnLang = document.getElementById("btnLang");
  var panelClose = document.getElementById("panelClose");

  var segFit = document.getElementById("segFit");
  var segKeys = document.getElementById("segKeys");
  var segControl = document.getElementById("segControl");
  var qSlider = document.getElementById("q");
  var fSlider = document.getElementById("f");
  var sSlider = document.getElementById("s");
  var qVal = document.getElementById("qVal");
  var fVal = document.getElementById("fVal");
  var sVal = document.getElementById("sVal");

  var ws = null;
  var connected = false;
  var naturalW = 1920;
  var naturalH = 1080;
  var fit = "contain";
  var keyboardOn = false;
  var prevUrl = null;

  var frameCount = 0;
  var byteCount = 0;
  var frameRate = 0;
  var byteRate = 0;
  var latency = 0;
  var lastTs = 0;
  var lastFrameAt = 0;

  var coarse = window.matchMedia && window.matchMedia("(pointer: coarse)").matches;
  var controlMode = coarse ? "touch" : "mouse";

  // ---------------------------------------------------------------- dil (i18n)
  var I18N = {
    tr: {
      "title": "Umbra · İkinci Ekran",
      "overlay.connecting": "Sunucuya bağlanılıyor…",
      "overlay.disconnected": "Bağlantı kesildi. Yeniden deneniyor…",
      "retry": "Yeniden dene",
      "state.connected": "Bağlı",
      "state.connecting": "Bağlanıyor",
      "state.disconnected": "Bağlı değil",
      "state.waiting": "Bekleniyor",
      "hint.connected": "bağlı",
      "hint.connecting": "bağlanıyor…",
      "hint.disconnected": "bağlı değil",
      "stat.state": "Durum",
      "stat.res": "Çözünürlük",
      "stat.fps": "Kare hızı",
      "stat.data": "Veri",
      "stat.ping": "Gecikme",
      "fit.contain": "Sığdır",
      "fit.cover": "Doldur",
      "fit.none": "1:1",
      "btn.full": "Tam ekran",
      "btn.exitfull": "Tam ekrandan çık",
      "btn.stats": "Bilgi",
      "btn.keys.on": "Klavye: açık",
      "btn.keys.off": "Klavye: kapalı",
      "btn.panel": "Ayarlar",
      "panel.title": "Ayarlar",
      "panel.close": "Kapat",
      "row.display": "Görüntü",
      "row.control": "Kontrol",
      "row.quality": "Kalite",
      "row.fps": "Kare hızı",
      "row.scale": "Ölçek",
      "row.keyboard": "Klavye aktarımı",
      "seg.on": "Açık",
      "seg.off": "Kapalı",
      "ctrl.touch": "Dokunmatik",
      "ctrl.mouse": "Fare",
      "note": "Ayarlar bu oturum boyunca geçerlidir ve sunucuya anında uygulanır."
    },
    en: {
      "title": "Umbra · Second Screen",
      "overlay.connecting": "Connecting to the server…",
      "overlay.disconnected": "Connection lost. Retrying…",
      "retry": "Retry",
      "state.connected": "Connected",
      "state.connecting": "Connecting",
      "state.disconnected": "Disconnected",
      "state.waiting": "Waiting",
      "hint.connected": "connected",
      "hint.connecting": "connecting…",
      "hint.disconnected": "disconnected",
      "stat.state": "Status",
      "stat.res": "Resolution",
      "stat.fps": "Frame rate",
      "stat.data": "Data",
      "stat.ping": "Latency",
      "fit.contain": "Fit",
      "fit.cover": "Fill",
      "fit.none": "1:1",
      "btn.full": "Fullscreen",
      "btn.exitfull": "Exit fullscreen",
      "btn.stats": "Info",
      "btn.keys.on": "Keyboard: on",
      "btn.keys.off": "Keyboard: off",
      "btn.panel": "Settings",
      "panel.title": "Settings",
      "panel.close": "Close",
      "row.display": "Display",
      "row.control": "Control",
      "row.quality": "Quality",
      "row.fps": "Frame rate",
      "row.scale": "Scale",
      "row.keyboard": "Keyboard",
      "seg.on": "On",
      "seg.off": "Off",
      "ctrl.touch": "Touch",
      "ctrl.mouse": "Mouse",
      "note": "Settings apply for this session and are sent to the server instantly."
    }
  };

  var lang = "tr";
  try { lang = localStorage.getItem("umbra.lang") || (coarse ? "tr" : "en"); } catch (e) { lang = "tr"; }
  if (lang !== "tr" && lang !== "en") lang = "tr";

  function t(key) {
    var table = I18N[lang] || I18N.tr;
    return table[key] != null ? table[key] : key;
  }

  function applyLang() {
    document.documentElement.lang = lang;
    document.title = t("title");

    var nodes = document.querySelectorAll("[data-i18n]");
    for (var i = 0; i < nodes.length; i++) {
      var key = nodes[i].getAttribute("data-i18n");
      nodes[i].textContent = t(key);
    }

    // dil butonu: hedef dili gosterir
    btnLang.textContent = lang === "tr" ? "EN" : "TR";

    // dinamik etiketleri yenile
    applyConnectionText();
    applyFit();
    setKeyboard(keyboardOn);
    refreshStats();
    updateResolution();
    updateFullscreenButton();
    applyControlMode(controlMode);
  }

  function setLang(next) {
    lang = next === "en" ? "en" : "tr";
    try { localStorage.setItem("umbra.lang", lang); } catch (e) { }
    applyLang();
  }

  // ---------------------------------------------------------------- bağlantı
  function applyConnectionText() {
    if (connected) {
      dot.className = "dot on";
      hint.textContent = t("hint.connected");
      sState.textContent = t("state.connected");
    } else if (connecting) {
      dot.className = "dot";
      hint.textContent = t("hint.connecting");
      sState.textContent = t("state.connecting");
    } else {
      dot.className = "dot";
      hint.textContent = t("hint.disconnected");
      sState.textContent = t("state.disconnected");
    }
  }

  var connecting = false;

  function setState(mode) {
    connected = mode === "on";
    connecting = mode === "connecting";
    applyConnectionText();
  }

  function connect() {
    if (ws) {
      try { ws.close(); } catch (e) { }
      ws = null;
    }

    var proto = location.protocol === "https:" ? "wss" : "ws";
    setState("connecting");

    var socket;
    try { socket = new WebSocket(proto + "://" + location.host + "/"); }
    catch (e) { setTimeout(connect, 1500); return; }

    ws = socket;
    socket.binaryType = "arraybuffer";

    socket.onopen = function () {
      setState("on");
      overlay.classList.add("hidden");
      lastTs = Date.now();
      send({ t: "mode", m: controlMode });
      startTimers();
    };

    socket.onclose = function () {
      if (ws !== socket) return;
      setState("off");
      stopTimers();
      overlay.classList.remove("hidden");
      overlayText.textContent = t("overlay.disconnected");
      setTimeout(connect, 1500);
    };

    socket.onerror = function () { /* onclose devralır */ };
    socket.onmessage = onMessage;
  }

  function onMessage(event) {
    if (typeof event.data === "string") { handleText(event.data); return; }

    var blob = new Blob([event.data], { type: "image/jpeg" });
    var url = URL.createObjectURL(blob);
    screenEl.src = url;
    if (prevUrl) URL.revokeObjectURL(prevUrl);
    prevUrl = url;

    frameCount++;
    byteCount += event.data.byteLength;
    lastFrameAt = Date.now();
  }

  function handleText(text) {
    var msg;
    try { msg = JSON.parse(text); } catch (e) { return; }

    if (msg.t === "hello") {
      if (msg.w) naturalW = msg.w;
      if (msg.h) naturalH = msg.h;
      updateResolution();
    } else if (msg.t === "pong") {
      latency = Math.max(0, Date.now() - msg.ts);
    }
  }

  function updateResolution() {
    sRes.textContent = naturalW + " × " + naturalH;
    if (connected) hint.textContent = naturalW + "×" + naturalH;
  }

  function send(obj) {
    if (!ws || ws.readyState !== 1) return;
    try { ws.send(JSON.stringify(obj)); } catch (e) { }
  }

  // ---------------------------------------------------------------- zamanlayıcılar
  var timers = [];

  function startTimers() {
    stopTimers();
    timers.push(setInterval(function () {
      frameRate = frameCount;
      byteRate = byteCount;
      frameCount = 0;
      byteCount = 0;
      refreshStats();
    }, 1000));

    timers.push(setInterval(function () {
      send({ t: "ping", ts: Date.now() });
    }, 2000));

    timers.push(setInterval(function () {
      if (lastFrameAt && Date.now() - lastFrameAt > 4000) sState.textContent = t("state.waiting");
      else if (connected) sState.textContent = t("state.connected");
    }, 2000));
  }

  function stopTimers() {
    for (var i = 0; i < timers.length; i++) clearInterval(timers[i]);
    timers = [];
    frameRate = 0;
    byteRate = 0;
    refreshStats();
  }

  function formatBytes(n) {
    if (n < 1024) return n + " B/s";
    if (n < 1024 * 1024) return (n / 1024).toFixed(0) + " KB/s";
    return (n / (1024 * 1024)).toFixed(2) + " MB/s";
  }

  function refreshStats() {
    sFps.textContent = frameRate + " fps";
    sNet.textContent = formatBytes(byteRate);
    sPing.textContent = latency > 0 ? latency + " ms" : "—";
  }

  // ---------------------------------------------------------------- koordinat
  function clamp01(v) { return v < 0 ? 0 : (v > 1 ? 1 : v); }

  function mapPoint(clientX, clientY) {
    var rect = screenEl.getBoundingClientRect();
    var cw = rect.width, ch = rect.height;
    var dw, dh, scale;

    if (fit === "cover") {
      scale = Math.max(cw / naturalW, ch / naturalH);
      dw = naturalW * scale; dh = naturalH * scale;
    } else if (fit === "none") {
      dw = naturalW; dh = naturalH;
    } else {
      scale = Math.min(cw / naturalW, ch / naturalH);
      dw = naturalW * scale; dh = naturalH * scale;
    }

    var offX = (cw - dw) / 2;
    var offY = (ch - dh) / 2;
    var x = clamp01((clientX - rect.left - offX) / dw);
    var y = clamp01((clientY - rect.top - offY) / dh);
    return { x: Math.round(x * 10000) / 10000, y: Math.round(y * 10000) / 10000 };
  }

  function buttonOf(e) {
    if (e.button === 2) return 1;
    if (e.button === 1) return 2;
    return 0;
  }

  // ---------------------------------------------------------------- işaretçi
  var moveQueued = false;
  var movePoint = null;

  function flushMove() {
    moveQueued = false;
    if (!movePoint) return;
    send({ t: "p", a: "move", x: movePoint.x, y: movePoint.y, b: 0, id: movePoint.id });
    movePoint = null;
  }

  screenEl.addEventListener("pointerdown", function (e) {
    if (!connected) return;
    e.preventDefault();
    try { screenEl.setPointerCapture(e.pointerId); } catch (er) { }
    var p = mapPoint(e.clientX, e.clientY);
    send({ t: "p", a: "down", x: p.x, y: p.y, b: buttonOf(e), id: e.pointerId });
  });

  screenEl.addEventListener("pointermove", function (e) {
    if (!connected) return;
    e.preventDefault();
    // Dokunmatik kipte, parmak basili degilken hareketi yok say (hayalet imlec olmasin).
    if (controlMode === "touch" && e.buttons === 0) return;
    var p = mapPoint(e.clientX, e.clientY);
    movePoint = { x: p.x, y: p.y, id: e.pointerId };
    if (!moveQueued) {
      moveQueued = true;
      requestAnimationFrame(flushMove);
    }
  });

  function endPointer(e) {
    if (!connected) return;
    var p = mapPoint(e.clientX, e.clientY);
    send({ t: "p", a: "up", x: p.x, y: p.y, b: buttonOf(e), id: e.pointerId });
  }

  screenEl.addEventListener("pointerup", endPointer);
  screenEl.addEventListener("pointercancel", endPointer);
  screenEl.addEventListener("contextmenu", function (e) { e.preventDefault(); });

  screenEl.addEventListener("wheel", function (e) {
    if (!connected) return;
    e.preventDefault();
    var p = mapPoint(e.clientX, e.clientY);
    var dir = e.deltaY > 0 ? -1 : 1;
    send({ t: "wheel", d: dir * 120, x: p.x, y: p.y });
  }, { passive: false });

  // ---------------------------------------------------------------- klavye
  function isTyping(el) {
    if (!el) return false;
    var tag = (el.tagName || "").toLowerCase();
    return tag === "input" || tag === "textarea" || tag === "select" || el.isContentEditable;
  }

  window.addEventListener("keydown", function (e) {
    if (!keyboardOn || !connected || isTyping(e.target)) return;
    e.preventDefault();
    send({ t: "key", a: "down", code: e.code, key: e.key.length === 1 ? e.key : "" });
  });

  window.addEventListener("keyup", function (e) {
    if (!keyboardOn || !connected || isTyping(e.target)) return;
    e.preventDefault();
    send({ t: "key", a: "up", code: e.code, key: e.key.length === 1 ? e.key : "" });
  });

  // ---------------------------------------------------------------- görünüm
  function applyFit() {
    screenEl.style.objectFit = fit;
    var buttons = segFit.querySelectorAll("button");
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].classList.toggle("active", buttons[i].getAttribute("data-fit") === fit);
    }
    btnFit.textContent = t("fit." + fit);
  }

  function setFit(value) {
    fit = value;
    applyFit();
  }

  segFit.addEventListener("click", function (e) {
    var target = e.target.closest("button");
    if (target) setFit(target.getAttribute("data-fit"));
  });

  btnFit.addEventListener("click", function () {
    setFit(fit === "contain" ? "cover" : (fit === "cover" ? "none" : "contain"));
  });

  function updateFullscreenButton() {
    var doc = document;
    var isFull = doc.fullscreenElement || doc.webkitFullscreenElement || doc.msFullscreenElement;
    btnFull.textContent = isFull ? t("btn.exitfull") : t("btn.full");
  }

  function toggleFullscreen() {
    var doc = document;
    var el = doc.documentElement;
    var isFull = doc.fullscreenElement || doc.webkitFullscreenElement || doc.msFullscreenElement;
    try {
      if (!isFull) {
        var request = el.requestFullscreen || el.webkitRequestFullscreen || el.msRequestFullscreen;
        if (request) request.call(el);
      } else {
        var exit = doc.exitFullscreen || doc.webkitExitFullscreen || doc.msExitFullscreen;
        if (exit) exit.call(doc);
      }
    } catch (e) { }
  }

  btnFull.addEventListener("click", toggleFullscreen);
  document.addEventListener("fullscreenchange", updateFullscreenButton);

  btnStats.addEventListener("click", function () {
    statsEl.classList.toggle("hidden");
    btnStats.classList.toggle("active", !statsEl.classList.contains("hidden"));
  });

  // ---------------------------------------------------------------- kontrol kipi
  function applyControlMode(mode) {
    controlMode = mode;
    var buttons = segControl.querySelectorAll("button");
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].classList.toggle("active", buttons[i].getAttribute("data-m") === mode);
    }
  }

  function setControlMode(mode) {
    if (mode !== "touch" && mode !== "mouse") return;
    applyControlMode(mode);
    send({ t: "mode", m: mode });
  }

  segControl.addEventListener("click", function (e) {
    var target = e.target.closest("button");
    if (target) setControlMode(target.getAttribute("data-m"));
  });

  // ---------------------------------------------------------------- ayarlar
  function setKeyboard(value) {
    keyboardOn = value;
    btnKeys.textContent = value ? t("btn.keys.on") : t("btn.keys.off");
    btnKeys.classList.toggle("active", value);
    var buttons = segKeys.querySelectorAll("button");
    for (var i = 0; i < buttons.length; i++) {
      buttons[i].classList.toggle("active", (buttons[i].getAttribute("data-k") === "1") === value);
    }
  }

  btnKeys.addEventListener("click", function () { setKeyboard(!keyboardOn); });

  segKeys.addEventListener("click", function (e) {
    var target = e.target.closest("button");
    if (target) setKeyboard(target.getAttribute("data-k") === "1");
  });

  btnPanel.addEventListener("click", function () { panel.classList.toggle("hidden"); });
  panelClose.addEventListener("click", function () { panel.classList.add("hidden"); });

  btnLang.addEventListener("click", function () { setLang(lang === "tr" ? "en" : "tr"); });

  function bindSlider(slider, label, formatter, key) {
    slider.addEventListener("input", function () {
      label.textContent = formatter(slider.value);
    });
    slider.addEventListener("change", function () {
      var payload = { t: "set" };
      payload[key] = parseFloat(slider.value);
      send(payload);
    });
  }

  bindSlider(qSlider, qVal, function (v) { return v; }, "quality");
  bindSlider(fSlider, fVal, function (v) { return v; }, "fps");
  bindSlider(sSlider, sVal, function (v) { return parseFloat(v).toFixed(2); }, "scale");

  retry.addEventListener("click", function () {
    overlayText.textContent = t("overlay.connecting");
    connect();
  });

  // ---------------------------------------------------------------- çubuğu gizle
  var hideTimer = null;

  function showToolbar() {
    if (coarse) return;
    toolbar.classList.remove("hidden");
    clearTimeout(hideTimer);
    hideTimer = setTimeout(function () {
      if (panel.classList.contains("hidden")) toolbar.classList.add("hidden");
    }, 3500);
  }

  document.addEventListener("mousemove", showToolbar);

  // ---------------------------------------------------------------- başlat
  applyLang();
  setKeyboard(false);
  setFit("contain");
  applyControlMode(controlMode);
  updateResolution();
  refreshStats();
  overlayText.textContent = t("overlay.connecting");
  connect();

  if (coarse) toolbar.classList.remove("hidden");
})();