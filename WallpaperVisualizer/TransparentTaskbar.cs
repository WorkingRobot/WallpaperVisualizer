using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WallpaperVisualizer {
    public class TransparentTaskbar
    {
        struct WINCOMPATTRDATA
        {
            public uint attribute; // the attribute to query, see below
            public IntPtr pData; // buffer to store the result
            public ulong dataSize; // size of the pData buffer

            public WINCOMPATTRDATA(uint attribute, IntPtr pData, ulong dataSize)
            {
                this.attribute = attribute;
                this.pData = pData;
                this.dataSize = dataSize;
            }
        }
        struct ACCENTPOLICY
        {
            public int nAccentState;
            public int nFlags;
            public int nColor;
            public int nAnimationId;

            public ACCENTPOLICY(int nAccentState, int nFlags, int nColor, int nAnimationId)
            {
                this.nAccentState = nAccentState;
                this.nFlags = nFlags;
                this.nColor = nColor;
                this.nAnimationId = nAnimationId;
            }
        }
        public struct TASKBARPROPERTIES
        {
            public IntPtr hmon;
            public TASKBARSTATE state;
        }
        public enum TASKBARSTATE { Normal, WindowMaximised, StartMenuOpen };

        [DllImport("user32")]
        private unsafe static extern bool SetWindowCompositionAttribute(IntPtr hwnd, WINCOMPATTRDATA* pAttrData);

        [DllImport("user32")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);


        public Dictionary<IntPtr, TASKBARPROPERTIES> taskbars { get; private set; } = new Dictionary<IntPtr, TASKBARPROPERTIES>();
        public IntPtr mainTaskbar { get; private set; }

        public void GetTaskbar()
        {
            IntPtr _taskbar;
            TASKBARPROPERTIES _properties = new TASKBARPROPERTIES();

            taskbars.Clear();
            _taskbar = FindWindow("Shell_TrayWnd", null);

            _properties.hmon = MonitorFromWindow(_taskbar, 1);
            _properties.state = TASKBARSTATE.Normal;
            taskbars.Add(_taskbar, _properties);
            mainTaskbar = _taskbar;
            IntPtr secondtaskbar = IntPtr.Zero;
            secondtaskbar = FindWindowEx(IntPtr.Zero, secondtaskbar, "Shell_SecondaryTrayWnd", null);
            while (secondtaskbar != IntPtr.Zero)
            {
                _properties.hmon = MonitorFromWindow(secondtaskbar, 1);
                _properties.state = TASKBARSTATE.Normal;
                taskbars.Add(secondtaskbar, _properties);
                secondtaskbar = FindWindowEx(IntPtr.Zero, secondtaskbar, "Shell_SecondaryTrayWnd", null);
            }
        }

        public unsafe void SetToABGR(IntPtr hwnd, int color)
        {
            ACCENTPOLICY policy = new ACCENTPOLICY(2, 2, color, 0);
            WINCOMPATTRDATA data = new WINCOMPATTRDATA(19, (IntPtr)(&policy), (ulong)sizeof(ACCENTPOLICY));
            SetWindowCompositionAttribute(hwnd, &data);
        }

        public void SetToTransparent(IntPtr hwnd)
        {
            SetToABGR(hwnd, 0x00000000);
        }
        public void SetToTransparent() { SetToTransparent(mainTaskbar); }
        public void SetToTransparent(object state) { SetToTransparent(); }
        
        public void SetToOpaque(IntPtr hwnd)
        {
            SetToABGR(hwnd, -16777216);
        }
        public void SetToOpaque() { SetToOpaque(mainTaskbar); }

        public unsafe void SetToDefault(IntPtr hwnd)
        {
            int color = (int)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Accent", "AccentColorMenu", -16777216);
            SetToABGR(hwnd, color);
        }
        public void SetToDefault() { SetToDefault(mainTaskbar); }

        public TransparentTaskbar()
        {
            GetTaskbar();
        }
    }
}