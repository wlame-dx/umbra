// Umbra uctan uca test: gercek uygulamaya baglanir.
// Fare/tiklama gondermez; yalnizca ping atar.
const ws = new WebSocket("ws://127.0.0.1:8777/");
ws.binaryType = "arraybuffer";

let frames = 0;
let bytes = 0;
let hello = false;
let pong = false;
let first = false;
const t0 = Date.now();

ws.onopen = () => {
  console.log("acildi");
  ws.send(JSON.stringify({ t: "ping", ts: Date.now() }));
};

ws.onmessage = (ev) => {
  if (typeof ev.data === "string") {
    let m = null;
    try { m = JSON.parse(ev.data); } catch (e) {}
    if (m && m.t === "hello") { hello = true; console.log("hello w=" + m.w + " h=" + m.h); }
    if (m && m.t === "pong") { pong = true; console.log("pong gecikme=" + (Date.now() - m.ts) + "ms"); }
    return;
  }
  frames++;
  bytes += ev.data.byteLength;
  if (!first) { first = true; console.log("ilk kare: " + ev.data.byteLength + " bayt, bekleme=" + (Date.now() - t0) + "ms"); }
};

ws.onerror = (e) => console.log("hata: " + (e.message || e.type));

setTimeout(() => {
  const dt = (Date.now() - t0) / 1000;
  console.log("SONUC frames=" + frames + " fps=" + (frames / dt).toFixed(1) + " bytes=" + bytes
    + " hello=" + hello + " pong=" + pong);
  try { ws.close(); } catch (e) {}
  process.exit(frames > 30 && hello && pong ? 0 : 1);
}, 6000);