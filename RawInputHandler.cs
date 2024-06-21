using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace AssignShortcuts
{
    public class RawInputHandler
    {
        public event Action<string, string, string> DeviceInputReceived;

        public void StartListening(IntPtr windowHandle)
        {
            var rid = new RawInputDevice[2];
            rid[0].UsagePage = 0x01;
            rid[0].Usage = 0x02; // Mouse
            rid[0].Flags = RawInputDeviceFlags.INPUTSINK;
            rid[0].Target = windowHandle;

            rid[1].UsagePage = 0x01;
            rid[1].Usage = 0x06; // Keyboard
            rid[1].Flags = RawInputDeviceFlags.INPUTSINK;
            rid[1].Target = windowHandle;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new Exception("Failed to register raw input devices.");
            }
        }

        public void StopListening()
        {
            var rid = new RawInputDevice[2];
            rid[0].UsagePage = 0x01;
            rid[0].Usage = 0x02; // Mouse
            rid[0].Flags = RawInputDeviceFlags.REMOVE;
            rid[0].Target = IntPtr.Zero;

            rid[1].UsagePage = 0x01;
            rid[1].Usage = 0x06; // Keyboard
            rid[1].Flags = RawInputDeviceFlags.REMOVE;
            rid[1].Target = IntPtr.Zero;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new Exception("Failed to unregister raw input devices.");
            }
        }

        public void ProcessInput(IntPtr lParam)
        {
            uint dwSize = 0;
            GetRawInputData(lParam, RawInputCommand.INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RawInputHeader)));
            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            if (buffer == IntPtr.Zero)
            {
                throw new Exception("Failed to allocate memory.");
            }

            try
            {
                if (GetRawInputData(lParam, RawInputCommand.INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RawInputHeader))) != dwSize)
                {
                    throw new Exception("Failed to get raw input data.");
                }

                var raw = (RawInput)Marshal.PtrToStructure(buffer, typeof(RawInput));
                string deviceType = raw.header.dwType == RawInputType.MOUSE ? "Mouse" : "Keyboard";
                string deviceName = GetDeviceName(raw.header.hDevice);
                DeviceInputReceived?.Invoke(deviceName, deviceType, raw.header.hDevice.ToString());
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private string GetDeviceName(IntPtr device)
        {
            uint pcbSize = 0;
            GetRawInputDeviceInfo(device, RawInputDeviceInfo.COMMAND, IntPtr.Zero, ref pcbSize);
            if (pcbSize <= 0)
            {
                return null;
            }

            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
            if (pData == IntPtr.Zero)
            {
                throw new Exception("Failed to allocate memory.");
            }

            try
            {
                if (GetRawInputDeviceInfo(device, RawInputDeviceInfo.COMMAND, pData, ref pcbSize) != pcbSize)
                {
                    throw new Exception("Failed to get raw input device info.");
                }

                return Marshal.PtrToStringAnsi(pData);
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }
        }

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, RawInputCommand command, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint command, IntPtr pData, ref uint pcbSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RegisterRawInputDevices([In] RawInputDevice[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        private const uint RID_INPUT = 0x10000003;

        private struct RawInputDevice
        {
            public ushort UsagePage;
            public ushort Usage;
            public RawInputDeviceFlags Flags;
            public IntPtr Target;
        }

        [Flags]
        private enum RawInputDeviceFlags
        {
            REMOVE = 0x00000001,
            EXCLUDE = 0x00000010,
            PAGEONLY = 0x00000020,
            NOLEGACY = 0x00000030,
            INPUTSINK = 0x00000100,
            CAPTUREMOUSE = 0x00000200,
            NOHOTKEYS = 0x00000200,
            APPKEYS = 0x00000400,
            EXINPUTSINK = 0x00001000,
            DEVNOTIFY = 0x00002000
        }

        private enum RawInputCommand
        {
            INPUT = 0x10000003
        }

        private struct RawInputHeader
        {
            public RawInputType dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        private struct RawMouse
        {
            public ushort usFlags;
            public uint ulButtons;
            public ushort usButtonFlags;
            public ushort usButtonData;
            public uint ulRawButtons;
            public int lLastX;
            public int lLastY;
            public uint ulExtraInformation;
        }

        private struct RawKeyboard
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        private struct RawInput
        {
            public RawInputHeader header;
            public Union data;

            [StructLayout(LayoutKind.Explicit)]
            public struct Union
            {
                [FieldOffset(0)]
                public RawMouse mouse;
                [FieldOffset(0)]
                public RawKeyboard keyboard;
            }
        }

        private enum RawInputType
        {
            MOUSE = 0,
            KEYBOARD = 1,
            HID = 2
        }

        private static class RawInputDeviceInfo
        {
            public const uint COMMAND = 0x20000007;
        }
    }
}
