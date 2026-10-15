using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Umbra
{
    /// <summary>
    /// Yerel agdaki istemcilerin Umbra'yi bulabilmesi icin UDP kesif yanitlayicisi.
    ///
    /// Istemci 255.255.255.255:8778 adresine <c>{"umbra":"discover"}</c> yollar;
    /// yayin acikken Umbra ayni soketten gonderenin adresine kendi bilgileriyle
    /// (ad, port, isletim sistemi) yanit verir. Yayin kapaliyken hic yanit vermez.
    /// </summary>
    internal sealed class DiscoveryResponder
    {
        /// <summary>Kesif icin ayrilan sabit UDP portu.</summary>
        public const int DiscoveryPort = 8778;

        private readonly Func<string> _payload;
        private UdpClient _client;
        private Thread _thread;
        private volatile bool _running;

        public DiscoveryResponder(Func<string> payload)
        {
            _payload = payload;
        }

        public void Start()
        {
            if (_running) return;
            try
            {
                var client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
                client.EnableBroadcast = true;

                _client = client;
                _running = true;

                _thread = new Thread(Loop);
                _thread.IsBackground = true;
                _thread.Name = "umbra-discovery";
                _thread.Start();
            }
            catch (Exception ex)
            {
                Log.Warn("Kesif dinleyicisi acilamadi: " + ex.Message);
                Stop();
            }
        }

        public void Stop()
        {
            _running = false;
            var client = _client;
            _client = null;
            try { if (client != null) client.Close(); } catch { }
        }

        private void Loop()
        {
            var remote = new IPEndPoint(IPAddress.Any, 0);
            while (_running)
            {
                try
                {
                    byte[] data = _client.Receive(ref remote);
                    if (data == null || data.Length == 0) continue;

                    string text = Encoding.UTF8.GetString(data);
                    if (text.IndexOf("\"discover\"", StringComparison.Ordinal) < 0) continue;

                    var provider = _payload;
                    string payload = provider == null ? null : provider();
                    if (string.IsNullOrEmpty(payload)) continue;

                    byte[] reply = Encoding.UTF8.GetBytes(payload);
                    _client.Send(reply, reply.Length, remote);
                }
                catch
                {
                    if (!_running) break;
                    Thread.Sleep(50);
                }
            }
        }
    }
}
