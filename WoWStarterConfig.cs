using System;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace WoWStarter
{
	public enum MultiBoxLayouts
	{
		BottomRow,
		BottomAndRight,
		BottomDoubleRow,
		PIPVertical,
		//PIPHorizontal,
		CustomConfig,
	}

	public struct Rectangle
	{
		public int Left, Top, Width, Height;
		public Rectangle(int left, int top, int width, int height) { Left = left; Top = top; Width = width; Height = height; }
	}

	public class WoWStarterConfig
	{
		public int boxCount = 5;
		public int maxBoxCount = 10;
		public MultiBoxLayouts layout = MultiBoxLayouts.BottomRow;
		public bool borderless = true;
		public bool alwaysOnTop = false;
		public String installPath = "C:\\Program Files\\World of Warcraft\\_retail_";
		public bool mouseFocusTracking = false;
		public bool taskbarAutohide = false;
		public bool maximizeHotkey = true;
		public bool subtractTaskbarHeight = false;
		public bool closeWoWWithApp = false;
		public Rectangle PIPPosition = new Rectangle(800, 800, 240, 135);
		public Rectangle[] customLayout;
		public const String configFileName = "WoWStarter.json";

		public static WoWStarterConfig load()
		{
			try
			{
				JsonSerializerOptions options = new JsonSerializerOptions();
				options.IncludeFields = true;
				String jsonString = File.ReadAllText(configFileName);
				return JsonSerializer.Deserialize<WoWStarterConfig>(jsonString, options);
			}
			catch (FileNotFoundException)
			{
				return new WoWStarterConfig();
			}
			catch (Exception e)
			{
				MessageBox.Show("Error while reading config file, try fixing errors or deleting it: " + configFileName + e.ToString());
				return null;
			}
		}

		public void save()
		{
			try
			{
				JsonSerializerOptions options = new JsonSerializerOptions();
				options.IncludeFields = true;
				options.WriteIndented = true;
				String jsonString = JsonSerializer.Serialize(this, options);
				File.WriteAllText(configFileName, jsonString);
			}
			catch (Exception e)
			{
				MessageBox.Show("Error while writing config file: " + configFileName + e.ToString());
			}
		}
	}
}
