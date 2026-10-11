using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Umbra
{
    /// <summary>
    /// Basit HTTP + WebSocket sunucusu. Yonetici hakki veya URL izni gerektirmez
    /// (HttpListener yerine ham TcpListener kullanir).
    /// </summary>
    internal sealed class WebServer
    {
        private static readonly Dictionary<string, string> Files =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "/", "index.html" },
                { "/index.html", "index.html" },
                { "/style.css", "style.css" },
                { "/app.js", "app.js" },
                { "/favicon.svg", "favicon.svg" }
            };

        private readonly StreamHub _hub;
        private readonly Action<ClientSession, string> _onMessage;

        private TcpListener _listener;
        private volatile bool _running;
        private Thread _acceptThread;

        public WebServer(StreamHub hub, Action<ClientSession, string> onMessage)
        {
            _hub = hub;
            _onMessage = onMessage;
        }

        public int Port { get; private set; }

        public void Start(int port)
        {
            Port = port;
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _running = true;

            _acceptThread = new Thread(AcceptLoop);
            _acceptThread.IsBackground = true;
            _acceptThread.Name = "umbra-http";
            _acceptThread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { if (_listener != null) _listener.Stop(); } catch { }
            _listener = null;
            _hub.CloseAll();
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                TcpClient client;
                try { client = _listener.AcceptTcpClient(); }
                catch { break; }

                var thread = new Thread(delegate() { HandleClient(client); });
                thread.IsBackground = true;
                thread.Start();
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                client.NoDelay = true;
                using (client)
                using (var stream = client.GetStream())
                {
                    string header = ReadHeaders(stream);
                    if (header == null) return;

                    string[] lines = header.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                    if (lines.Length == 0) return;

                    string[] parts = lines[0].Split(' ');
                    string method = parts.Length > 0 ? parts[0] : "GET";
                    string path = parts.Length > 1 ? parts[1] : "/";

                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        int colon = lines[i].IndexOf(':');
                        if (colon > 0)
                            headers[lines[i].Substring(0, colon).Trim()] = lines[i].Substring(colon + 1).Trim();
                    }

                    string upgrade;
                    if (headers.TryGetValue("Upgrade", out upgrade) &&
                        upgrade.IndexOf("websocket", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        HandleWebSocket(stream, headers);
                    }
                    else
                    {
                        ServeStatic(stream, method, path);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Istemci hatasi: " + ex.Message);
            }
        }

        private static string ReadHeaders(NetworkStream stream)
        {
            var buffer = new List<byte>(1024);
            var one = new byte[1];
            while (buffer.Count < 16384)
            {
                int read;
                try { read = stream.Read(one, 0, 1); }
                catch { return null; }
                if (read <= 0) return null;

                buffer.Add(one[0]);
                int n = buffer.Count;
                if (n >= 4 && buffer[n - 4] == 13 && buffer[n - 3] == 10 && buffer[n - 2] == 13 && buffer[n - 1] == 10)
                    break;
            }
            return Encoding.UTF8.GetString(buffer.ToArray());
        }

        private void HandleWebSocket(NetworkStream stream, Dictionary<string, string> headers)
        {
            string key;
            headers.TryGetValue("Sec-WebSocket-Key", out key);
            if (string.IsNullOrEmpty(key))
            {
                WriteResponse(stream, "400 Bad Request", "text/plain", Encoding.UTF8.GetBytes("websocket anahtari yok"));
                return;
            }

            string accept = Convert.ToBase64String(SHA1.Create().ComputeHash(
                Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));

            string response =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: " + accept + "\r\n\r\n";

            byte[] bytes = Encoding.ASCII.GetBytes(response);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();

            var connection = new WsConnection(stream);
            var session = new ClientSession(connection);
            _hub.Add(session);
            Log.Info("Istemci baglandi (" + session.Id + "). Aktif: " + _hub.Count);

            var pumpThread = new Thread(delegate() { session.Pump(); });
            pumpThread.IsBackground = true;
            pumpThread.Start();

            while (session.Alive)
            {
                byte[] payload = connection.ReadMessage();
                if (payload == null) break;
                if (payload.Length == 0) continue;

                string text = Encoding.UTF8.GetString(payload);
                var handler = _onMessage;
                if (handler != null) handler(session, text);
            }

            session.Alive = false;
            _hub.Remove(session);
            session.Close();
            try { pumpThread.Join(1500); } catch { }

            Log.Info("Istemci ayrildi (" + session.Id + "). Aktif: " + _hub.Count);
        }

        private void ServeStatic(NetworkStream stream, string method, string path)
        {
            int query = path.IndexOf('?');
            if (query >= 0) path = path.Substring(0, query);

            string file;
            if (!Files.TryGetValue(path, out file))
            {
                WriteResponse(stream, "404 Not Found", "text/plain; charset=utf-8", Encoding.UTF8.GetBytes("Bulunamadi"));
                return;
            }

            byte[] data = ResourceStore.LoadBytes("wwwroot." + file, Path.Combine("wwwroot", file));
            if (data == null)
            {
                WriteResponse(stream, "404 Not Found", "text/plain; charset=utf-8", new byte[0]);
                return;
            }

            WriteResponse(stream, "200 OK", ContentType(file), data);
        }

        private static string ContentType(string file)
        {
            if (file.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) return "text/html; charset=utf-8";
            if (file.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) return "text/css; charset=utf-8";
            if (file.EndsWith(".js", StringComparison.OrdinalIgnoreCase)) return "application/javascript; charset=utf-8";
            if (file.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) return "image/svg+xml";
            if (file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) return "application/json; charset=utf-8";
            if (file.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return "image/png";
            if (file.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) return "image/x-icon";
            return "application/octet-stream";
        }

        private static void WriteResponse(NetworkStream stream, string status, string contentType, byte[] body)
        {
            string head =
                "HTTP/1.1 " + status + "\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + body.Length + "\r\n" +
                "Cache-Control: no-store\r\n" +
                "Connection: close\r\n\r\n";

            byte[] headBytes = Encoding.ASCII.GetBytes(head);
            stream.Write(headBytes, 0, headBytes.Length);
            if (body.Length > 0) stream.Write(body, 0, body.Length);
            stream.Flush();
        }
    }
}
