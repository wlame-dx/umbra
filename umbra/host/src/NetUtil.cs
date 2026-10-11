using System;
using System.Net;
using System.Net.Sockets;

namespace Umbra
{
    internal static class NetUtil
    {
        /// <summary>Yerel ag arayuzunun IPv4 adresini bulur (paket gondermez).</summary>
        public static string GetLanIPv4()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect("8.8.8.8", 65530);
                    var endpoint = socket.LocalEndPoint as IPEndPoint;
                    if (endpoint != null) return endpoint.Address.ToString();
                }
            }
            catch { }

            try
            {
                var addresses = Dns.GetHostAddresses(Dns.GetHostName());
                foreach (var address in addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                        return address.ToString();
                }
            }
            catch { }

            return "127.0.0.1";
        }
    }
}
