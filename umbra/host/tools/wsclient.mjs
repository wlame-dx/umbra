// Umbra WebSocket istemci testi (Node'un yerlesik WebSocket'i ile)
const port = process.argv[2] || "8799";
const ws = new WebSocket("ws://127.0.0.1:" + port + "/");
ws.binaryType = "arraybuffer";

let frames = 0;
let bytes = 0;
let hello = false;
let pong = false;

const timeout = setTimeout(() => {
  console.log("SONUC frames=" + frames + " bytes=" + bytes + " hello=" + hello + " pong=" + pong);
  try { ws.close(); } catch (e) {}
  process.exit(frames > 5 && hello && pong ? 0 : 1);
}, 4000);

ws.onopen = () => {
  console.log("acildi");
  ws.send(JSON.stringify({ t: "p", a: "down", x: 0.5, y: 0.5, b: 0, id: 1 }));
};

ws.onmessage = (ev) => {
  if (typeof ev.data === "string") {
    let msg = null;
    try { msg = JSON.parse(ev.data); } catch (e) {}
    if (msg && msg.t === "hello") { hello = true; console.log("hello w=" + msg.w + " h=" + msg.h); }
    if (msg && msg.t === "pong") { pong = true; console.log("pong"); }
    return;
  }
  frames++;
  bytes += ev.data.byteLength;
  if (frames === 1) console.log("ilk kare bayt=" + ev.data.byteLength);
};

ws.onerror = (e) => console.log("hata: " + (e.message || e.type));
ws.onclose = () => console.log("kapandi");
