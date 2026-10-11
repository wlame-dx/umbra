using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Umbra
{
    /// <summary>
    /// Uzak cihazdan gelen olaylari Windows'a uygular.
    /// Fare (tek isaretci) ve coklu dokunma enjeksiyonunu destekler.
    /// </summary>
    internal sealed class InputInjector
    {
        // ------------------------------------------------------------------
        //  SendInput
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg; public ushort wParamL; public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mi;
            [FieldOffset(0)] public KEYBDINPUT ki;
            [FieldOffset(0)] public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type; public INPUTUNION u;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint count, INPUT[] inputs, int size);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern short VkKeyScan(char ch);

        // ------------------------------------------------------------------
        //  Dokunma enjeksiyonu
        // ------------------------------------------------------------------
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int left; public int top; public int right; public int bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTER_INFO
        {
            public uint pointerType;
            public uint pointerId;
            public uint frameId;
            public uint pointerFlags;
            public IntPtr sourceDevice;
            public IntPtr hwndTarget;
            public POINT ptPixelLocation;
            public POINT ptHimetricLocation;
            public POINT ptPixelLocationRaw;
            public POINT ptHimetricLocationRaw;
            public uint dwTime;
            public uint historyCount;
            public int inputData;
            public uint dwKeyStates;
            public ulong performanceCount;
            public uint buttonChangeType;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINTER_TOUCH_INFO
        {
            public POINTER_INFO pointerInfo;
            public uint touchFlags;
            public uint touchMask;
            public RECT rcContact;
            public RECT rcContactRaw;
            public uint orientation;
            public uint pressure;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InitializeTouchInjection(uint maxCount, uint mode);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool InjectTouchInput(uint count, POINTER_TOUCH_INFO[] contacts);

        // ------------------------------------------------------------------
        //  Sabitler
        // ------------------------------------------------------------------
        private const uint INPUT_MOUSE = 0;
        private const uint INPUT_KEYBOARD = 1;

        private const uint MOUSEEVENTF_MOVE = 0x0001;
        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        private const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const ushort VK_SHIFT = 0x10;

        private const uint TOUCH_FEEDBACK_DEFAULT = 1;
        private const uint PT_TOUCH = 2;
        private const uint POINTER_FLAG_INCONTACT = 0x00000001;
        private const uint POINTER_FLAG_INRANGE = 0x00000002;
        private const uint POINTER_FLAG_PRIMARY = 0x00002000;
        private const uint POINTER_FLAG_DOWN = 0x00010000;
        private const uint POINTER_FLAG_UPDATE = 0x00020000;
        private const uint POINTER_FLAG_UP = 0x00040000;
        private const uint TOUCH_MASK_CONTACTAREA = 0x00000001;

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        // ------------------------------------------------------------------
        //  Durum
        // ------------------------------------------------------------------
        private readonly Dictionary<int, POINT> _contacts = new Dictionary<int, POINT>();
        private bool _touchReady;
        private bool _touchUnavailable;
        private uint _frameId;

        private static readonly Dictionary<string, ushort> SpecialKeys = BuildSpecialKeys();
        private static readonly Dictionary<string, ushort> NumpadKeys = BuildNumpadKeys();

        // ==================================================================
        //  Fare
        // ==================================================================

        public void Move(double nx, double ny, MonitorInfo monitor)
        {
            int px, py, ax, ay;
            MapPixel(nx, ny, monitor, out px, out py);
            ToAbsolute(px, py, out ax, out ay);
            SendMouse(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, ax, ay, 0);
        }

        public void Button(bool down, int button, double nx, double ny, MonitorInfo monitor)
        {
            int px, py, ax, ay;
            MapPixel(nx, ny, monitor, out px, out py);
            ToAbsolute(px, py, out ax, out ay);

            uint flag;
            if (button == 1) flag = down ? MOUSEEVENTF_RIGHTDOWN : MOUSEEVENTF_RIGHTUP;
            else if (button == 2) flag = down ? MOUSEEVENTF_MIDDLEDOWN : MOUSEEVENTF_MIDDLEUP;
            else flag = down ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_LEFTUP;

            SendMouse(flag | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, ax, ay, 0);
        }

        public void Wheel(int delta, double nx, double ny, MonitorInfo monitor)
        {
            int px, py, ax, ay;
            MapPixel(nx, ny, monitor, out px, out py);
            ToAbsolute(px, py, out ax, out ay);
            SendMouse(MOUSEEVENTF_WHEEL | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, ax, ay, unchecked((uint)delta));
        }

        // ==================================================================
        //  Klavye
        // ==================================================================

        public void Key(bool down, string code, string key)
        {
            ushort vk;
            bool shift;
            if (!ResolveVirtualKey(code, key, out vk, out shift)) return;

            var inputs = new List<INPUT>();
            if (down && shift) inputs.Add(KeyboardInput(VK_SHIFT, false));
            inputs.Add(KeyboardInput(vk, !down));
            if (!down && shift) inputs.Add(KeyboardInput(VK_SHIFT, true));

            var array = inputs.ToArray();
            SendInput((uint)array.Length, array, Marshal.SizeOf(typeof(INPUT)));
        }

        // ==================================================================
        //  Dokunma
        // ==================================================================

        public bool TouchPointer(MonitorInfo monitor, int id, double nx, double ny, string action)
        {
            EnsureTouch();
            if (!_touchReady) return false;

            int px, py;
            MapPixel(nx, ny, monitor, out px, out py);

            var point = new POINT();
            point.x = px;
            point.y = py;

            uint changed;
            if (action == "down")
            {
                _contacts[id] = point;
                changed = POINTER_FLAG_DOWN | POINTER_FLAG_INRANGE | POINTER_FLAG_INCONTACT;
            }
            else if (action == "up")
            {
                changed = POINTER_FLAG_UP;
            }
            else
            {
                _contacts[id] = point;
                changed = POINTER_FLAG_UPDATE | POINTER_FLAG_INRANGE | POINTER_FLAG_INCONTACT;
            }

            uint frame = ++_frameId;
            var list = new List<POINTER_TOUCH_INFO>();
            bool primaryAssigned = false;

            foreach (var entry in _contacts)
            {
                var contact = new POINTER_TOUCH_INFO();
                contact.pointerInfo.pointerType = PT_TOUCH;
                contact.pointerInfo.pointerId = (uint)entry.Key;
                contact.pointerInfo.frameId = frame;
                contact.pointerInfo.ptPixelLocation.x = entry.Value.x;
                contact.pointerInfo.ptPixelLocation.y = entry.Value.y;
                contact.pointerInfo.ptPixelLocationRaw = contact.pointerInfo.ptPixelLocation;
                contact.pointerInfo.ptHimetricLocation = contact.pointerInfo.ptPixelLocation;
                contact.pointerInfo.ptHimetricLocationRaw = contact.pointerInfo.ptPixelLocation;

                uint flags = (entry.Key == id)
                    ? changed
                    : (POINTER_FLAG_UPDATE | POINTER_FLAG_INRANGE | POINTER_FLAG_INCONTACT);
                if (!primaryAssigned) { flags |= POINTER_FLAG_PRIMARY; primaryAssigned = true; }
                contact.pointerInfo.pointerFlags = flags;

                contact.touchMask = TOUCH_MASK_CONTACTAREA;
                contact.rcContact.left = px - 4;
                contact.rcContact.top = py - 4;
                contact.rcContact.right = px + 4;
                contact.rcContact.bottom = py + 4;
                contact.rcContactRaw = contact.rcContact;
                contact.pressure = 32000;

                list.Add(contact);
            }

            var array = list.ToArray();
            bool ok = InjectTouchInput((uint)array.Length, array);

            if (action == "up") _contacts.Remove(id);
            return ok;
        }

        public void ReleaseAll()
        {
            if (!_touchReady || _contacts.Count == 0) return;
            foreach (var id in new List<int>(_contacts.Keys))
            {
                int px = _contacts[id].x, py = _contacts[id].y;
                var contact = new POINTER_TOUCH_INFO();
                contact.pointerInfo.pointerType = PT_TOUCH;
                contact.pointerInfo.pointerId = (uint)id;
                contact.pointerInfo.frameId = ++_frameId;
                contact.pointerInfo.pointerFlags = POINTER_FLAG_UP | POINTER_FLAG_PRIMARY;
                contact.pointerInfo.ptPixelLocation.x = px;
                contact.pointerInfo.ptPixelLocation.y = py;
                contact.pointerInfo.ptPixelLocationRaw = contact.pointerInfo.ptPixelLocation;
                contact.pointerInfo.ptHimetricLocation = contact.pointerInfo.ptPixelLocation;
                contact.pointerInfo.ptHimetricLocationRaw = contact.pointerInfo.ptPixelLocation;
                contact.touchMask = TOUCH_MASK_CONTACTAREA;
                contact.rcContact.left = px - 4; contact.rcContact.top = py - 4;
                contact.rcContact.right = px + 4; contact.rcContact.bottom = py + 4;
                contact.rcContactRaw = contact.rcContact;
                contact.pressure = 32000;
                InjectTouchInput(1, new[] { contact });
            }
            _contacts.Clear();
        }

        private void EnsureTouch()
        {
            if (_touchReady || _touchUnavailable) return;
            if (InitializeTouchInjection(10, TOUCH_FEEDBACK_DEFAULT)) _touchReady = true;
            else _touchUnavailable = true;
        }

        /// <summary>Coklu dokunma enjeksiyonu kullanilabilir mi?</summary>
        public bool CanTouch
        {
            get { EnsureTouch(); return _touchReady; }
        }

        // ==================================================================
        //  Yardimcilar
        // ==================================================================

        private static void MapPixel(double nx, double ny, MonitorInfo monitor, out int px, out int py)
        {
            if (nx < 0) nx = 0; if (nx > 1) nx = 1;
            if (ny < 0) ny = 0; if (ny > 1) ny = 1;
            px = monitor.X + (int)Math.Round(nx * (monitor.Width - 1));
            py = monitor.Y + (int)Math.Round(ny * (monitor.Height - 1));
        }

        private static void ToAbsolute(int px, int py, out int ax, out int ay)
        {
            int vx = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int vy = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int vw = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int vh = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            if (vw < 2) vw = 2;
            if (vh < 2) vh = 2;

            ax = (int)Math.Round((px - vx) * 65535.0 / (vw - 1));
            ay = (int)Math.Round((py - vy) * 65535.0 / (vh - 1));
            if (ax < 0) ax = 0; if (ax > 65535) ax = 65535;
            if (ay < 0) ay = 0; if (ay > 65535) ay = 65535;
        }

        private static void SendMouse(uint flags, int dx, int dy, uint data)
        {
            var inputs = new INPUT[1];
            inputs[0].type = INPUT_MOUSE;
            inputs[0].u.mi.dx = dx;
            inputs[0].u.mi.dy = dy;
            inputs[0].u.mi.mouseData = data;
            inputs[0].u.mi.dwFlags = flags;
            inputs[0].u.mi.dwExtraInfo = IntPtr.Zero;
            SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT)));
        }

        private static INPUT KeyboardInput(ushort vk, bool keyUp)
        {
            var input = new INPUT();
            input.type = INPUT_KEYBOARD;
            input.u.ki.wVk = vk;
            input.u.ki.dwFlags = keyUp ? KEYEVENTF_KEYUP : 0;
            return input;
        }

        private static bool ResolveVirtualKey(string code, string key, out ushort vk, out bool shift)
        {
            vk = 0;
            shift = false;

            if (!string.IsNullOrEmpty(code))
            {
                if (code.Length == 4 && code.StartsWith("Key", StringComparison.Ordinal))
                {
                    char c = code[3];
                    if (c >= 'A' && c <= 'Z') { vk = (ushort)c; return true; }
                }
                if (code.Length == 6 && code.StartsWith("Digit", StringComparison.Ordinal))
                {
                    char c = code[5];
                    if (c >= '0' && c <= '9') { vk = (ushort)c; return true; }
                }
                if (code.Length > 6 && code.StartsWith("Numpad", StringComparison.Ordinal))
                {
                    ushort numpad;
                    if (NumpadKeys.TryGetValue(code.Substring(6), out numpad)) { vk = numpad; return true; }
                }
                ushort special;
                if (SpecialKeys.TryGetValue(code, out special)) { vk = special; return true; }
            }

            if (!string.IsNullOrEmpty(key) && key.Length == 1)
            {
                short scan = VkKeyScan(key[0]);
                if (scan != -1)
                {
                    vk = (ushort)(scan & 0xFF);
                    shift = (scan & 0x0100) != 0;
                    return true;
                }
            }
            return false;
        }

        private static Dictionary<string, ushort> BuildSpecialKeys()
        {
            var d = new Dictionary<string, ushort>(StringComparer.Ordinal);
            d["Enter"] = 0x0D; d["NumpadEnter"] = 0x0D;
            d["Escape"] = 0x1B; d["Backspace"] = 0x08; d["Tab"] = 0x09; d["Space"] = 0x20;
            d["ArrowLeft"] = 0x25; d["ArrowUp"] = 0x26; d["ArrowRight"] = 0x27; d["ArrowDown"] = 0x28;
            d["Home"] = 0x24; d["End"] = 0x23; d["PageUp"] = 0x21; d["PageDown"] = 0x22;
            d["Insert"] = 0x2D; d["Delete"] = 0x2E;

            d["ShiftLeft"] = 0xA0; d["ShiftRight"] = 0xA1;
            d["ControlLeft"] = 0xA2; d["ControlRight"] = 0xA3;
            d["AltLeft"] = 0xA4; d["AltRight"] = 0xA5;
            d["MetaLeft"] = 0x5B; d["MetaRight"] = 0x5C;

            d["CapsLock"] = 0x14; d["PrintScreen"] = 0x2C; d["ScrollLock"] = 0x91; d["Pause"] = 0x13;
            d["Minus"] = 0xBD; d["Equal"] = 0xBB;
            d["BracketLeft"] = 0xDB; d["BracketRight"] = 0xDD;
            d["Backslash"] = 0xDC; d["Semicolon"] = 0xBA; d["Quote"] = 0xDE; d["Backquote"] = 0xC0;
            d["Comma"] = 0xBC; d["Period"] = 0xBE; d["Slash"] = 0xBF;

            for (int i = 1; i <= 12; i++) d["F" + i] = (ushort)(0x70 + (i - 1));
            return d;
        }

        private static Dictionary<string, ushort> BuildNumpadKeys()
        {
            var d = new Dictionary<string, ushort>(StringComparer.Ordinal);
            d["0"] = 0x60; d["1"] = 0x61; d["2"] = 0x62; d["3"] = 0x63; d["4"] = 0x64;
            d["5"] = 0x65; d["6"] = 0x66; d["7"] = 0x67; d["8"] = 0x68; d["9"] = 0x69;
            d["Add"] = 0x6B; d["Subtract"] = 0x6D; d["Multiply"] = 0x6A; d["Divide"] = 0x6F;
            d["Decimal"] = 0x6E;
            return d;
        }
    }
}
