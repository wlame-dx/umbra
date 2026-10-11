using System;
using System.Collections.Generic;
using System.Threading;

namespace Umbra
{
    internal sealed class ClientSession
    {
        private readonly WsConnection _connection;
        private readonly object _gate = new object();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private byte[] _pending;
        private bool _signaled;

        public readonly string Id;
        public volatile bool Alive = true;
        public long BytesSent;

        public ClientSession(WsConnection connection)
        {
            _connection = connection;
            Id = Guid.NewGuid().ToString("N").Substring(0, 6);
        }

        public WsConnection Connection { get { return _connection; } }

        /// <summary>En son kareyi kuyruga koyar. Yavas istemcilerde eski kareler atilir.</summary>
        public void QueueFrame(byte[] jpeg)
        {
            if (!Alive || jpeg == null) return;
            lock (_gate)
            {
                _pending = jpeg;
                if (!_signaled)
                {
                    _signaled = true;
                    _signal.Release();
                }
            }
        }

        public void Pump()
        {
            while (Alive)
            {
                try { _signal.Wait(); }
                catch { break; }
                if (!Alive) break;

                byte[] frame;
                lock (_gate)
                {
                    frame = _pending;
                    _pending = null;
                    _signaled = false;
                }
                if (frame == null) continue;

                _connection.WriteBinary(frame);
                BytesSent += frame.Length;
            }
            Alive = false;
        }

        public void SendText(string text)
        {
            if (!Alive) return;
            _connection.WriteText(text);
        }

        public void Close()
        {
            Alive = false;
            try { _signal.Release(); } catch { }
            _connection.Close();
        }
    }

    internal sealed class StreamHub
    {
        private readonly List<ClientSession> _clients = new List<ClientSession>();
        private readonly object _gate = new object();

        public event Action<ClientSession> Added;
        public event Action<ClientSession> Removed;

        public int Count { get { lock (_gate) return _clients.Count; } }

        public void Add(ClientSession session)
        {
            lock (_gate) _clients.Add(session);
            var handler = Added;
            if (handler != null) handler(session);
        }

        public void Remove(ClientSession session)
        {
            lock (_gate) _clients.Remove(session);
            var handler = Removed;
            if (handler != null) handler(session);
        }

        public void Broadcast(byte[] frame)
        {
            ClientSession[] snapshot;
            lock (_gate) snapshot = _clients.ToArray();
            foreach (var client in snapshot) client.QueueFrame(frame);
        }

        public void BroadcastText(string text)
        {
            ClientSession[] snapshot;
            lock (_gate) snapshot = _clients.ToArray();
            foreach (var client in snapshot) client.SendText(text);
        }

        public void CloseAll()
        {
            ClientSession[] snapshot;
            lock (_gate) snapshot = _clients.ToArray();
            foreach (var client in snapshot) client.Close();
        }
    }
}
