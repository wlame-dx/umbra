using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Umbra
{
    /// <summary>
    /// RFC 6455 WebSocket cercevelemesi. .NET Framework'te WebSocket.CreateFromStream
    /// bulunmadigi icin istemci cercevelerini kendimiz cozumluyoruz.
    /// </summary>
    internal sealed class WsConnection
    {
        private const int OP_CONT = 0x0;
        private const int OP_TEXT = 0x1;
        private const int OP_BINARY = 0x2;
        private const int OP_CLOSE = 0x8;
        private const int OP_PING = 0x9;
        private const int OP_PONG = 0xA;

        private readonly Stream _stream;
        private readonly object _writeGate = new object();

        public WsConnection(Stream stream)
        {
            _stream = stream;
        }

        private bool ReadExact(byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read;
                try { read = _stream.Read(buffer, offset, count - offset); }
                catch { return false; }
                if (read <= 0) return false;
                offset += read;
            }
            return true;
        }

        /// <summary>Bir tam mesaji okur. Baglanti kapandiginda null doner.</summary>
        public byte[] ReadMessage()
        {
            var message = new MemoryStream();

            while (true)
            {
                var header = new byte[2];
                if (!ReadExact(header, 2)) return null;

                int opcode = header[0] & 0x0F;
                bool fin = (header[0] & 0x80) != 0;
                bool masked = (header[1] & 0x80) != 0;
                long length = header[1] & 0x7F;

                if (length == 126)
                {
                    var ext = new byte[2];
                    if (!ReadExact(ext, 2)) return null;
                    length = (ext[0] << 8) | ext[1];
                }
                else if (length == 127)
                {
                    var ext = new byte[8];
                    if (!ReadExact(ext, 8)) return null;
                    length = 0;
                    for (int i = 0; i < 8; i++) length = (length << 8) | ext[i];
                }

                if (length < 0 || length > 32L * 1024 * 1024) return null;

                var mask = new byte[4];
                if (masked && !ReadExact(mask, 4)) return null;

                var payload = new byte[(int)length];
                if (length > 0 && !ReadExact(payload, (int)length)) return null;

                if (masked)
                {
                    for (int i = 0; i < payload.Length; i++)
                        payload[i] = (byte)(payload[i] ^ mask[i % 4]);
                }

                if (opcode == OP_CLOSE) return null;
                if (opcode == OP_PING) { WriteFrame(payload, OP_PONG); continue; }
                if (opcode == OP_PONG) continue;

                if (opcode == OP_TEXT || opcode == OP_BINARY || opcode == OP_CONT)
                {
                    message.Write(payload, 0, payload.Length);
                    if (fin) return message.ToArray();
                }
            }
        }

        public void WriteFrame(byte[] payload, int opcode)
        {
            if (payload == null) payload = new byte[0];

            lock (_writeGate)
            {
                try
                {
                    var header = new List<byte>(10);
                    header.Add((byte)(0x80 | (opcode & 0x0F)));

                    if (payload.Length < 126)
                    {
                        header.Add((byte)payload.Length);
                    }
                    else if (payload.Length <= 0xFFFF)
                    {
                        header.Add(126);
                        header.Add((byte)((payload.Length >> 8) & 0xFF));
                        header.Add((byte)(payload.Length & 0xFF));
                    }
                    else
                    {
                        header.Add(127);
                        long length = payload.Length;
                        for (int i = 7; i >= 0; i--) header.Add((byte)((length >> (8 * i)) & 0xFF));
                    }

                    _stream.Write(header.ToArray(), 0, header.Count);
                    if (payload.Length > 0) _stream.Write(payload, 0, payload.Length);
                    _stream.Flush();
                }
                catch { }
            }
        }

        public void WriteText(string text)
        {
            WriteFrame(Encoding.UTF8.GetBytes(text), OP_TEXT);
        }

        public void WriteBinary(byte[] payload)
        {
            WriteFrame(payload, OP_BINARY);
        }

        public void Close()
        {
            // 1000 - normal kapanis
            WriteFrame(new byte[] { 0x03, 0xE8 }, OP_CLOSE);
            try { _stream.Flush(); } catch { }
        }

        /// <summary>Bekleyen okumayi da serbest birakmak icin alt soketi kapatir.</summary>
        public void Abort()
        {
            try { _stream.Close(); } catch { }
        }
    }
}
