using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Umbra
{
    internal sealed class MonitorInfo
    {
        public string Device;
        public string Name;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public bool Primary;

        public override string ToString()
        {
            return Name + "   " + Width + "x" + Height + (Primary ? "   " + I18n.T("screen.primary") : "");
        }
    }

    internal static class MonitorManager
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetMonitorInfoW")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX info);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "EnumDisplaySettingsW")]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devmode);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "ChangeDisplaySettingsExW")]
        private static extern int ChangeDisplaySettingsEx(string deviceName, ref DEVMODE devmode, IntPtr hwnd, uint flags, IntPtr param);

        private const uint MONITORINFOF_PRIMARY = 1;
        private const uint CDS_UPDATEREGISTRY = 0x00000001;
        private const int DM_PELSWIDTH = 0x00080000;
        private const int DM_PELSHEIGHT = 0x00100000;

        public static List<MonitorInfo> Enumerate()
        {
            var list = new List<MonitorInfo>();
            int index = 1;

            MonitorEnumProc proc = delegate(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data)
            {
                var info = new MONITORINFOEX();
                info.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
                if (GetMonitorInfo(hMonitor, ref info))
                {
                    var monitor = new MonitorInfo();
                    monitor.Device = info.szDevice;
                    monitor.X = rect.Left;
                    monitor.Y = rect.Top;
                    monitor.Width = rect.Right - rect.Left;
                    monitor.Height = rect.Bottom - rect.Top;
                    monitor.Primary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;
                    monitor.Name = I18n.F("screen.monitor", index);
                    list.Add(monitor);
                    index++;
                }
                return true;
            };

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, proc, IntPtr.Zero);
            return list;
        }

        public static MonitorInfo Find(string device)
        {
            foreach (var monitor in Enumerate())
                if (string.Equals(monitor.Device, device, StringComparison.OrdinalIgnoreCase)) return monitor;
            return null;
        }

        public static MonitorInfo Primary()
        {
            var all = Enumerate();
            foreach (var monitor in all) if (monitor.Primary) return monitor;
            return all.Count > 0 ? all[0] : null;
        }

        public static List<string> GetModes(string deviceName)
        {
            var seen = new List<string>();
            var result = new List<string>();
            var mode = new DEVMODE();

            int i = 0;
            while (true)
            {
                mode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                if (!EnumDisplaySettings(deviceName, i, ref mode)) break;

                string key = mode.dmPelsWidth + "x" + mode.dmPelsHeight + "x" + mode.dmDisplayFrequency;
                if (!seen.Contains(key))
                {
                    seen.Add(key);
                    result.Add(mode.dmPelsWidth + " x " + mode.dmPelsHeight + "   " + mode.dmDisplayFrequency + " Hz");
                }
                i++;
            }
            return result;
        }

        public static bool SetMode(string deviceName, int width, int height)
        {
            var mode = new DEVMODE();
            int i = 0;
            while (true)
            {
                mode.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
                if (!EnumDisplaySettings(deviceName, i, ref mode)) break;

                if (mode.dmPelsWidth == width && mode.dmPelsHeight == height)
                {
                    mode.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT;
                    int code = ChangeDisplaySettingsEx(deviceName, ref mode, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
                    return code == 0; // DISP_CHANGE_SUCCESSFUL
                }
                i++;
            }
            return false;
        }
    }
}
