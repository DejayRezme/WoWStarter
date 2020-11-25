using System;
using System.Runtime.InteropServices;

namespace WoWStarter
{
	public struct RECT
	{
		public int Left;        // x position of upper-left corner
		public int Top;         // y position of upper-left corner
		public int Right;       // x position of lower-right corner
		public int Bottom;      // y position of lower-right corner
	}

	enum KeyModifier
	{
		None = 0,
		Alt = 1,
		Control = 2,
		Shift = 4,
		WinKey = 8
	}

	public static class Win32Util
	{
		[DllImport("user32.dll")]
		public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
		[DllImport("user32.dll")]
		public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		public const int GWL_STYLE = -16;
		public const int WS_BORDER = 0x00800000;
		public const int WS_CAPTION = 0x00C00000;
		public const int WS_SIZEBOX = 0x00040000;
		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		public static void setBorderless(IntPtr wowHandle, bool borderless)
		{
			int style = GetWindowLong(wowHandle, GWL_STYLE);
			int newStyle = borderless ? (style & ~WS_CAPTION & ~WS_SIZEBOX) : (style | WS_CAPTION | WS_SIZEBOX);
			if (newStyle != style)
				Win32Util.SetWindowLong(wowHandle, Win32Util.GWL_STYLE, newStyle);
		}

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();
		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr handle);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

		[DllImport("User32.dll")]
		public extern static bool MoveWindow(IntPtr handle, int x, int y, int width, int height, bool redraw);

		[DllImport("User32.dll")]
		public extern static int GetSystemMetrics(int nIndex);

		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
		public static IntPtr FindWindow(string windowName) { return FindWindow(null, windowName); }

		[DllImport("user32.dll")]
		public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

		[DllImport("user32.dll")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		static readonly IntPtr HWND_TOP = new IntPtr(0);
		static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
		static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
		const UInt32 SWP_NOSIZE = 0x0001;
		const UInt32 SWP_NOMOVE = 0x0002;
		public static void PositionWindow(IntPtr fHandle, int x, int y, int w, int h, bool alwaysOnTop)
		{
			SetWindowPos(fHandle, HWND_TOP, x, y, w, h, 0); // SWP_FRAMECHANGED
		}
		public static void setAlwaysOnTop(IntPtr fHandle, bool alwaysOnTop)
		{
			SetWindowPos(fHandle, alwaysOnTop ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
		}
		public static void MakeChild(IntPtr aHandle, IntPtr bHandle)
		{
			SetWindowPos(aHandle, bHandle, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
		}
		public static void SetOwner(IntPtr child, IntPtr parent)
		{ // GWL_HWNDPARENT = -8
			SetWindowLong(child, -8, (int)parent);
		}

		[DllImport("shell32.dll")]
		public static extern UInt32 SHAppBarMessage(UInt32 dwMessage, ref APPBARDATA pData);
		public const int ABM_SETAUTOHIDEBAR = 0x0a;
		[StructLayout(LayoutKind.Sequential)]
		public struct APPBARDATA
		{
			public int cbSize; // initialize this field using: Marshal.SizeOf(typeof(APPBARDATA));
			public IntPtr hWnd;
			public uint uCallbackMessage;
			public uint uEdge;
			public RECT rc;
			public int lParam;
		}

		public static void setTaskbarAutohide(bool taskbarAutohide)
		{
			APPBARDATA msgData = new APPBARDATA();
			msgData.cbSize = Marshal.SizeOf(msgData);
			msgData.hWnd = FindWindow("System_TrayWnd", null);
			msgData.lParam = taskbarAutohide ? 1 : 2;
			SHAppBarMessage(ABM_SETAUTOHIDEBAR, ref msgData);
		}

		public const int SPI_SETACTIVEWINDOWTRACKING = 0x1001;
		public const int SPI_SETACTIVEWNDTRKZORDER = 0x100D;
		public const int SPI_SETACTIVEWNDTRKTIMEOUT = 0x2003;
		public const int SPIF_SENDWININICHANGE = 2;
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool SystemParametersInfo(int uiAction, int uiParam, IntPtr pvParam, int fWinIni);

		public static void setMouseFocusTracking(bool doMouseFocusTracking)
		{
			if (doMouseFocusTracking)
			{
				SystemParametersInfo(SPI_SETACTIVEWINDOWTRACKING, 0, new IntPtr(1), SPIF_SENDWININICHANGE);
				SystemParametersInfo(SPI_SETACTIVEWNDTRKZORDER, 0, new IntPtr(0), SPIF_SENDWININICHANGE);
				SystemParametersInfo(SPI_SETACTIVEWNDTRKTIMEOUT, 0, new IntPtr(0), SPIF_SENDWININICHANGE);
			}
			else
			{
				SystemParametersInfo(SPI_SETACTIVEWINDOWTRACKING, 0, new IntPtr(0), SPIF_SENDWININICHANGE);
			}
		}
	}
}