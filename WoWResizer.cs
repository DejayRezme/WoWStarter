using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace WoWStarter
{
	// Class to launch, configure, resize and layout the wow windows
	// Should be able to do:
	// * Launch all wow windows
	// * Relaunch a wow window if it's missing or if number of boxes is increased
	// * Change layout if different layout is selected
	// * Global hotkey to maximize PIPs or smaller WoW windows or cycle through followers
	// * 
	public class WoWResizer
	{
		protected WoWStarterConfig config;

		protected int selectedWindow = 0;
		protected List<Rectangle> layouts = new List<Rectangle>();
		//protected List<Process> wowProcesses = new List<Process>(100);
		protected List<WoWBoxState> wow = new List<WoWBoxState>(40);
		protected int currentMaximized = 0;

		protected int windowBorderSize = 0;
		protected int windowCaptionSize = 0;

		public class WoWBoxState
		{
			public Process process = null;
			public bool isBorderless = false;
			public bool isAlwaysOnTop = false;
			public Rectangle position = new Rectangle(-1, -1, 0, 0);
		}

		public WoWResizer(WoWStarterConfig config)
		{
			this.config = config;
			for (int i = 0; i < 40; i++)
				wow.Add(new WoWBoxState());
		}

		public void LaunchWoW(int boxNumber)
		{
			// first check if the process isn't already running
			Process wowProcess = wow[boxNumber].process;
			if (wowProcess == null || wowProcess.HasExited)
			{
				ProcessStartInfo startInfo = new ProcessStartInfo();

				// get the appropriate install path of which directory to launch for this box
				int installPathIndex = Math.Min(boxNumber, config.installPaths.Length - 1);
				String installPath = config.installPaths[installPathIndex];

				// check if wow.exe or wowClassic.exe exists	
				String wowExePath;
				if (File.Exists(installPath + "\\Wow.exe"))
					wowExePath = installPath + "\\Wow.exe";
				else if (File.Exists(installPath + "\\WowClassic.exe"))
					wowExePath = installPath + "\\WowClassic.exe";
				else
					throw new Exception("Wow.exe or WowClassic.exe not found in " + installPath);

				startInfo.FileName = wowExePath;
				startInfo.WorkingDirectory = installPath;

				// check to see if special config<N>.wtf exists and use it. Otherwise don't pass argument
				String configWTF = "config" + (boxNumber + 1) + ".wtf";
				if (File.Exists(installPath + "\\WTF\\" + configWTF))
					startInfo.Arguments = "-config " + configWTF;

				wowProcess = Process.Start(startInfo);

				wowProcess.WaitForInputIdle(5000);
				IntPtr wowHandle = wowProcess.MainWindowHandle;

				//Thread.Sleep(100);
				wow[boxNumber].process = wowProcess;

				// if this is our first window, get the border size by comparing window and client size
				if (windowBorderSize == 0)
				{
					//int captionHeight = SystemInformation.CaptionHeight; // doesn't seem to include shadow
					//Size borderSize = SystemInformation.Border3DSize;					
					RECT windowRect, clientRect;
					Win32Util.GetWindowRect(wowHandle, out windowRect);
					Win32Util.GetClientRect(wowHandle, out clientRect);
					windowBorderSize = ((windowRect.Right - windowRect.Left) - clientRect.Right) / 2;
					windowCaptionSize = ((windowRect.Bottom - windowRect.Top) - clientRect.Bottom - windowBorderSize);
				}
			}
		}

		public void GenerateWoWLayout()
		{
			layouts.Clear();

			Rectangle screen;
			if (config.subtractTaskbarHeight)
			{
				var vs = SystemInformation.VirtualScreen;
				vs = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
				screen = new Rectangle(vs.Left, vs.Top, vs.Width, vs.Height);
			}
			else
				screen = new Rectangle(0, 0, SystemInformation.PrimaryMonitorSize.Width, SystemInformation.PrimaryMonitorSize.Height);

			if (config.layout == MultiBoxLayouts.BottomRow)
			{
				// put all them pips on the bottom row, divide screen by boxCount-1
				int gridN = Math.Max(2, config.boxCount - 1);
				int pipWidth = screen.Width / gridN;
				int pipHeight = (pipWidth / 16) * 9;
				int mainHeight = screen.Height - pipHeight;
				int mainWidth = mainHeight / 9 * 16;

				layouts.Add(new Rectangle(screen.Left, screen.Top, mainWidth, mainHeight));
				for (int i = 0; i < config.boxCount - 1; i++)
				{   // we can fit gridN windows at the bottom
					layouts.Add(new Rectangle(i * pipWidth, mainHeight, pipWidth, pipHeight));
				}
			}
			else if (config.layout == MultiBoxLayouts.BottomDoubleRow)
			{
				// put all them pips on the bottom row, divide screen by boxCount-1
				int gridN = Math.Max(4, (int)Math.Ceiling((config.boxCount - 1) / 2.0));
				int pipWidth = screen.Width / gridN;
				int pipHeight = (pipWidth / 16) * 9;
				int mainHeight = screen.Height - pipHeight * 2;
				int mainWidth = mainHeight / 9 * 16;

				layouts.Add(new Rectangle(screen.Left, screen.Top, mainWidth, mainHeight));
				// we can fit equal windows on both rows
				int firstRow = ((config.boxCount - 1) / 2);
				for (int i = 0; i < config.boxCount - 1; i++)
				{
					if (i < firstRow)
						layouts.Add(new Rectangle(i * pipWidth, mainHeight, pipWidth, pipHeight));
					else
						layouts.Add(new Rectangle((i - firstRow) * pipWidth, mainHeight + pipHeight, pipWidth, pipHeight));
				}
			}
			else if (config.layout == MultiBoxLayouts.BottomAndRight)
			{   // Main window topleft with windows arranted in L shape around bottom right
				// how many screens fit into an L shape in an N*N grid: 1 + N + N-1 = 2*N
				// Calculate grid needed for LType layout: MB / 2
				int gridN = Math.Max(2, (int)Math.Ceiling(config.boxCount / 2.0));
				int pipWidth = screen.Width / gridN;
				//pipWidth = Math.Floor(pipWidth / 16) * 16;
				int pipHeight = (pipWidth / 16) * 9;
				int mainHeight = screen.Height - pipHeight;
				//mainHeight = Math.Floor(mainHeight / 9) * 9;
				int mainWidth = pipWidth * (gridN - 1);

				layouts.Add(new Rectangle(screen.Left, screen.Top, mainWidth, mainHeight));
				for (int i = 0; i < gridN; i++)
				{   // we can fit gridN windows at the bottom
					layouts.Add(new Rectangle(i * pipWidth, mainHeight, pipWidth, pipHeight));
				}
				for (int i = 0; i < (config.boxCount - 1 - gridN); i++)
				{   // we can fit the rest at the right
					layouts.Add(new Rectangle(mainWidth, mainHeight - (i + 1) * pipHeight, pipWidth, pipHeight));
				}
			}
			else if (config.layout == MultiBoxLayouts.PIPVertical)
			{
				layouts.Add(screen);
				var pip = config.PIPPosition;
				for (int i = 0; i < config.boxCount - 1; i++)
				{   // put the pips according to config vertical
					layouts.Add(new Rectangle(pip.Left, pip.Top + i * pip.Height, pip.Width, pip.Height));
				}
			}
			else if (config.layout == MultiBoxLayouts.CustomConfig)
			{
				for (int i = 0; i < config.boxCount; i++)
				{   // put custom configs in there, repeat last one if not enough
					layouts.Add(config.customLayout[Math.Min(i, config.customLayout.Length - 1)]);
				}
			}
		}

		public void LayoutWoWWindows()
		{
			currentMaximized = 0;
			for (int i = 0; i < config.boxCount; i++)
			{
				bool alwaysOnTop = config.alwaysOnTop && (i > 0);
				LayoutWoWWindow(i, i, alwaysOnTop);
			}
		}

		public void LayoutWoWWindow(int boxNumber, int layoutNumer, bool alwaysOnTop)
		{
			WoWBoxState wowState = wow[boxNumber];
			IntPtr wowHandle = wow[boxNumber].process.MainWindowHandle;

			string test = wowState.process.MainWindowTitle;
			// update the window style to borderless and captionless
			if (wowState.isBorderless != config.borderless) {
				Win32Util.setBorderless(wowHandle, config.borderless);
				wowState.isBorderless = config.borderless;
			}

			int x = layouts[layoutNumer].Left;
			int y = layouts[layoutNumer].Top;
			int w = layouts[layoutNumer].Width;
			int h = layouts[layoutNumer].Height;

			if (!config.borderless)
			{
				x -= windowBorderSize;
				y -= windowCaptionSize;
				w += windowBorderSize * 2;
				h += windowCaptionSize + windowBorderSize;
			}

			// move and resize the window as needed. MoveWindow seems to be faster than PositionWindow
			Win32Util.MoveWindow(wowHandle, x, y, w, h, false);
			//Win32Util.PositionWindow(wowHandle, x, y, w, h, alwaysOnTop);

			// resizing can invalidate always on top setting
			bool isResize = (wowState.position.Width != w || wowState.position.Height != h);
			wowState.position = new Rectangle(x, y, w, h);

			// update the always on top flag if needed or on resize
			if (wowState.isAlwaysOnTop != alwaysOnTop || isResize) {
				Win32Util.setAlwaysOnTop(wowHandle, alwaysOnTop);
				wowState.isAlwaysOnTop = alwaysOnTop;
			}
		}

		public void cycleToWindow(int nextMaximized)
		{
			// reset currentlyMaximized (soon previously maximized) unless it's 0
			if (currentMaximized != 0)
				LayoutWoWWindow(currentMaximized, currentMaximized, config.alwaysOnTop);
			// put the 0 to where the nextMaximized is
			LayoutWoWWindow(0, nextMaximized, config.alwaysOnTop);
			// maximize the nextMaximized
			LayoutWoWWindow(nextMaximized, 0, false);

			// this sets the maximized window as the foreground window but messes with switching back and fourth
			//Win32Util.SetForegroundWindow(wow[nextMaximized].process.MainWindowHandle);

			currentMaximized = nextMaximized;
		}

		public void maximizeHotkey(bool tabForward)
		{
			// get the current foreground window
			IntPtr forergoundWin = Win32Util.GetForegroundWindow();

			// check if it's one of our wow windows
			for (int i = 0; i < config.boxCount; i++)
			{
				Process wp = wow[i].process;
				if (wp != null && !wp.HasExited && wp.MainWindowHandle == forergoundWin)
				{
					// either we pressed Ctrl+Tab above the maximized or main window and want to cycle next
					// or we Ctrl+Tab above above one of the PIP windows and want to maximize that one
					if (i == currentMaximized)
					{
						int nextMaximized = (currentMaximized + 1) % config.boxCount;
						cycleToWindow(nextMaximized);
					}
					else
					{
						cycleToWindow(i);
					}
					// break from this loop
					return;
				}
			}
		}

		public void LaunchWoWClients()
		{
			// Process[] list = Process.GetProcesses();
			// foreach (Process p in list) {
			// 	if (p.MainWindowTitle.StartsWith("World of Warcraft"))
			// 		Console.WriteLine("WoW: " + p.ToString());
			// }
			currentMaximized = 0;
			GenerateWoWLayout();
			for (int i = 0; i < config.boxCount; i++)
			{
				LaunchWoW(i);
				bool alwaysOnTop = config.alwaysOnTop && (i > 0);
				LayoutWoWWindow(i, i, alwaysOnTop);
			}
		}

		public bool isLaunched()
		{
			return (wow[0].process != null && !wow[0].process.HasExited);
		}

		public void CloseWoWClients()
		{
			for (int i = 0; i < wow.Count; i++)
			{
				if (wow[i].process != null && !wow[i].process.HasExited)
					wow[i].process.CloseMainWindow();
			}
		}
	}
}