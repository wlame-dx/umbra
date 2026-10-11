namespace Umbra
{
    /// <summary>Yayin ayarlari. Arayuz ve yayin dongusu tarafindan paylasilir.</summary>
    internal sealed class HostSettings
    {
        public string MonitorDevice = "";
        public int Port = 8777;
        public int Fps = 30;
        public int Quality = 72;
        public double Scale = 1.0;
        public bool AllowInput = true;

        // "touch" -> coklu dokunma enjeksiyonu, "mouse" -> fare imleci
        public string InputMode = "touch";

        // Yayin baslarken sanal ekrani olustur, dururken kaldir
        public bool AutoVd = true;
    }
}
