using System;
using System.Threading;
using Umbra;

// WebServer + WsConnection icin kucuk bir dogrulama programi.
internal static class NetTest
{
    private static int _frames;

    private static void Main(string[] args)
    {
        Log.Init();
        Log.Line += delegate(string line) { Console.WriteLine("[log] " + line); };

        var hub = new StreamHub();
        hub.Added += delegate(ClientSession session)
        {
            session.SendText("{\"t\":\"hello\",\"app\":\"Umbra\",\"w\":1280,\"h\":720}");
        };

        var server = new WebServer(hub, delegate(ClientSession session, string text)
        {
            Console.WriteLine("[gelen] " + text);
            session.SendText("{\"t\":\"pong\",\"ts\":1}");
        });

        int port = 8799;
        if (args.Length > 0) int.TryParse(args[0], out port);

        server.Start(port);
        Console.WriteLine("dinleniyor: " + port);

        var timer = new Timer(delegate
        {
            var frame = new byte[2048];
            for (int i = 0; i < frame.Length; i++) frame[i] = (byte)(i & 0xFF);
            hub.Broadcast(frame);
            _frames++;
        }, null, 200, 100);

        Thread.Sleep(12000);
        timer.Dispose();
        server.Stop();
        Console.WriteLine("gonderilen kare: " + _frames);
        Environment.Exit(0);
    }
}
