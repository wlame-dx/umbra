// Umbra - kontrol kipi ve giris mesajlarinin host'u bozmadigini dogrular.
const ws = new WebSocket("ws://127.0.0.1:8777/");
ws.binaryType = "arraybuffer";
let hello = false, pong = false, frames = 0;

ws.onopen = () => {
  ws.send(JSON.stringify({ t: "mode", m: "mouse" }));
  ws.send(JSON.stringify({ t: "p", a: "down", x: 0.5, y: 0.5, b: 0, id: 1 }));
  ws.send(JSON.stringify({ t: "p", a: "move", x: 0.52, y: 0.5, b: 0, id: 1 }));
  ws.send(JSON.stringify({ t: "p", a: "up", x: 0.52, y: 0.5, b: 0, id: 1 }));
  ws.send(JSON.stringify({ t: "mode", m: "touch" }));
  ws.send(JSON.stringify({ t: "p", a: "down", x: 0.3, y: 0.4, b: 0, id: 2 }));
  ws.send(JSON.stringify({ t: "p", a: "up", x: 0.3, y: 0.4, b: 0, id: 2 }));
  ws.send(JSON.stringify({ t: "wheel", d: 120, x: 0.5, y: 0.5 }));
  ws.send(JSON.stringify({ t: "key", a: "down", code: "KeyA", key: "a" }));
  ws.send(JSON.stringify({ t: "key", a: "up", code: "KeyA", key: "a" }));
  ws.send(JSON.stringify({ t: "set", quality: 60, fps: 24, scale: 0.8 }));
  setTimeout(() => ws.send(JSON.stringify({ t: "ping", ts: Date.now() })), 800);
};

ws.onmessage = (ev) => {
  if (typeof ev.data === "string") {
    let m = null; try { m = JSON.parse(ev.data); } catch (e) {}
    if (m && m.t === "hello") hello = true;
    if (m && m.t === "pong") pong = true;
    return;
  }
  frames++;
};

setTimeout(() => {
  console.log("SONUC frames=" + frames + " hello=" + hello + " pong=" + pong);
  try { ws.close(); } catch (e) {}
  process.exit(frames > 10 && hello && pong ? 0 : 1);
}, 4000);
